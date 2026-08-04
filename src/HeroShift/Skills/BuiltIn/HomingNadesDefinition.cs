using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record HomingNadesOptions : ISkillOptions
{
    public float Strength { get; init; } = 150;
    public float MaxVelocity { get; init; } = 2000;
    public float DetonationRange { get; init; } = 130;
    public int GrenadeLimit { get; init; } = 2;
}

public static class HomingNadesDefinition
{
    public static SkillDefinition<HomingNadesOptions> Create() => new()
    {
        Id = BuiltInSkillIds.HomingNades,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#384728",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new HomingNadesOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = HomingNades.LoadSkill,
            EnableSkill = HomingNades.EnableSkill,
            DisableSkill = HomingNades.DisableSkill,
            OnEntitySpawned = HomingNades.OnEntitySpawned,
            OnTick = HomingNades.OnTick,
            NewRound = HomingNades.NewRound,
            WeaponEquip = HomingNades.WeaponEquip,
            WeaponPickup = HomingNades.WeaponPickup,
            GrenadeThrown = HomingNades.GrenadeThrown,
        },
    };
}
