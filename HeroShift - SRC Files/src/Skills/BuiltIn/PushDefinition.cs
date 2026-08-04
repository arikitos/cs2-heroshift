using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * PushOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record PushOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .3f;
    public float ChanceTo { get; init; } = .4f;
    public float JumpVelocity { get; init; } = 300f;
    public float PushVelocity { get; init; } = 400f;
}

/*
 * PushDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Push gameplay implementation.
 */
public static class PushDefinition
{
    public static SkillDefinition<PushOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Push,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#1e9ab0",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PushOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Push.LoadSkill,
            EnableSkill = Push.EnableSkill,
            PlayerHurt = Push.PlayerHurt,
        },
    };
}
