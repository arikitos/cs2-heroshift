"""
Regenerates skills.generated.json for the HeroEditor local tool from:
- tests/HeroShift.Tests/Fixtures/baseline.json (skill metadata + default options, kept in sync by CI)
- src/HeroShift/Localization/Resources/en.json (display names + descriptions)

Run from anywhere: python src/HeroEditor/regenerate.py
Re-run this after adding/removing a skill or changing its default metadata/options.
"""
import json, re, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BASELINE = os.path.join(ROOT, "tests", "HeroShift.Tests", "Fixtures", "baseline.json")
EN = os.path.join(ROOT, "src", "HeroShift", "Localization", "Resources", "en.json")
OUT = os.path.join(os.path.dirname(__file__), "skills.generated.json")

baseline = json.load(open(BASELINE, encoding="utf-8"))
en = json.load(open(EN, encoding="utf-8"))


def unwrap_literal(v):
    if v is None:
        return None
    s = str(v).strip()
    if s == "null":
        return None
    if s.startswith('"') and s.endswith('"'):
        return json.loads(s)
    if s.endswith("f") and re.match(r"^-?\.?\d", s):
        s = s[:-1]
    try:
        if "." in s:
            return float(s)
        return int(s)
    except ValueError:
        if s in ("true", "false"):
            return s == "true"
        return s.split(".")[-1] if "." in s else s


skills = []
for sk in baseline["skills"]:
    name = sk["name"]
    skill_id = (name[0].lower() + name[1:]).lower()

    meta = sk["metadata"]
    options = sk["options"]

    skills.append({
        "id": skill_id,
        "className": name,
        "displayName": en.get(skill_id, name),
        "description": en.get(skill_id + "_desc", ""),
        "hooks": sk["hooks"],
        "metadata": {
            "active": unwrap_literal(meta.get("active")),
            "color": unwrap_literal(meta.get("color")),
            "onlyTeam": meta.get("onlyTeam", "CsTeam.None").split(".")[-1],
            "disableOnFreezeTime": unwrap_literal(meta.get("disableOnFreezeTime")),
            "needsTeammates": unwrap_literal(meta.get("needsTeammates")),
            "requiredPermission": unwrap_literal(meta.get("requiredPermission")),
            "hudDuration": unwrap_literal(meta.get("hudDuration")),
            "descriptionHudDuration": unwrap_literal(meta.get("descriptionHudDuration")),
            "maxPerServer": unwrap_literal(meta.get("maxPerServer")),
            "rarity": meta.get("rarity", "Rarity.Common").split(".")[-1],
        },
        "options": {k: unwrap_literal(v) for k, v in options.items()},
    })

skills.sort(key=lambda s: s["displayName"])

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(skills, f, indent=2, ensure_ascii=False)

print(f"Wrote {len(skills)} skills to {OUT}")
