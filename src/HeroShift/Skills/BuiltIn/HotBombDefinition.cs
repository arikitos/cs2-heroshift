using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record HotBombOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 1;
    public int Damage { get; init; } = 2;
}

public static class HotBombDefinition
{
    public static SkillDefinition<HotBombOptions> Create() => new()
    {
        Id = BuiltInSkillIds.HotBomb,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#baf081",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.CounterTerrorist,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new HotBombOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = HotBomb.LoadSkill,
            EnableSkill = HotBomb.EnableSkill,
            DisableSkill = HotBomb.DisableSkill,
            OnTick = HotBomb.OnTick,
            NewRound = HotBomb.NewRound,
            PlayerDeath = HotBomb.PlayerDeath,
            WeaponPickup = HotBomb.WeaponPickup,
        },
    };
}
