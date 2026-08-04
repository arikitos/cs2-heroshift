using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record JumpBanOptions : ISkillOptions
{
}

public static class JumpBanDefinition
{
    public static SkillDefinition<JumpBanOptions> Create() => new()
    {
        Id = BuiltInSkillIds.JumpBan,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#b01e5d",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new JumpBanOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = JumpBan.LoadSkill,
            EnableSkill = JumpBan.EnableSkill,
            DisableSkill = JumpBan.DisableSkill,
            TypeSkill = JumpBan.TypeSkill,
            OnTick = JumpBan.OnTick,
            NewRound = JumpBan.NewRound,
            PlayerDeath = JumpBan.PlayerDeath,
            PlayerJump = JumpBan.PlayerJump,
            PlayerDisconnect = JumpBan.PlayerDisconnect,
        },
    };
}
