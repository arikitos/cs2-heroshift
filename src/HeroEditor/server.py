from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import threading
import webbrowser
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any

EDITOR_DIR = Path(__file__).resolve().parent
REPO_ROOT = EDITOR_DIR.parents[1]
LOCALIZATION_PATH = REPO_ROOT / "src" / "HeroShift" / "Localization" / "Resources" / "en.json"
CONFIG_PATH = REPO_ROOT / "config" / "heroshift.json"
SKILLS_PATH = EDITOR_DIR / "skills.generated.json"
BINDINGS_PATH = EDITOR_DIR / "description.bindings.json"
PROJECT_PATH = REPO_ROOT / "src" / "HeroShift" / "HeroShift.csproj"
RELEASE_SCRIPT = REPO_ROOT / "release.ps1"
VERSION_RE = re.compile(r"^(\d+)\.(\d+)\.(\d+)$")


def read_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as stream:
        return json.load(stream)


def write_json_atomic(path: Path, value: Any) -> None:
    temporary = path.with_suffix(path.suffix + ".tmp")
    with temporary.open("w", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False)
        stream.write("\n")
    temporary.replace(path)


def parse_version(value: str) -> tuple[int, int, int]:
    match = VERSION_RE.fullmatch(value.strip())
    if match is None:
        raise ValueError(f"Invalid semantic version: {value}")
    return tuple(int(part) for part in match.groups())


def format_version(version: tuple[int, int, int]) -> str:
    return ".".join(str(part) for part in version)


def next_patch(version: tuple[int, int, int]) -> tuple[int, int, int]:
    major, minor, patch = version
    return major, minor, patch + 1


def read_project_version(project_path: Path = PROJECT_PATH) -> tuple[int, int, int]:
    text = project_path.read_text(encoding="utf-8-sig")
    match = re.search(r"<Version>\s*([^<]+)\s*</Version>", text)
    if match is None:
        raise RuntimeError(f"Version element was not found in {project_path}")
    return parse_version(match.group(1))


def local_versions(repo_root: Path = REPO_ROOT) -> list[tuple[int, int, int]]:
    versions: list[tuple[int, int, int]] = []
    for archive in repo_root.glob("HeroShift-v*.zip"):
        version_text = archive.stem.removeprefix("HeroShift-v")
        try:
            versions.append(parse_version(version_text))
        except ValueError:
            continue

    git = shutil.which("git")
    if git is not None and (repo_root / ".git").exists():
        result = subprocess.run(
            [git, "-C", str(repo_root), "tag", "--list", "v*"],
            capture_output=True,
            text=True,
            check=False,
        )
        if result.returncode == 0:
            for line in result.stdout.splitlines():
                try:
                    versions.append(parse_version(line.removeprefix("v")))
                except ValueError:
                    continue
    return versions


def calculate_next_version(repo_root: Path = REPO_ROOT) -> str:
    candidates = [read_project_version(repo_root / "src" / "HeroShift" / "HeroShift.csproj")]
    candidates.extend(local_versions(repo_root))
    return format_version(next_patch(max(candidates)))


def powershell_command() -> list[str]:
    executable = shutil.which("pwsh")
    if executable is not None:
        return [executable, "-NoProfile"]

    executable = shutil.which("powershell")
    if executable is not None:
        return [executable, "-NoProfile", "-ExecutionPolicy", "Bypass"]

    raise RuntimeError("PowerShell 7 or Windows PowerShell is required for local packaging")


def project_payload() -> dict[str, Any]:
    return {
        "skills": read_json(SKILLS_PATH),
        "localization": read_json(LOCALIZATION_PATH),
        "config": read_json(CONFIG_PATH),
        "bindings": read_json(BINDINGS_PATH),
        "nextVersion": calculate_next_version(),
    }


def validate_object(value: Any, name: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ValueError(f"{name} must be a JSON object")
    return value


def save_payload(payload: dict[str, Any]) -> None:
    localization = validate_object(payload.get("localization"), "localization")
    config = validate_object(payload.get("config"), "config")
    bindings = validate_object(payload.get("bindings"), "bindings")

    if config.get("schemaVersion") != 1:
        raise ValueError("config.schemaVersion must equal 1")

    write_json_atomic(LOCALIZATION_PATH, localization)
    write_json_atomic(CONFIG_PATH, config)
    write_json_atomic(BINDINGS_PATH, bindings)


def package_local_release() -> dict[str, Any]:
    version = calculate_next_version()
    command = powershell_command() + [
        "-File",
        str(RELEASE_SCRIPT),
        "-Version",
        version,
        "-NoPublish",
    ]
    result = subprocess.run(
        command,
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    output = "\n".join(part for part in (result.stdout.strip(), result.stderr.strip()) if part)
    if result.returncode != 0:
        raise RuntimeError(output or f"release.ps1 exited with code {result.returncode}")

    archive = REPO_ROOT / f"HeroShift-v{version}.zip"
    if not archive.is_file():
        raise RuntimeError(f"Expected archive was not created: {archive}")

    return {
        "version": version,
        "archive": str(archive),
        "output": output,
    }


class HeroEditorHandler(SimpleHTTPRequestHandler):
    server_version = "HeroEditor/1.0"

    def __init__(self, *args: Any, **kwargs: Any) -> None:
        super().__init__(*args, directory=str(EDITOR_DIR), **kwargs)

    def end_headers(self) -> None:
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def do_OPTIONS(self) -> None:
        self.send_response(HTTPStatus.NO_CONTENT)
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()

    def do_GET(self) -> None:
        if self.path == "/api/project":
            self.send_json(project_payload())
            return
        if self.path == "/":
            self.path = "/index.html"
        super().do_GET()

    def do_POST(self) -> None:
        try:
            payload = self.read_json_body()
            if self.path == "/api/save":
                save_payload(payload)
                self.send_json({"ok": True, "nextVersion": calculate_next_version()})
                return
            if self.path == "/api/publish":
                save_payload(payload)
                self.send_json({"ok": True, **package_local_release()})
                return
            self.send_error(HTTPStatus.NOT_FOUND)
        except ValueError as error:
            self.send_json({"ok": False, "error": str(error)}, HTTPStatus.BAD_REQUEST)
        except Exception as error:
            self.send_json({"ok": False, "error": str(error)}, HTTPStatus.INTERNAL_SERVER_ERROR)

    def read_json_body(self) -> dict[str, Any]:
        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0 or length > 10_000_000:
            raise ValueError("Invalid request body size")
        value = json.loads(self.rfile.read(length).decode("utf-8"))
        if not isinstance(value, dict):
            raise ValueError("Request body must be a JSON object")
        return value

    def send_json(self, value: Any, status: HTTPStatus = HTTPStatus.OK) -> None:
        body = json.dumps(value, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, format: str, *args: Any) -> None:
        print(f"HeroEditor: {format % args}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Run the local HeroShift editor")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--no-browser", action="store_true")
    args = parser.parse_args()

    server = ThreadingHTTPServer((args.host, args.port), HeroEditorHandler)
    url = f"http://{args.host}:{args.port}/"
    print(f"HeroEditor is available at {url}")
    print("Press Ctrl+C to stop it")

    if not args.no_browser:
        threading.Timer(0.35, lambda: webbrowser.open(url)).start()

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
