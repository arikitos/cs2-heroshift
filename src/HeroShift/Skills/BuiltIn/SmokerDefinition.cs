using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record SmokerOptions : ISkillOptions
{
    public int GrenadeLimit { get; init; } = 1;
}

public static class SmokerDefinition
{
    public static SkillDefinition<SmokerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Smoker,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#b5ab8f",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SmokerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Smoker.LoadSkill,
            EnableSkill = Smoker.EnableSkill,
            DisableSkill = Smoker.DisableSkill,
            NewRound = Smoker.NewRound,
            WeaponEquip = Smoker.WeaponEquip,
            WeaponPickup = Smoker.WeaponPickup,
            GrenadeThrown = Smoker.GrenadeThrown,
            SmokegrenadeDetonate = Smoker.SmokegrenadeDetonate,
        },
    };
}
