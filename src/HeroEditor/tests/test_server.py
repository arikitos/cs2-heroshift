from __future__ import annotations

import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import server


class VersionTests(unittest.TestCase):
    def test_parse_version(self) -> None:
        self.assertEqual((1, 2, 3), server.parse_version("1.2.3"))

    def test_parse_version_rejects_invalid_input(self) -> None:
        with self.assertRaises(ValueError):
            server.parse_version("v1.2.3")

    def test_next_patch(self) -> None:
        self.assertEqual((2, 4, 10), server.next_patch((2, 4, 9)))

    def test_calculate_next_version_uses_highest_local_version(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project = root / "src" / "HeroShift" / "HeroShift.csproj"
            project.parent.mkdir(parents=True)
            project.write_text(
                "<Project><PropertyGroup><Version>1.4.2</Version></PropertyGroup></Project>",
                encoding="utf-8",
            )
            (root / "HeroShift-v1.5.7.zip").write_bytes(b"zip")
            with patch.object(server, "local_versions", return_value=[(1, 5, 7)]):
                self.assertEqual("1.5.8", server.calculate_next_version(root))


if __name__ == "__main__":
    unittest.main()
