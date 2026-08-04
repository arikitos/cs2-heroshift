using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GlazOptions : ISkillOptions
{
    public int GrenadeLimit { get; init; } = 2;
}

public static class GlazDefinition
{
    public static SkillDefinition<GlazOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Glaz,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#5d00ff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new GlazOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Glaz.LoadSkill,
            EnableSkill = Glaz.EnableSkill,
            DisableSkill = Glaz.DisableSkill,
            CheckTransmit = Glaz.CheckTransmit,
            NewRound = Glaz.NewRound,
            WeaponEquip = Glaz.WeaponEquip,
            WeaponPickup = Glaz.WeaponPickup,
            GrenadeThrown = Glaz.GrenadeThrown,
            SmokegrenadeDetonate = Glaz.SmokegrenadeDetonate,
            SmokegrenadeExpired = Glaz.SmokegrenadeExpired,
        },
    };
}
