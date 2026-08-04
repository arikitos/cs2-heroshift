using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record SoldierOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = 1.15f;
    public float ChanceTo { get; init; } = 1.35f;
}

public static class SoldierDefinition
{
    public static SkillDefinition<SoldierOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Soldier,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#09ba00",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SoldierOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.ChanceFrom, options.ChanceTo, "chanceFrom", "chanceTo"),
        Hooks = new SkillHookSet
        {
            LoadSkill = Soldier.LoadSkill,
            EnableSkill = Soldier.EnableSkill,
            OnTakeDamage = Soldier.OnTakeDamage,
        },
    };
}
