using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record WeightlessOptions : ISkillOptions
{
    public int GrenadeLimit { get; init; } = 2;
}

public static class WeightlessDefinition
{
    public static SkillDefinition<WeightlessOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Weightless,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8f6dc9",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new WeightlessOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Weightless.LoadSkill,
            EnableSkill = Weightless.EnableSkill,
            DisableSkill = Weightless.DisableSkill,
            OnEntitySpawned = Weightless.OnEntitySpawned,
            OnTick = Weightless.OnTick,
            NewRound = Weightless.NewRound,
            WeaponEquip = Weightless.WeaponEquip,
            WeaponPickup = Weightless.WeaponPickup,
            GrenadeThrown = Weightless.GrenadeThrown,
        },
    };
}
