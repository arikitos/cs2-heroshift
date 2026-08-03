#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC_ROOT = ROOT / "HeroShift - SRC Files"
BASELINE_PATH = SRC_ROOT / "tools/refactor-baseline/snapshot/baseline.json"
SKILLS_DIR = SRC_ROOT / "src/player/skills"
DEFINITIONS_DIR = SRC_ROOT / "src/Skills/BuiltIn"
CATALOG_PATH = SRC_ROOT / "src/Skills/BuiltInSkillCatalog.cs"
IDS_PATH = SRC_ROOT / "src/Skills/Abstractions/BuiltInSkillIds.cs"
CATALOG_TEST_PATH = SRC_ROOT / "tests/HeroShift.Tests/BuiltInSkillCatalogTests.cs"
CHANGELOG_PATH = ROOT / "CHANGELOG.md"

BATCHES: dict[str, list[str]] = {
    "batch-a": [
        "AntyFlash", "Astronaut", "Behind", "Catapult", "Disarmament", "Dracula", "Dwarf",
        "FastReload", "FragileBomb", "Grenadier", "Illiterate", "Impostor", "InfiniteAmmo",
        "JumpingJack", "Knockback", "None", "Push", "Pyro", "Rambo", "ReturnToSender",
        "RichBoy", "RobinHood", "Saper", "ShortBomb", "Silent", "Teleporter", "Zeus",
    ],
    "batch-b": [
        "AimLock", "Anomaly", "AreaReaper", "Bankrupt", "BunnyHop", "C4Camouflage", "Chicken",
        "ChillOut", "Darkness", "Dash", "Deactivator", "Deaf", "Distancer", "Duplicator",
        "EnemySpawn", "ExpensiveAmmo", "FalconEye", "Flash", "FrozenDecoy", "Ghost", "Giant",
        "Glitch", "HealingChicken", "Jammer", "JetKick", "JumpBan", "JumpCurse", "LifeSwap",
        "MagneticDecoy", "Magnifier", "Medic", "MoneySwap", "Ninja", "NoRecoil", "PawelJumper",
        "Pilot", "Planter", "PrimaryBan", "PsychicDefusing", "QuickShot", "RadarHack",
        "Regeneration", "Retreat", "Rubber", "SoundMaker", "Spectator", "SwapPosition", "TakeAmmo",
        "Thief", "ThirdEye", "WeaponsSwap",
    ],
    "batch-c": [
        "Aimbot", "AntyHead", "Armored", "Assassin", "Berserker", "BladeMaster", "CarefulBullets",
        "Cutter", "DemonEye", "Fortnite", "FriendlyFire", "HotBomb", "KillerFlash", "LastGasp",
        "NoNades", "OneShot", "OnlyHead", "Phoenix", "Poison", "Prosthesis", "ReZombie",
        "ReactiveArmor", "Replicator", "SecondLife", "Soldier", "Thorns",
    ],
    "batch-d": [
        "Baseball", "BlastShot", "DeathBomb", "ExplodingBarrel", "ExplosiveShot", "FireRain",
        "Flashlight", "Glue", "GodMode", "HealingSmoke", "HolyHandGrenade", "HomingNades",
        "Illusionist", "Jackal", "Jester", "Magneto", "Miner", "Nightmare", "RandomWeapon",
        "SniperElite", "ThrowingKnife", "ToxicSmoke", "Watchmaker", "Weightless", "WildThrow",
    ],
    "batch-e": ["Cypher", "Iana", "LongKnife", "LongZeus", "Noclip", "Shade", "TeamTeleport", "Tripwire"],
    "batch-g": ["Gambler", "Glaz", "Hermit", "Smoker", "Wallhack"],
}

BATCH_DESCRIPTIONS = {
    "batch-a": "passive skills",
    "batch-b": "tick and movement skills",
    "batch-c": "damage pipeline skills",
    "batch-d": "entity and grenade skills",
    "batch-e": "RayTrace skills",
    "batch-g": "remaining complex skills",
}

HOOK_ORDER = [
    "LoadSkill", "EnableSkill", "DisableSkill", "UseSkill", "TypeSkill", "OnTakeDamage",
    "OnTakeDamagePost", "OnEntitySpawned", "OnTick", "CheckTransmit", "NewRound", "RoundEnd",
    "PlayerMakeSound", "PlayerBlind", "PlayerHurt", "PlayerHurtPre", "PlayerDeath", "PlayerJump",
    "SwitchTeam", "BotTakeover", "WeaponFire", "WeaponEquip", "WeaponPickup", "WeaponReload",
    "WeaponDrop", "GrenadeThrown", "BulletImpact", "BombBeginplant", "BombAbortplant", "BombPlanted",
    "BombBegindefuse", "DecoyStarted", "DecoyDetonate", "SmokegrenadeDetonate",
    "SmokegrenadeExpired", "OnTriggerEnter", "OnTriggerExit", "OnWeaponCanAcquire",
]

METADATA_LOOKUP_KEYS = {
    "active", "color", "onlyteam", "disableonfreezetime", "needsteammates",
    "requiredpermission", "hudduration", "descriptionhudduration", "maxperserver", "rarity",
}


def read_utf8(path: Path) -> tuple[str, bool]:
    raw = path.read_bytes()
    return raw.decode("utf-8-sig"), raw.startswith(b"\xef\xbb\xbf")


def write_utf8(path: Path, text: str, bom: bool = False) -> None:
    data = text.encode("utf-8")
    if bom:
        data = b"\xef\xbb\xbf" + data
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)


def load_baseline() -> dict[str, dict]:
    data = json.loads(BASELINE_PATH.read_text(encoding="utf-8-sig"))
    return {skill["name"]: skill for skill in data["skills"]}


def property_info(source: str, skill_name: str, options: dict[str, str]) -> list[tuple[str, str, str, str]]:
    declarations = [
        (match.group(1).strip(), match.group(2))
        for match in re.finditer(r"public\s+([^\n{=]+?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}", source)
    ]
    result: list[tuple[str, str, str, str]] = []
    for key, default in options.items():
        found = next(((typ, prop) for typ, prop in declarations if prop.lower() == key.lower()), None)
        if found is None:
            raise RuntimeError(f"{skill_name}: property for option '{key}' was not found")
        result.append((key, found[1], found[0], default))
    return result


def implemented_hooks(source: str) -> list[str]:
    hooks: list[str] = []
    for hook in HOOK_ORDER:
        pattern = rf"public\s+static\s+(?:(?:unsafe|async)\s+)*[^\n;(]+?\s+{re.escape(hook)}\s*\("
        if re.search(pattern, source):
            hooks.append(hook)
    return hooks


def metadata_expression(value: str, key: str) -> str:
    if key == "onlyTeam":
        return "CounterStrikeSharp.API.Modules.Utils." + value
    if key == "rarity":
        return "global::src.utils." + value
    return value


def generate_definition(skill_name: str, baseline: dict[str, dict]) -> tuple[str, list[tuple[str, str, str, str]], list[str]]:
    source_path = SKILLS_DIR / f"{skill_name}.cs"
    source, _ = read_utf8(source_path)
    entry = baseline[skill_name]
    properties = property_info(source, skill_name, entry["options"])
    hooks = implemented_hooks(source)

    lines = [
        "using src.player.skills;",
        "using src.SkillsCore.Abstractions;",
        "",
        "namespace src.SkillsCore.BuiltIn;",
        "",
        "/*",
        f" * {skill_name}Options - typed replacement for the legacy {skill_name}.SkillConfig",
        " * tunables. Defaults are transcribed verbatim from the baseline snapshot.",
        " */",
        f"public sealed record {skill_name}Options : ISkillOptions",
        "{",
    ]
    for _, property_name, property_type, default in properties:
        lines.append(f"    public {property_type} {property_name} {{ get; init; }} = {default};")

    lines.extend([
        "}",
        "",
        "/*",
        f" * {skill_name}Definition - canonical identity, metadata, typed defaults and hooks",
        f" * for the existing {skill_name} gameplay implementation.",
        " */",
        f"public static class {skill_name}Definition",
        "{",
        f"    public static SkillDefinition<{skill_name}Options> Create() => new()",
        "    {",
        f"        Id = BuiltInSkillIds.{skill_name},",
        "        Metadata = new SkillMetadata(",
    ])

    metadata = entry["metadata"]
    fields = [
        ("Active", "active"), ("Color", "color"), ("OnlyTeam", "onlyTeam"),
        ("DisableOnFreezeTime", "disableOnFreezeTime"), ("NeedsTeammates", "needsTeammates"),
        ("RequiredPermission", "requiredPermission"), ("HudDuration", "hudDuration"),
        ("DescriptionHudDuration", "descriptionHudDuration"), ("MaxPerServer", "maxPerServer"),
        ("Rarity", "rarity"),
    ]
    for index, (property_name, key) in enumerate(fields):
        suffix = "," if index < len(fields) - 1 else "),"
        lines.append(f"            {property_name}: {metadata_expression(metadata[key], key)}{suffix}")

    lines.extend([
        f"        DefaultOptions = new {skill_name}Options(),",
        "        Hooks = new SkillHookSet",
        "        {",
    ])
    for hook in hooks:
        lines.append(f"            {hook} = {skill_name}.{hook},")
    lines.extend(["        },", "    };", "}", ""])
    return "\n".join(lines), properties, hooks


def ensure_using(source: str, namespace: str) -> str:
    using_line = f"using {namespace};"
    if using_line in source:
        return source
    matches = list(re.finditer(r"^using .*?;\s*$", source, re.MULTILINE))
    if not matches:
        return using_line + "\n" + source
    position = matches[-1].end()
    return source[:position] + "\n" + using_line + source[position:]


def ensure_options_accessor(source: str, skill_name: str) -> str:
    accessor = (
        f"        private static {skill_name}Options Options => "
        f"SkillConfigurationResolver.Get<{skill_name}Options>(BuiltInSkillIds.{skill_name});\n"
    )
    if accessor.strip() in source:
        return source
    pattern = rf"(\s*private\s+const\s+Skills\s+skillName\s*=\s*Skills\.{re.escape(skill_name)};\s*\n)"
    match = re.search(pattern, source)
    if match is None:
        raise RuntimeError(f"{skill_name}: skillName constant was not found")
    return source[:match.end()] + accessor + source[match.end():]


def migrate_skill(skill_name: str, baseline: dict[str, dict]) -> None:
    source_path = SKILLS_DIR / f"{skill_name}.cs"
    source, had_bom = read_utf8(source_path)
    original = source
    definition, properties, _ = generate_definition(skill_name, baseline)
    property_map = {key.lower(): property_name for key, property_name, _, _ in properties}
    replacements = 0

    direct = f"SkillConfigurationResolver.Get<{skill_name}Options>(BuiltInSkillIds.{skill_name})"
    if direct in source:
        source = source.replace(direct, "Options")
        replacements += 1

    def replace_lookup(match: re.Match[str]) -> str:
        nonlocal replacements
        argument = match.group(2).strip()
        key = match.group(3)
        if argument != "skillName" or key.lower() in METADATA_LOOKUP_KEYS:
            return match.group(0)
        property_name = property_map.get(key.lower())
        if property_name is None:
            raise RuntimeError(f"{skill_name}: lookup key '{key}' has no typed property")
        replacements += 1
        return f"Options.{property_name}"

    source = re.sub(
        r"SkillsInfo\.GetValue<([^>]+)>\(([^,]+),\s*\"([^\"]+)\"\)",
        replace_lookup,
        source,
    )

    if replacements:
        source = ensure_using(source, "src.SkillsCore")
        source = ensure_using(source, "src.SkillsCore.BuiltIn")
        source = ensure_options_accessor(source, skill_name)

    if source != original:
        write_utf8(source_path, source, had_bom)
    write_utf8(DEFINITIONS_DIR / f"{skill_name}Definition.cs", definition)


def built_in_order() -> list[str]:
    text = IDS_PATH.read_text(encoding="utf-8-sig")
    return re.findall(r"public static readonly SkillId (\w+) = SkillId\.Create", text)


def migrated_skills_in_order() -> list[str]:
    return [name for name in built_in_order() if (DEFINITIONS_DIR / f"{name}Definition.cs").exists()]


def update_catalog(migrated: list[str]) -> None:
    registrations = "\n".join(f"        registry.Register({name}Definition.Create());" for name in migrated)
    content = f'''using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;

namespace src.SkillsCore;

/*
 * BuiltInSkillCatalog - registers every migrated skill's typed definition in
 * stable legacy skill order. The catalog remains additive until the live
 * runtime is switched from reflection to SkillDispatcher.
 */
public static class BuiltInSkillCatalog
{{
    public static SkillRegistry BuildRegistry()
    {{
        var registry = new SkillRegistry();

{registrations}

        return registry;
    }}
}}
'''
    CATALOG_PATH.write_text(content, encoding="utf-8")


def update_catalog_test(migrated: list[str]) -> None:
    text, had_bom = read_utf8(CATALOG_TEST_PATH)
    entries = "\n".join(f"            BuiltInSkillIds.{name}," for name in migrated)
    replacement = f'''    [Fact]
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
    pattern = re.compile(
        r"    \[Fact\]\s+public void BuildRegistry_RegistersEveryMigratedSkillExactlyOnce\(\)\s+\{.*?^    \}\s*",
        re.MULTILINE | re.DOTALL,
    )
    updated, count = pattern.subn(replacement + "\n", text, count=1)
    if count != 1:
        raise RuntimeError("Could not update BuiltInSkillCatalogTests registration test")
    write_utf8(CATALOG_TEST_PATH, updated, had_bom)


def update_changelog(batch: str) -> None:
    description = BATCH_DESCRIPTIONS[batch]
    bullet = (
        f"- Migrated the {description} batch to canonical typed skill definitions and options "
        "while retaining the legacy runtime dispatcher until the final cutover."
    )
    text = CHANGELOG_PATH.read_text(encoding="utf-8")
    if bullet in text:
        return
    if "### Changed\n" not in text:
        marker = "## Unreleased\n"
        text = text.replace(marker, marker + "\n### Changed\n\n" + bullet + "\n", 1)
    else:
        text = text.replace("### Changed\n", "### Changed\n\n" + bullet + "\n", 1)
    CHANGELOG_PATH.write_text(text, encoding="utf-8")


def validate_migration(skill_names: list[str], baseline: dict[str, dict], migrated: list[str]) -> None:
    errors: list[str] = []
    if len(migrated) != len(set(migrated)):
        errors.append("Built-in catalog contains duplicate skill IDs")

    for skill_name in skill_names:
        definition_path = DEFINITIONS_DIR / f"{skill_name}Definition.cs"
        if not definition_path.exists():
            errors.append(f"{skill_name}: definition was not generated")
            continue
        source, _ = read_utf8(SKILLS_DIR / f"{skill_name}.cs")
        for match in re.finditer(r"SkillsInfo\.GetValue<[^>]+>\(skillName,\s*\"([^\"]+)\"\)", source):
            if match.group(1).lower() not in METADATA_LOOKUP_KEYS:
                errors.append(f"{skill_name}: legacy option lookup remains for '{match.group(1)}'")
        if skill_name not in baseline:
            errors.append(f"{skill_name}: missing from baseline")

    catalog = CATALOG_PATH.read_text(encoding="utf-8")
    registrations = re.findall(r"registry\.Register\((\w+)Definition\.Create\(\)\);", catalog)
    if registrations != migrated:
        errors.append("BuiltInSkillCatalog registration order does not match BuiltInSkillIds")

    if errors:
        raise RuntimeError("Migration validation failed:\n- " + "\n- ".join(errors))


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] not in BATCHES:
        print("Usage: generate_skill_batch.py <" + "|".join(BATCHES) + ">", file=sys.stderr)
        return 2

    batch = sys.argv[1]
    baseline = load_baseline()
    for skill_name in BATCHES[batch]:
        migrate_skill(skill_name, baseline)

    migrated = migrated_skills_in_order()
    update_catalog(migrated)
    update_catalog_test(migrated)
    update_changelog(batch)
    validate_migration(BATCHES[batch], baseline, migrated)

    print(f"Migrated {batch}: {len(BATCHES[batch])} skills; catalog now contains {len(migrated)} definitions")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
