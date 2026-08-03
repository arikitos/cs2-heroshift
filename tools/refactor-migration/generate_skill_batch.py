#!/usr/bin/env python3
from __future__ import annotations

import json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "HeroShift - SRC Files"
SKILLS = SRC / "src/player/skills"
DEFS = SRC / "src/Skills/BuiltIn"
BASELINE = SRC / "tools/refactor-baseline/snapshot/baseline.json"
BATCH_DOC = SRC / "src/Skills/MIGRATION_BATCHES.md"
IDS = SRC / "src/Skills/Abstractions/BuiltInSkillIds.cs"
CATALOG = SRC / "src/Skills/BuiltInSkillCatalog.cs"
CATALOG_TEST = SRC / "tests/HeroShift.Tests/BuiltInSkillCatalogTests.cs"
CHANGELOG = ROOT / "CHANGELOG.md"

HOOKS = """LoadSkill EnableSkill DisableSkill UseSkill TypeSkill OnTakeDamage OnTakeDamagePost
OnEntitySpawned OnTick CheckTransmit NewRound RoundEnd PlayerMakeSound PlayerBlind PlayerHurt
PlayerHurtPre PlayerDeath PlayerJump SwitchTeam BotTakeover WeaponFire WeaponEquip WeaponPickup
WeaponReload WeaponDrop GrenadeThrown BulletImpact BombBeginplant BombAbortplant BombPlanted
BombBegindefuse DecoyStarted DecoyDetonate SmokegrenadeDetonate SmokegrenadeExpired OnTriggerEnter
OnTriggerExit OnWeaponCanAcquire""".split()
META = {"active", "color", "onlyteam", "disableonfreezetime", "needsteammates",
        "requiredpermission", "hudduration", "descriptionhudduration", "maxperserver", "rarity"}


def read(path: Path) -> tuple[str, bool]:
    raw = path.read_bytes()
    return raw.decode("utf-8-sig"), raw.startswith(b"\xef\xbb\xbf")


def write(path: Path, text: str, bom: bool = False) -> None:
    data = text.encode()
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes((b"\xef\xbb\xbf" if bom else b"") + data)


def batches() -> dict[str, tuple[str, list[str]]]:
    text = BATCH_DOC.read_text(encoding="utf-8")
    found: dict[str, tuple[str, list[str]]] = {}
    pattern = re.compile(r"^## Batch ([A-Z]): ([^(\n]+).*?\n\n(.*?)(?=^## Batch|^Note:)", re.M | re.S)
    for letter, description, body in pattern.findall(text):
        body = re.sub(r"\([^)]*\)", "", body)
        names = re.findall(r"\b[A-Z][A-Za-z0-9]*\b", body)
        found[f"batch-{letter.lower()}"] = (description.strip().lower(), names)
    return found


def class_body(source: str, declaration: str) -> str:
    start = source.find(declaration)
    opening = source.find("{", start)
    if start < 0 or opening < 0:
        raise RuntimeError(f"Missing {declaration}")
    depth = 0
    for i in range(opening, len(source)):
        depth += source[i] == "{"
        depth -= source[i] == "}"
        if depth == 0:
            return source[opening + 1:i]
    raise RuntimeError(f"Unterminated {declaration}")


def option_properties(source: str, name: str, options: dict[str, str]) -> list[tuple[str, str, str, str]]:
    body = class_body(source, "public class SkillConfig")
    declarations = [(m.group(1).strip(), m.group(2)) for m in
                    re.finditer(r"public\s+([^\n{=]+?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}", body)]
    result = []
    for key, default in options.items():
        match = next(((typ, prop) for typ, prop in declarations if prop.lower() == key.lower()), None)
        if not match:
            raise RuntimeError(f"{name}: option property {key} not found in SkillConfig")
        result.append((key, match[1], match[0], default))
    return result


def implemented_hooks(source: str) -> list[str]:
    return [h for h in HOOKS if re.search(
        rf"public\s+static\s+(?:(?:unsafe|async)\s+)*[^\n;(]+?\s+{re.escape(h)}\s*\(", source)]


def metadata_value(value: str, key: str) -> str:
    if key == "onlyTeam":
        return value if value.startswith(("CounterStrikeSharp.", "global::")) else "CounterStrikeSharp.API.Modules.Utils." + value
    if key == "rarity":
        if value.startswith("global::"): return value
        if value.startswith("src.utils."): return "global::" + value
        if value.startswith("utils."): return "global::src." + value
        return "global::src.utils." + value
    return value


def definition(name: str, source: str, entry: dict) -> tuple[str, list[tuple[str, str, str, str]]]:
    props = option_properties(source, name, entry["options"])
    lines = ["using src.player.skills;", "using src.SkillsCore.Abstractions;", "",
             "namespace src.SkillsCore.BuiltIn;", "", f"public sealed record {name}Options : ISkillOptions", "{"]
    lines += [f"    public {typ} {prop} {{ get; init; }} = {default};" for _, prop, typ, default in props]
    lines += ["}", "", f"public static class {name}Definition", "{",
              f"    public static SkillDefinition<{name}Options> Create() => new()", "    {",
              f"        Id = BuiltInSkillIds.{name},", "        Metadata = new SkillMetadata("]
    fields = [("Active", "active"), ("Color", "color"), ("OnlyTeam", "onlyTeam"),
              ("DisableOnFreezeTime", "disableOnFreezeTime"), ("NeedsTeammates", "needsTeammates"),
              ("RequiredPermission", "requiredPermission"), ("HudDuration", "hudDuration"),
              ("DescriptionHudDuration", "descriptionHudDuration"), ("MaxPerServer", "maxPerServer"),
              ("Rarity", "rarity")]
    for i, (prop, key) in enumerate(fields):
        lines.append(f"            {prop}: {metadata_value(entry['metadata'][key], key)}{',' if i < len(fields)-1 else '),'}")
    lines += [f"        DefaultOptions = new {name}Options(),", "        Hooks = new SkillHookSet", "        {"]
    lines += [f"            {hook} = {name}.{hook}," for hook in implemented_hooks(source)]
    lines += ["        },", "    };", "}", ""]
    return "\n".join(lines), props


def add_using(source: str, namespace: str) -> str:
    line = f"using {namespace};"
    if line in source: return source
    matches = list(re.finditer(r"^using .*?;\s*$", source, re.M))
    pos = matches[-1].end() if matches else 0
    return source[:pos] + ("\n" if pos else "") + line + source[pos:]


def migrate(name: str, baseline: dict[str, dict]) -> None:
    path = SKILLS / f"{name}.cs"
    source, bom = read(path)
    original = source
    generated, props = definition(name, source, baseline[name])
    prop_map = {key.lower(): prop for key, prop, _, _ in props}
    direct = f"SkillConfigurationResolver.Get<{name}Options>(BuiltInSkillIds.{name})"
    replaced = direct in source
    source = source.replace(direct, "Options")

    def lookup(match: re.Match[str]) -> str:
        nonlocal replaced
        argument, key = match.group(2).strip(), match.group(3)
        if argument != "skillName" or key.lower() in META: return match.group(0)
        prop = prop_map.get(key.lower())
        if not prop: raise RuntimeError(f"{name}: unknown option lookup {key}")
        replaced = True
        return f"Options.{prop}"

    source = re.sub(r"SkillsInfo\.GetValue<([^>]+)>\(([^,]+),\s*\"([^\"]+)\"\)", lookup, source)
    if replaced:
        source = add_using(add_using(source, "src.SkillsCore"), "src.SkillsCore.BuiltIn")
        accessor = f"        private static {name}Options Options => SkillConfigurationResolver.Get<{name}Options>(BuiltInSkillIds.{name});\n"
        if accessor.strip() not in source:
            match = re.search(rf"(\s*private\s+const\s+Skills\s+skillName\s*=\s*Skills\.{name};\s*\n)", source)
            if not match: raise RuntimeError(f"{name}: skillName constant not found")
            source = source[:match.end()] + accessor + source[match.end():]
    if source != original: write(path, source, bom)
    write(DEFS / f"{name}Definition.cs", generated)


def migrated_order() -> list[str]:
    text = IDS.read_text(encoding="utf-8-sig")
    ids = re.findall(r"public static readonly SkillId (\w+) = SkillId\.Create", text)
    return [name for name in ids if (DEFS / f"{name}Definition.cs").exists()]


def update_catalog(names: list[str]) -> None:
    registrations = "\n".join(f"        registry.Register({n}Definition.Create());" for n in names)
    CATALOG.write_text(f'''using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;

namespace src.SkillsCore;

public static class BuiltInSkillCatalog
{{
    public static SkillRegistry BuildRegistry()
    {{
        var registry = new SkillRegistry();
{registrations}
        return registry;
    }}
}}
''', encoding="utf-8")


def update_test(names: list[str]) -> None:
    text, bom = read(CATALOG_TEST)
    entries = "\n".join(f"            BuiltInSkillIds.{n}," for n in names)
    method = f'''    [Fact]
    public void BuildRegistry_RegistersEveryMigratedSkillExactlyOnce()
    {{
        var registry = BuiltInSkillCatalog.BuildRegistry();
        SkillId[] expected =
        [
{entries}
        ];
        Assert.Equal(expected.Length, registry.All.Count);
        Assert.Equal(expected, registry.All.Select(definition => definition.Id));
    }}
'''
    pattern = re.compile(r"    \[Fact\]\s+public void BuildRegistry_RegistersEveryMigratedSkillExactlyOnce\(\)\s+\{.*?^    \}\s*", re.M | re.S)
    text, count = pattern.subn(method + "\n", text, count=1)
    if count != 1: raise RuntimeError("Catalog test method not found")
    write(CATALOG_TEST, text, bom)


def update_changelog(batch: str, description: str) -> None:
    text = CHANGELOG.read_text(encoding="utf-8")
    fixed = "- Restrict typed option discovery to the nested `SkillConfig` body and normalize fully qualified enum defaults, preventing runtime-state properties from being emitted as options."
    if fixed not in text: text = text.replace("### Fixed\n", "### Fixed\n\n" + fixed + "\n", 1)
    bullet = f"- Migrated the {description} batch to canonical typed skill definitions and options while retaining the legacy runtime dispatcher until the final cutover."
    if bullet not in text:
        if "### Changed\n" not in text: text = text.replace("## Unreleased\n", "## Unreleased\n\n### Changed\n\n" + bullet + "\n", 1)
        else: text = text.replace("### Changed\n", "### Changed\n\n" + bullet + "\n", 1)
    CHANGELOG.write_text(text, encoding="utf-8")


def validate(batch_names: list[str], names: list[str]) -> None:
    errors = []
    if len(names) != len(set(names)): errors.append("duplicate catalog IDs")
    for name in batch_names:
        source, _ = read(SKILLS / f"{name}.cs")
        for match in re.finditer(r"SkillsInfo\.GetValue<[^>]+>\(skillName,\s*\"([^\"]+)\"\)", source):
            if match.group(1).lower() not in META: errors.append(f"{name}: legacy option {match.group(1)}")
        definition_text = (DEFS / f"{name}Definition.cs").read_text()
        if "src.utils.utils" in definition_text: errors.append(f"{name}: invalid rarity namespace")
    registrations = re.findall(r"registry\.Register\((\w+)Definition", CATALOG.read_text())
    if registrations != names: errors.append("catalog order mismatch")
    if errors: raise RuntimeError("Migration validation failed:\n- " + "\n- ".join(errors))


def main() -> int:
    available = batches()
    if len(sys.argv) != 2 or sys.argv[1] not in available:
        print("Usage: generate_skill_batch.py <" + "|".join(available) + ">", file=sys.stderr)
        return 2
    batch = sys.argv[1]
    description, names = available[batch]
    data = json.loads(BASELINE.read_text(encoding="utf-8-sig"))
    baseline = {s["name"]: s for s in data["skills"]}
    for name in names: migrate(name, baseline)
    all_migrated = migrated_order()
    update_catalog(all_migrated)
    update_test(all_migrated)
    update_changelog(batch, description)
    validate(names, all_migrated)
    print(f"Migrated {batch}: {len(names)} skills; catalog now contains {len(all_migrated)} definitions")
    return 0

if __name__ == "__main__": raise SystemExit(main())
