using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * TeleporterOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record TeleporterOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .5f;
    public float ChanceTo { get; init; } = .6f;
}

/*
 * TeleporterDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Teleporter gameplay implementation.
 */
public static class TeleporterDefinition
{
    public static SkillDefinition<TeleporterOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Teleporter,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8A2BE2",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new TeleporterOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.ChanceFrom, options.ChanceTo, "chanceFrom", "chanceTo"),
        Hooks = new SkillHookSet
        {
            LoadSkill = Teleporter.LoadSkill,
            EnableSkill = Teleporter.EnableSkill,
            PlayerHurt = Teleporter.PlayerHurt,
        },
    };
}
