using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record HolyHandGrenadeOptions : ISkillOptions
{
    public float DamageMultiplier { get; init; } = 2f;
    public float DamageRadiusMultiplier { get; init; } = 2f;
    public int GrenadeLimit { get; init; } = 1;
}

public static class HolyHandGrenadeDefinition
{
    public static SkillDefinition<HolyHandGrenadeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.HolyHandGrenade,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffdd00",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new HolyHandGrenadeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = HolyHandGrenade.LoadSkill,
            EnableSkill = HolyHandGrenade.EnableSkill,
            DisableSkill = HolyHandGrenade.DisableSkill,
            OnEntitySpawned = HolyHandGrenade.OnEntitySpawned,
            WeaponEquip = HolyHandGrenade.WeaponEquip,
            WeaponPickup = HolyHandGrenade.WeaponPickup,
            GrenadeThrown = HolyHandGrenade.GrenadeThrown,
        },
    };
}
