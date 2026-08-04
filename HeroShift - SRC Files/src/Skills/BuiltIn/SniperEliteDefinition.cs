using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record SniperEliteOptions : ISkillOptions
{
}

public static class SniperEliteDefinition
{
    public static SkillDefinition<SniperEliteOptions> Create() => new()
    {
        Id = BuiltInSkillIds.SniperElite,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#e0873a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SniperEliteOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = SniperElite.LoadSkill,
            EnableSkill = SniperElite.EnableSkill,
            DisableSkill = SniperElite.DisableSkill,
            UseSkill = SniperElite.UseSkill,
            NewRound = SniperElite.NewRound,
            PlayerDeath = SniperElite.PlayerDeath,
            WeaponEquip = SniperElite.WeaponEquip,
        },
    };
}
