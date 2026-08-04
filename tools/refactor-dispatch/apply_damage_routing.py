from pathlib import Path

path = Path("HeroShift - SRC Files/src/player/PlayerEvents.cs")
text = path.read_text(encoding="utf-8")

text = text.replace("            object[] args = [h];\n", "", 1)
text = text.replace("                InvokeOnTakeDamage(p.Skill, h, args, post);", "                InvokeOnTakeDamage(p.Skill, h, post);")
text = text.replace("                InvokeOnTakeDamage(skill, h, args, post);", "                InvokeOnTakeDamage(skill, h, post);")
text = text.replace(
    "        private static void InvokeOnTakeDamage(Skills skill, DynamicHook h, object[] args, bool post)",
    "        private static void InvokeOnTakeDamage(Skills skill, DynamicHook h, bool post)",
    1,
)
text = text.replace(
    '                InvokeSkill(skill, post ? "OnTakeDamagePost" : "OnTakeDamage", args);',
    "                Instance.SkillDispatcher.DispatchOnTakeDamage([SkillRuntime.GetId(skill)], h, post);",
    1,
)
text = text.replace(
    '            InvokeSkill(skill, post ? "OnTakeDamagePost" : "OnTakeDamage", args);',
    "            Instance.SkillDispatcher.DispatchOnTakeDamage([SkillRuntime.GetId(skill)], h, post);",
    1,
)

if 'InvokeOnTakeDamage(p.Skill, h, args, post)' in text or 'InvokeOnTakeDamage(skill, h, args, post)' in text:
    raise SystemExit("Legacy damage argument routing remains")
if 'post ? "OnTakeDamagePost" : "OnTakeDamage"' in text:
    raise SystemExit("String-based damage hook routing remains")

path.write_text(text, encoding="utf-8")

changelog = Path("CHANGELOG.md")
log = changelog.read_text(encoding="utf-8")
entry = "- Route the ordered damage pre/post pipeline through typed dispatcher calls while preserving deferred revive-skill ordering and per-skill debug damage snapshots."
if entry not in log:
    log = log.replace("### Changed\n\n", f"### Changed\n\n{entry}\n\n", 1)
changelog.write_text(log, encoding="utf-8")
