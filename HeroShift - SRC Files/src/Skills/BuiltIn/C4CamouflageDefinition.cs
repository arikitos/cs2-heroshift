using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record C4CamouflageOptions : ISkillOptions
{
}

public static class C4CamouflageDefinition
{
    public static SkillDefinition<C4CamouflageOptions> Create() => new()
    {
        Id = BuiltInSkillIds.C4Camouflage,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#00911f",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Uncommon),
        DefaultOptions = new C4CamouflageOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = C4Camouflage.LoadSkill,
            EnableSkill = C4Camouflage.EnableSkill,
            DisableSkill = C4Camouflage.DisableSkill,
            OnTick = C4Camouflage.OnTick,
            CheckTransmit = C4Camouflage.CheckTransmit,
            NewRound = C4Camouflage.NewRound,
            PlayerHurt = C4Camouflage.PlayerHurt,
            WeaponEquip = C4Camouflage.WeaponEquip,
            WeaponPickup = C4Camouflage.WeaponPickup,
        },
    };
}
