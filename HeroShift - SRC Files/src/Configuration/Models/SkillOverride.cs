using Newtonsoft.Json.Linq;

namespace src.Configuration.Models;

/*
 * SkillOverride - a server operator's heroshift.json override for one skill.
 * "Options" is deliberately untyped JSON at this layer (see REFACTOR.md
 * section 13-14): the configuration loader does not know each skill's typed
 * option shape - only the SkillRegistry (populated once skills migrate) does.
 * Binding "Options" into a skill's concrete TOptions record, validating
 * unknown/invalid fields, happens in the per-skill options resolver added
 * once the dispatcher and first skill batch land, not in this commit.
 */
public sealed record SkillOverride
{
    public bool? Enabled { get; init; }
    public string? Color { get; init; }
    public string? OnlyTeam { get; init; }
    public bool? DisableOnFreezeTime { get; init; }
    public bool? NeedsTeammates { get; init; }
    public string? RequiredPermission { get; init; }
    public float? HudDuration { get; init; }
    public float? DescriptionHudDuration { get; init; }
    public int? MaxPerServer { get; init; }
    public string? Rarity { get; init; }
    public JObject? Options { get; init; }
}
