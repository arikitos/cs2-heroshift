from __future__ import annotations

import json
import re
import unittest
from pathlib import Path

EDITOR_DIR = Path(__file__).resolve().parents[1]
SKILLS_PATH = EDITOR_DIR / "skills.generated.json"
BINDINGS_PATH = EDITOR_DIR / "description.bindings.json"
TOKEN_RE = re.compile(r"\{\{([A-Za-z0-9_]+)(?:\|([A-Za-z0-9_]+))?\}\}")
NUMBER_RE = re.compile(r"(?<![A-Za-z])\d+(?:\.\d+)?%?")
SUPPORTED_FORMATTERS = {None, "raw", "percent", "seconds", "multiplier", "currency"}
TECHNICAL_NUMERIC_OPTIONS = {
    "r",
    "g",
    "b",
    "a",
    "colorR",
    "colorG",
    "colorB",
    "blindAlpha",
}
SEMANTIC_NUMERIC_DESCRIPTIONS = {
    # Zeus x27 is the weapon name, not a configurable gameplay quantity.
    "zeus",
}


def load_json(path: Path):
    with path.open("r", encoding="utf-8-sig") as stream:
        return json.load(stream)


class DescriptionBindingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.skills = load_json(SKILLS_PATH)
        cls.skills_by_id = {skill["id"]: skill for skill in cls.skills}
        cls.bindings = load_json(BINDINGS_PATH)

    def test_every_binding_targets_an_existing_skill_and_option(self) -> None:
        for skill_id, template in self.bindings.items():
            with self.subTest(skill=skill_id):
                self.assertIn(skill_id, self.skills_by_id)
                skill = self.skills_by_id[skill_id]
                options = skill.get("options", {})
                for key, formatter in TOKEN_RE.findall(template):
                    self.assertIn(key, options, f"Unknown option token {key!r} in {skill_id}")
                    self.assertIn(formatter or None, SUPPORTED_FORMATTERS)

    def test_every_gameplay_numeric_option_is_represented(self) -> None:
        missing: list[str] = []
        for skill in self.skills:
            gameplay_options = {
                key
                for key, value in skill.get("options", {}).items()
                if isinstance(value, (int, float, bool)) and key not in TECHNICAL_NUMERIC_OPTIONS
            }
            if not gameplay_options:
                continue

            template = self.bindings.get(skill["id"], "")
            bound_options = {key for key, _ in TOKEN_RE.findall(template)}
            for key in sorted(gameplay_options - bound_options):
                missing.append(f"{skill['id']}.{key}")

        self.assertEqual([], missing, "Gameplay options missing from descriptions: " + ", ".join(missing))

    def test_numeric_default_descriptions_have_a_binding(self) -> None:
        missing = [
            skill["id"]
            for skill in self.skills
            if NUMBER_RE.search(skill.get("description", ""))
            and skill["id"] not in self.bindings
            and skill["id"] not in SEMANTIC_NUMERIC_DESCRIPTIONS
        ]
        self.assertEqual([], missing, "Numeric descriptions without bindings: " + ", ".join(missing))


if __name__ == "__main__":
    unittest.main()
