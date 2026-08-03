using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * DashOptions - typed replacement for the legacy Dash.SkillConfig tunables
 * (src/player/skills/Dash.cs). Defaults transcribed verbatim from that
 * SkillConfig's constructor parameters.
 */
public sealed record DashOptions : ISkillOptions
{
    public float JumpVelocity { get; init; } = 150f;
    public float PushVelocity { get; init; } = 600f;
    public bool AnyDirection { get; init; } = true;
    public float Cooldown { get; init; } = 2f;
}

/*
 * DashDefinition - typed SkillDefinition for Dash. Hooks reference the
 * skill's existing public static methods directly as delegates (REFACTOR.md
 * section 23) - Dash.cs's hook bodies are unchanged except for the
 * SkillsInfo.GetValue calls, which now read SkillConfigurationResolver's
 * typed DashOptions snapshot instead.
 */
public static class DashDefinition
{
    public static SkillDefinition<DashOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Dash,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#42bbfc",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: utils.Rarity.Common),
        DefaultOptions = new DashOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Dash.LoadSkill,
            EnableSkill = Dash.EnableSkill,
            DisableSkill = Dash.DisableSkill,
            OnTick = Dash.OnTick,
            NewRound = Dash.NewRound,
        },
    };
}
