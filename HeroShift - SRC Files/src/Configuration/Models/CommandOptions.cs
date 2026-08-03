namespace src.Configuration.Models;

/*
 * CommandDefinition - one console/chat command's aliases and required
 * permission. Aliases are an array, not the legacy comma-separated string
 * (e.g. "setskill, set_skill") - REFACTOR.md section 20 requires arrays in
 * the new schema; the loader that reads heroshift.json overrides is
 * responsible for trimming, rejecting empty entries and detecting
 * duplicates across commands (added with the override loader).
 */
public sealed record CommandDefinition
{
    public required IReadOnlyList<string> Aliases { get; init; }
    public required string Permission { get; init; }
}

/*
 * CommandOptions - field-for-field equivalent of the legacy
 * src/utils/Config.NormalCommands nested class. Default aliases/permissions
 * are transcribed verbatim from that type's constructor.
 */
public sealed record CommandOptions
{
    public CommandDefinition SetSkillCommand { get; init; } = new() { Aliases = ["setskill", "set_skill"], Permission = "@HeroShift/admin" };
    public CommandDefinition SkillsListCommand { get; init; } = new() { Aliases = ["skills"], Permission = "@HeroShift/admin" };
    public CommandDefinition UseSkillCommand { get; init; } = new() { Aliases = ["t", "useSkill"], Permission = "@HeroShift/admin" };
    public CommandDefinition HealCommand { get; init; } = new() { Aliases = ["heal"], Permission = "@HeroShift/admin" };
    public CommandDefinition HealthCommand { get; init; } = new() { Aliases = ["sethealth", "set_health", "health"], Permission = "@HeroShift/admin" };
    public CommandDefinition PlantedBomb { get; init; } = new() { Aliases = ["plantedbomb", "planted_bomb", "bomb"], Permission = "@HeroShift/admin" };
    public CommandDefinition BotPlace { get; init; } = new() { Aliases = ["botplace", "bot_place"], Permission = "@HeroShift/admin" };
    public CommandDefinition ConsoleCommand { get; init; } = new() { Aliases = ["console", "sv"], Permission = "@HeroShift/owner" };
    public CommandDefinition HudCommand { get; init; } = new() { Aliases = ["hud", "hood"], Permission = "" };
    public CommandDefinition SetStaticSkillCommand { get; init; } = new() { Aliases = ["setstaticskill", "set_static_skill"], Permission = "@HeroShift/admin" };
    public CommandDefinition ReloadCommand { get; init; } = new() { Aliases = ["reload", "refresh"], Permission = "@HeroShift/admin" };
    public CommandDefinition NextCommand { get; init; } = new() { Aliases = ["next_skill"], Permission = "@HeroShift/admin" };
    public CommandDefinition CheckEntityCommand { get; init; } = new() { Aliases = ["ent", "entity", "checkentity", "check_entity", "checkent", "check_ent"], Permission = "@HeroShift/owner" };
}
