using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * RamboOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record RamboOptions : ISkillOptions
{
    public int MinExtraHealth { get; init; } = 50;
    public int MaxExtraHealth { get; init; } = 501;
}

/*
 * RamboDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Rambo gameplay implementation.
 */
public static class RamboDefinition
{
    public static SkillDefinition<RamboOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Rambo,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#009905",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RamboOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.MinExtraHealth, options.MaxExtraHealth, "minExtraHealth", "maxExtraHealth"),
        Hooks = new SkillHookSet
        {
            LoadSkill = Rambo.LoadSkill,
            EnableSkill = Rambo.EnableSkill,
            DisableSkill = Rambo.DisableSkill,
        },
    };
}
