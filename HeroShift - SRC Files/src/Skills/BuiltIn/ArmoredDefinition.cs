using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ArmoredOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .65f;
    public float ChanceTo { get; init; } = .85f;
}

public static class ArmoredDefinition
{
    public static SkillDefinition<ArmoredOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Armored,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#d1430a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ArmoredOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.ChanceFrom, options.ChanceTo, "chanceFrom", "chanceTo"),
        Hooks = new SkillHookSet
        {
            LoadSkill = Armored.LoadSkill,
            EnableSkill = Armored.EnableSkill,
            OnTakeDamage = Armored.OnTakeDamage,
        },
    };
}
