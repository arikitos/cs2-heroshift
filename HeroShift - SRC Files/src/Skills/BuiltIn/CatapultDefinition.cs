using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * CatapultOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record CatapultOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .2f;
    public float ChanceTo { get; init; } = .4f;
}

/*
 * CatapultDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Catapult gameplay implementation.
 */
public static class CatapultDefinition
{
    public static SkillDefinition<CatapultOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Catapult,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FF4500",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new CatapultOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.ChanceFrom, options.ChanceTo, "chanceFrom", "chanceTo"),
        Hooks = new SkillHookSet
        {
            LoadSkill = Catapult.LoadSkill,
            EnableSkill = Catapult.EnableSkill,
            PlayerHurt = Catapult.PlayerHurt,
        },
    };
}
