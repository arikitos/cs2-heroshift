using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record BaseballOptions : ISkillOptions
{
    public float SpeedMultipier { get; init; } = 2f;
    public float MaxSpeed { get; init; } = 900f;
    public int DamageDeal { get; init; } = 9999;
    public int GrenadeLimit { get; init; } = 3;
}

public static class BaseballDefinition
{
    public static SkillDefinition<BaseballOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Baseball,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#2effc7",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new BaseballOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Baseball.LoadSkill,
            EnableSkill = Baseball.EnableSkill,
            DisableSkill = Baseball.DisableSkill,
            OnEntitySpawned = Baseball.OnEntitySpawned,
            OnTick = Baseball.OnTick,
            NewRound = Baseball.NewRound,
            PlayerHurt = Baseball.PlayerHurt,
            WeaponEquip = Baseball.WeaponEquip,
            WeaponPickup = Baseball.WeaponPickup,
            GrenadeThrown = Baseball.GrenadeThrown,
            DecoyStarted = Baseball.DecoyStarted,
        },
    };
}
