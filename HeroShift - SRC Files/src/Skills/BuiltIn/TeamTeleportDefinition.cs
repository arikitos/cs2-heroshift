using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record TeamTeleportOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 15f;
    public float TeleportAngle { get; init; } = 10.0f;
    public float TeleportDistance { get; init; } = 100f;
}

public static class TeamTeleportDefinition
{
    public static SkillDefinition<TeamTeleportOptions> Create() => new()
    {
        Id = BuiltInSkillIds.TeamTeleport,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#bcf542",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: true,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new TeamTeleportOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = TeamTeleport.LoadSkill,
            EnableSkill = TeamTeleport.EnableSkill,
            DisableSkill = TeamTeleport.DisableSkill,
            UseSkill = TeamTeleport.UseSkill,
            OnTick = TeamTeleport.OnTick,
            NewRound = TeamTeleport.NewRound,
        },
    };
}
