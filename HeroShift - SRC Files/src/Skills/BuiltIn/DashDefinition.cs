using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DashOptions : ISkillOptions
{
    public float JumpVelocity { get; init; } = 150f;
    public float PushVelocity { get; init; } = 600f;
    public bool AnyDirection { get; init; } = true;
    public float Cooldown { get; init; } = 2f;
}

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
            Rarity: global::src.utils.Rarity.Common),
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
