using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record LifeSwapOptions : ISkillOptions
{
}

public static class LifeSwapDefinition
{
    public static SkillDefinition<LifeSwapOptions> Create() => new()
    {
        Id = BuiltInSkillIds.LifeSwap,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a3651a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new LifeSwapOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = LifeSwap.LoadSkill,
            EnableSkill = LifeSwap.EnableSkill,
            DisableSkill = LifeSwap.DisableSkill,
            TypeSkill = LifeSwap.TypeSkill,
            OnTick = LifeSwap.OnTick,
            NewRound = LifeSwap.NewRound,
        },
    };
}
