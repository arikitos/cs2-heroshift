using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record HealingSmokeOptions : ISkillOptions
{
    public int SmokeHeal { get; init; } = 1;
    public float SmokeRadius { get; init; } = 180;
    public int TickCooldown { get; init; } = 16;
    public int GrenadeLimit { get; init; } = 1;
}

public static class HealingSmokeDefinition
{
    public static SkillDefinition<HealingSmokeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.HealingSmoke,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#1fe070",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new HealingSmokeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = HealingSmoke.LoadSkill,
            EnableSkill = HealingSmoke.EnableSkill,
            DisableSkill = HealingSmoke.DisableSkill,
            OnEntitySpawned = HealingSmoke.OnEntitySpawned,
            OnTick = HealingSmoke.OnTick,
            NewRound = HealingSmoke.NewRound,
            WeaponEquip = HealingSmoke.WeaponEquip,
            WeaponPickup = HealingSmoke.WeaponPickup,
            GrenadeThrown = HealingSmoke.GrenadeThrown,
            SmokegrenadeDetonate = HealingSmoke.SmokegrenadeDetonate,
            SmokegrenadeExpired = HealingSmoke.SmokegrenadeExpired,
        },
    };
}
