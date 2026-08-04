using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record JetKickOptions : ISkillOptions
{
    public float PushVelocity { get; init; } = 400f;
}

public static class JetKickDefinition
{
    public static SkillDefinition<JetKickOptions> Create() => new()
    {
        Id = BuiltInSkillIds.JetKick,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#5a4fd1",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new JetKickOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = JetKick.LoadSkill,
            EnableSkill = JetKick.EnableSkill,
            DisableSkill = JetKick.DisableSkill,
            TypeSkill = JetKick.TypeSkill,
            OnTick = JetKick.OnTick,
            NewRound = JetKick.NewRound,
            PlayerDeath = JetKick.PlayerDeath,
            WeaponFire = JetKick.WeaponFire,
        },
    };
}
