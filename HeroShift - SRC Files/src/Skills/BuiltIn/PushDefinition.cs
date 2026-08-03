using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * PushOptions - typed replacement for the legacy Push.SkillConfig tunables
 * (src/player/skills/Push.cs). Defaults transcribed verbatim from that
 * SkillConfig's constructor parameters.
 */
public sealed record PushOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .3f;
    public float ChanceTo { get; init; } = .4f;
    public float JumpVelocity { get; init; } = 300f;
    public float PushVelocity { get; init; } = 400f;
}

/*
 * PushDefinition - typed SkillDefinition for Push. Hooks reference the
 * skill's existing public static methods directly as delegates (REFACTOR.md
 * section 23) - Push.cs's hook bodies are unchanged except for the
 * SkillsInfo.GetValue calls, which now read SkillConfigurationResolver's
 * typed PushOptions snapshot instead.
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
            Rarity: utils.Rarity.Common),
        DefaultOptions = new PushOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Push.LoadSkill,
            EnableSkill = Push.EnableSkill,
            PlayerHurt = Push.PlayerHurt,
        },
    };
}
