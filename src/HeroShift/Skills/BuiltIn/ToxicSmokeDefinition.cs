using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ToxicSmokeOptions : ISkillOptions
{
    public int SmokeDamage { get; init; } = 2;
    public float SmokeRadius { get; init; } = 180;
    public int TickCooldown { get; init; } = 17;
    public int GrenadeLimit { get; init; } = 1;
}

public static class ToxicSmokeDefinition
{
    public static SkillDefinition<ToxicSmokeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ToxicSmoke,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#507529",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ToxicSmokeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ToxicSmoke.LoadSkill,
            EnableSkill = ToxicSmoke.EnableSkill,
            DisableSkill = ToxicSmoke.DisableSkill,
            OnEntitySpawned = ToxicSmoke.OnEntitySpawned,
            OnTick = ToxicSmoke.OnTick,
            NewRound = ToxicSmoke.NewRound,
            WeaponEquip = ToxicSmoke.WeaponEquip,
            WeaponPickup = ToxicSmoke.WeaponPickup,
            GrenadeThrown = ToxicSmoke.GrenadeThrown,
            SmokegrenadeDetonate = ToxicSmoke.SmokegrenadeDetonate,
            SmokegrenadeExpired = ToxicSmoke.SmokegrenadeExpired,
        },
    };
}
