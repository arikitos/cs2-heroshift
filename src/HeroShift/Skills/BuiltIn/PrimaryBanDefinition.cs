using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record PrimaryBanOptions : ISkillOptions
{
}

public static class PrimaryBanDefinition
{
    public static SkillDefinition<PrimaryBanOptions> Create() => new()
    {
        Id = BuiltInSkillIds.PrimaryBan,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffc061",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PrimaryBanOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = PrimaryBan.LoadSkill,
            EnableSkill = PrimaryBan.EnableSkill,
            DisableSkill = PrimaryBan.DisableSkill,
            TypeSkill = PrimaryBan.TypeSkill,
            OnTick = PrimaryBan.OnTick,
            NewRound = PrimaryBan.NewRound,
            PlayerDeath = PrimaryBan.PlayerDeath,
            WeaponEquip = PrimaryBan.WeaponEquip,
            PlayerDisconnect = PrimaryBan.PlayerDisconnect,
        },
    };
}
