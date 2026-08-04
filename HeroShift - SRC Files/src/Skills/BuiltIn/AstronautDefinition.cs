using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * AstronautOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record AstronautOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .1f;
    public float ChanceTo { get; init; } = .7f;
}

/*
 * AstronautDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Astronaut gameplay implementation.
 */
public static class AstronautDefinition
{
    public static SkillDefinition<AstronautOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Astronaut,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#7E10AD",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AstronautOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.ChanceFrom, options.ChanceTo, "chanceFrom", "chanceTo"),
        Hooks = new SkillHookSet
        {
            LoadSkill = Astronaut.LoadSkill,
            EnableSkill = Astronaut.EnableSkill,
            DisableSkill = Astronaut.DisableSkill,
            NewRound = Astronaut.NewRound,
        },
    };
}
