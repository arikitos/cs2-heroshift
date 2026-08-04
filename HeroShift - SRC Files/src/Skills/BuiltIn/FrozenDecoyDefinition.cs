using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record FrozenDecoyOptions : ISkillOptions
{
    public float TriggerRadius { get; init; } = 180;
    public int SlownessMultiplier { get; init; } = 5;
    public int GrenadeLimit { get; init; } = 3;
}

public static class FrozenDecoyDefinition
{
    public static SkillDefinition<FrozenDecoyOptions> Create() => new()
    {
        Id = BuiltInSkillIds.FrozenDecoy,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#00eaff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new FrozenDecoyOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = FrozenDecoy.LoadSkill,
            EnableSkill = FrozenDecoy.EnableSkill,
            DisableSkill = FrozenDecoy.DisableSkill,
            OnTick = FrozenDecoy.OnTick,
            NewRound = FrozenDecoy.NewRound,
            WeaponEquip = FrozenDecoy.WeaponEquip,
            WeaponPickup = FrozenDecoy.WeaponPickup,
            GrenadeThrown = FrozenDecoy.GrenadeThrown,
            DecoyStarted = FrozenDecoy.DecoyStarted,
            DecoyDetonate = FrozenDecoy.DecoyDetonate,
        },
    };
}
