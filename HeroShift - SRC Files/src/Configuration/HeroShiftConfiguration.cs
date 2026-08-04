using src.Configuration.Models;
using src.SkillsCore.Abstractions;

namespace src.Configuration;

/*
 * HeroShiftConfiguration - the one typed effective configuration root
 * (REFACTOR.md section 13). Code holds every canonical default (see the
 * property initializers below and each Options record); heroshift.json
 * (added with the override loader) supplies server-specific overrides only
 * - it never needs to restate a default the operator hasn't changed.
 *
 * This record is the canonical effective configuration consumed directly by runtime code.
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
