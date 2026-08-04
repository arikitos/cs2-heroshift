using Newtonsoft.Json.Linq;

namespace src.Configuration.Models;

/*
 * Server supplied override for one skill. Options remain JSON at this boundary
 * and are bound to the registered skill's concrete option type by the loader.
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
