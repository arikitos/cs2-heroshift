using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record MoneySwapOptions : ISkillOptions
{
}

public static class MoneySwapDefinition
{
    public static SkillDefinition<MoneySwapOptions> Create() => new()
    {
        Id = BuiltInSkillIds.MoneySwap,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#52f54c",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new MoneySwapOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = MoneySwap.LoadSkill,
            EnableSkill = MoneySwap.EnableSkill,
            DisableSkill = MoneySwap.DisableSkill,
            TypeSkill = MoneySwap.TypeSkill,
            OnTick = MoneySwap.OnTick,
            NewRound = MoneySwap.NewRound,
        },
    };
}
