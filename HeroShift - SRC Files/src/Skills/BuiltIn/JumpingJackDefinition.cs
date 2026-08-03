using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * JumpingJackOptions - typed replacement for the legacy JumpingJack.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record JumpingJackOptions : ISkillOptions
{
    public int HealthToAdd { get; init; } = 3;
}

/*
 * JumpingJackDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing JumpingJack gameplay implementation.
 */
public static class JumpingJackDefinition
{
    public static SkillDefinition<JumpingJackOptions> Create() => new()
    {
        Id = BuiltInSkillIds.JumpingJack,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a86eff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new JumpingJackOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = JumpingJack.LoadSkill,
            PlayerJump = JumpingJack.PlayerJump,
        },
    };
}
