using src.Configuration.Models;
using src.SkillsCore.Abstractions;

namespace src.Configuration;

/*
 * Canonical effective configuration consumed by the runtime. Code owns every
 * default and heroshift.json supplies only server specific overrides.
 */
public sealed record HeroShiftConfiguration
{
    public int SchemaVersion { get; init; } = 1;
    public GeneralOptions General { get; init; } = new();
    public HudOptions Hud { get; init; } = new();
    public ChatOptions Chat { get; init; } = new();
    public CommandOptions Commands { get; init; } = new();
    public VotingOptions Voting { get; init; } = new();
    public IReadOnlyDictionary<SkillId, SkillOverride> Skills { get; init; } = new Dictionary<SkillId, SkillOverride>();
}
