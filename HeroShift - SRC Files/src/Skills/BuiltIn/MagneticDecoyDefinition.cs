using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record MagneticDecoyOptions : ISkillOptions
{
    public float TriggerRadius { get; init; } = 180;
    public float Strenght { get; init; } = 30;
    public int GrenadeLimit { get; init; } = 3;
}

public static class MagneticDecoyDefinition
{
    public static SkillDefinition<MagneticDecoyOptions> Create() => new()
    {
        Id = BuiltInSkillIds.MagneticDecoy,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#81f0c4",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new MagneticDecoyOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = MagneticDecoy.LoadSkill,
            EnableSkill = MagneticDecoy.EnableSkill,
            DisableSkill = MagneticDecoy.DisableSkill,
            OnTick = MagneticDecoy.OnTick,
            NewRound = MagneticDecoy.NewRound,
            WeaponEquip = MagneticDecoy.WeaponEquip,
            WeaponPickup = MagneticDecoy.WeaponPickup,
            GrenadeThrown = MagneticDecoy.GrenadeThrown,
            DecoyStarted = MagneticDecoy.DecoyStarted,
            DecoyDetonate = MagneticDecoy.DecoyDetonate,
        },
    };
}
