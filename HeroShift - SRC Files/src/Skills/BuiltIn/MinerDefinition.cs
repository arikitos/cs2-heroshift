using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record MinerOptions : ISkillOptions
{
    public float DetonationRange { get; init; } = 130;
    public int GrenadeLimit { get; init; } = 3;
}

public static class MinerDefinition
{
    public static SkillDefinition<MinerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Miner,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#adf542",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new MinerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Miner.LoadSkill,
            EnableSkill = Miner.EnableSkill,
            DisableSkill = Miner.DisableSkill,
            OnEntitySpawned = Miner.OnEntitySpawned,
            OnTick = Miner.OnTick,
            NewRound = Miner.NewRound,
            WeaponEquip = Miner.WeaponEquip,
            WeaponPickup = Miner.WeaponPickup,
            GrenadeThrown = Miner.GrenadeThrown,
        },
    };
}
