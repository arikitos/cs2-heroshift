using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * BehindOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record BehindOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .2f;
    public float ChanceTo { get; init; } = .4f;
}

/*
 * BehindDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Behind gameplay implementation.
 */
public static class BehindDefinition
{
    public static SkillDefinition<BehindOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Behind,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#00FF00",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new BehindOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Behind.LoadSkill,
            EnableSkill = Behind.EnableSkill,
            PlayerHurt = Behind.PlayerHurt,
        },
    };
}
