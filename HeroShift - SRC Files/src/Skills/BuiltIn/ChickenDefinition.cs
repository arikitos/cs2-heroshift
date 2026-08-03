using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ChickenOptions : ISkillOptions
{
}

public static class ChickenDefinition
{
    public static SkillDefinition<ChickenOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Chicken,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FF8B42",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ChickenOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Chicken.LoadSkill,
            EnableSkill = Chicken.EnableSkill,
            DisableSkill = Chicken.DisableSkill,
            OnTick = Chicken.OnTick,
            NewRound = Chicken.NewRound,
            WeaponPickup = Chicken.WeaponPickup,
        },
    };
}
