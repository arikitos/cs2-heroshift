from pathlib import Path

ROOT = Path('HeroShift - SRC Files/src')
UTF8_BOM = b'\xef\xbb\xbf'


def read_source(path: Path) -> tuple[str, bool]:
    raw = path.read_bytes()
    has_bom = raw.startswith(UTF8_BOM)
    if has_bom:
        raw = raw[len(UTF8_BOM):]
    return raw.decode('utf-8'), has_bom


def write_source(path: Path, text: str, has_bom: bool) -> None:
    raw = text.encode('utf-8')
    path.write_bytes((UTF8_BOM if has_bom else b'') + raw)


def replace_required(text: str, old: str, new: str, label: str, expected: int = 1) -> str:
    actual = text.count(old)
    if actual != expected:
        raise SystemExit(f'{label}: expected {expected} occurrence(s), found {actual}: {old!r}')
    return text.replace(old, new)

# Command lifecycle calls and architecture comments.
path = ROOT / 'command/Command.cs'
text, bom = read_source(path)
for old, new, label, count in [
    ("     *     With no arguments it reflection-calls UseSkill on the player's hero;\n",
     "     *     With no arguments it invokes UseSkill on the player's typed hero definition;\n",
     'command use comment', 1),
    ('     *     Both go through Instance.SkillAction(skill, "HookName", params), so a\n',
     '     *     Both go through the explicit typed lifecycle coordinator, so a\n',
     'command coordinator comment', 1),
    ('     *     old hero gets "DisableSkill" and the new one "EnableSkill", and\n',
     '     *     old hero is disabled before the new one is enabled, and\n',
     'command ordering comment', 1),
    ('                Instance.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);',
     '                Instance.InvokeUseSkill(playerInfo.Skill, player);',
     'command use', 1),
    ('                Instance.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [player, commands]);',
     '                Instance.InvokeTypeSkill(playerInfo.Skill, player, commands);',
     'command type', 1),
    ('                Instance.SkillAction(skillPlayer.Skill.ToString(), "DisableSkill", [targetPlayer]);',
     '                Instance.InvokeDisableSkill(skillPlayer.Skill, targetPlayer);',
     'command indented disable', 2),
    ('                Instance.SkillAction(skill.Skill.ToString(), "EnableSkill", [targetPlayer]);',
     '                Instance.InvokeEnableSkill(skill.Skill, targetPlayer);',
     'command indented enable', 2),
    ('                        Instance.SkillAction(skill.ToString()!, "LoadSkill");',
     '                        Instance.InvokeLoadSkill(skill);',
     'command reload load', 1),
    ('            Instance.SkillAction(skillPlayer.Skill.ToString(), "DisableSkill", [targetPlayer]);',
     '            Instance.InvokeDisableSkill(skillPlayer.Skill, targetPlayer);',
     'command next disable', 1),
    ('            Instance.SkillAction(skill.Skill.ToString(), "EnableSkill", [targetPlayer]);',
     '            Instance.InvokeEnableSkill(skill.Skill, targetPlayer);',
     'command next enable', 1),
    ('        // Live-reloads config.json, skillsInfo.json and the language file, then\n',
     '        // Live-reloads heroshift.json and the selected optional language file, then\n',
     'command reload source comment', 1),
    ('        // "LoadSkill" on every Skills value whose skillsInfo "active" flag is true.\n',
     '        // invokes LoadSkill on every Skills value whose effective metadata is active.\n',
     'command reload load comment', 1),
]:
    text = replace_required(text, old, new, label, count)
write_source(path, text, bom)

# Straight lifecycle replacements.
file_replacements: dict[str, list[tuple[str, str, str, int]]] = {
    'player/BotManager.cs': [
        ('            // Reflection dispatch into src/player/skills/<Skill>.UseSkill(player).\n',
         '            // Invoke the active typed skill definition. Passive skills simply have no UseSkill hook.\n',
         'bot comment', 1),
        ('            // Heroes with no UseSkill (passive ones) simply do nothing here.\n', '', 'bot duplicate comment', 1),
        ('            Instance.SkillAction(bot_info.Skill.ToString(), "UseSkill", [randomBot]);',
         '            Instance.InvokeUseSkill(bot_info.Skill, randomBot);', 'bot use', 1),
    ],
    'player/skills/Deactivator.cs': [
        ('                Instance.SkillAction(enemyInfo.Skill.ToString(), "DisableSkill", [enemy]);',
         '                Instance.InvokeDisableSkill(enemyInfo.Skill, enemy);', 'deactivator disable', 1),
    ],
    'player/skills/Duplicator.cs': [
        ('                        Instance.SkillAction(skillName.ToString(), "EnableSkill", [player]);',
         '                        Instance.InvokeEnableSkill(skillName, player);', 'duplicator restore', 1),
        ('                        Instance?.SkillAction(enemySkill.ToString(), "EnableSkill", [player]);',
         '                        Instance?.InvokeEnableSkill(enemySkill, player);', 'duplicator delayed enable', 1),
        ('                    Instance?.SkillAction(enemySkill.ToString(), "EnableSkill", [player]);',
         '                    Instance?.InvokeEnableSkill(enemySkill, player);', 'duplicator enable', 1),
    ],
    'player/skills/Gambler.cs': [
        ('                        Instance?.SkillAction(skill.Skill.ToString(), "EnableSkill", [player]);',
         '                        Instance?.InvokeEnableSkill(skill.Skill, player);', 'gambler delayed enable', 1),
        ('                    Instance?.SkillAction(skill.Skill.ToString(), "EnableSkill", [player]);',
         '                    Instance?.InvokeEnableSkill(skill.Skill, player);', 'gambler enable', 1),
    ],
    'player/skills/Thief.cs': [
        ('                        Instance.SkillAction(skillName.ToString(), "EnableSkill", [p]);',
         '                        Instance.InvokeEnableSkill(skillName, p);', 'thief restore', 1),
        ('                Instance.SkillAction(enemySkill.ToString(), "EnableSkill", [p]);',
         '                Instance.InvokeEnableSkill(enemySkill, p);', 'thief initial enable', 1),
        ('                            Instance?.SkillAction(enemySkill.ToString(), "EnableSkill", [player]);',
         '                            Instance?.InvokeEnableSkill(enemySkill, player);', 'thief delayed enable', 1),
        ('                    Instance?.SkillAction(enemySkill.ToString(), "EnableSkill", [p]);',
         '                    Instance?.InvokeEnableSkill(enemySkill, p);', 'thief immediate enable', 1),
        ('                Instance.SkillAction(enemySkill.ToString(), "DisableSkill", [e]);',
         '                Instance.InvokeDisableSkill(enemySkill, e);', 'thief enemy disable', 1),
    ],
    'utils/SkillUtils.cs': [
        ('                    HeroShift.Instance.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { item.Item2 }]);',
         '                    HeroShift.Instance.InvokeTypeSkill(playerInfo.Skill, p, [item.Item2]);', 'menu item type', 1),
        ('                    HeroShift.Instance.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [player, new[] { randomTarget }]);',
         '                    HeroShift.Instance.InvokeTypeSkill(playerInfo.Skill, player, [randomTarget]);', 'random target type', 1),
        ('                    HeroShift.Instance.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { enemy.Item2 }]);',
         '                    HeroShift.Instance.InvokeTypeSkill(playerInfo.Skill, p, [enemy.Item2]);', 'enemy type', 1),
        ('                    HeroShift.Instance.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { lastElement.Value.Item2 }]);',
         '                    HeroShift.Instance.InvokeTypeSkill(playerInfo.Skill, p, [lastElement.Value.Item2]);', 'last target type', 1),
    ],
}

for relative, replacements in file_replacements.items():
    path = ROOT / relative
    text, bom = read_source(path)
    for old, new, label, count in replacements:
        text = replace_required(text, old, new, label, count)
    write_source(path, text, bom)

# Replace stale architecture documentation.
path = ROOT / 'player/PlayerEvents.cs'
text, bom = read_source(path)
text = replace_required(
    text,
    '     *   `public static` hook methods, and is reached by REFLECTION through\n'
    '     *   HeroShift.Instance.SkillAction(skillName, "HookName", args), which resolves\n'
    '     *   "src.player.skills.{Skill}" plus a public static method of that name. If a\n',
    '     *   `public static` hook methods registered as typed delegates in the built-in\n'
    '     *   SkillRegistry. Event callbacks resolve stable SkillIds and invoke those\n'
    '     *   delegates directly. If a\n',
    'player events architecture comment',
)
text = replace_required(
    text,
    '     *        -> InvokeSkill -> SkillAction(...) -> <Skill>.HookName(args)\n',
    '     *        -> SkillDispatcher -> registered <Skill>.HookName delegate\n',
    'player events flow comment',
)
write_source(path, text, bom)

path = ROOT / 'player/ISkill.cs'
text, bom = read_source(path)
text = replace_required(
    text,
    ''' * `public class <Name> : ISkill`. The methods below are NOT called through the
 * interface - they are all `static`, so the plugin finds and calls them by NAME
 * through reflection in HeroShift.SkillAction() ("src.player.skills.{Skill}" +
 * method name). That means:
 *   - A skill only implements the hooks it actually needs; the empty bodies here
 *     are the fallbacks, so an unimplemented hook simply does nothing.
 *   - The method signature in a skill file must match the one here EXACTLY,
 *     otherwise reflection will not find it and the hook silently never fires.
''',
    ''' * `public class <Name> : ISkill`. The methods below are static gameplay hooks.
 * BuiltInSkillCatalog registers the hooks explicitly against a stable SkillId,
 * and SkillDispatcher invokes those typed delegates directly. That means:
 *   - A skill only implements the hooks it actually needs; an unregistered hook
 *     simply does nothing.
 *   - The method signature in a skill file must match the typed SkillHookSet
 *     delegate assigned by its canonical definition.
''',
    'ISkill architecture comment',
)
text = replace_required(
    text,
    ''' * WHERE THE TUNABLE VALUES LIVE
 * Each skill file ends with a `SkillConfig` class whose constructor parameters
 * are the tunables (damage, duration, radius, chance, limits...). They are
 * serialized to configs/skillsInfo.json and read back at runtime with
 * SkillsInfo.GetValue<T>(skillName, "key"). So to rebalance a hero you edit
 * skillsInfo.json (or the default in the SkillConfig constructor) - never the
 * hook code.
''',
    ''' * WHERE THE TUNABLE VALUES LIVE
 * Every built-in skill has a canonical typed options record under src/Skills/BuiltIn.
 * Code owns the defaults; configs/heroshift.json contains server-specific overrides.
 * Gameplay code reads its typed options through SkillRuntime, without reflection or
 * string property names.
''',
    'ISkill options comment',
)
write_source(path, text, bom)

# Remove the now-unused generic compatibility dispatcher and argument helpers.
path = ROOT / 'HeroShift.cs'
text, bom = read_source(path)
text = replace_required(
    text,
    '''     * HOW A SKILL GETS CALLED
     * Every built-in skill is registered in BuiltInSkillCatalog with typed hook
     * delegates. The compatibility SkillAction entry point below resolves those
     * delegates through SkillRegistry while call sites are migrated incrementally.
     *
     * LOAD ORDER (Load method): config -> skill tunables -> translations ->
     * event/tick listeners -> commands -> WASD menu -> all skills -> player sync.
     *
     * CONFIG FILES this reads (both in the plugin's configs/ folder):
     *   settings.json    - global plugin behaviour (see utils/Config.cs)
     *   skillsInfo.json  - per-hero tunables (see utils/SkillsInfo.cs)
''',
    '''     * HOW A SKILL GETS CALLED
     * Every built-in skill is registered in BuiltInSkillCatalog with typed hook
     * delegates. SkillDispatcher routes game events, while explicit lifecycle
     * coordinator methods preserve assignment history, curse ownership and PerfLog.
     *
     * LOAD ORDER (Load method): typed heroshift.json snapshot -> embedded English /
     * optional language override -> event/tick listeners -> commands -> WASD menu ->
     * enabled skills -> player sync.
''',
    'HeroShift architecture header',
)
text = replace_required(
    text,
     '        // Skills that were enabled at least once this round; used to reset only those on round change (not all 124).\n',
     '        // Skills enabled at least once this round; used to reset only those on round change, not all 142 definitions.\n',
    'HeroShift skill-count comment',
)
start_marker = '        /*\n         * Temporary compatibility entry point while event call sites move to the\n'
end_marker = '        internal new void AddCommand'
start = text.find(start_marker)
end = text.find(end_marker, start)
if start < 0 or end < 0:
    raise SystemExit('HeroShift compatibity dispatcher block was not found')
text = text[:start] + text[end:]
write_source(path, text, bom)

# Changelog.
path = Path('CHANGELOG.md')
text = path.read_text(encoding='utf-8')
entry = '- Remove the final generic `SkillAction` compatibility dispatcher and migrate commands, bots, menu targeting and skill-copy/deactivation flows to explicit typed lifecycle coordinator methods.'
if entry not in text:
    text = text.replace('### Changed\n\n', f'### Changed\n\n{entry}\n\n', 1)
path.write_text(text, encoding='utf-8')
