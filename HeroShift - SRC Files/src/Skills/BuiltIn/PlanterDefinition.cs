using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record PlanterOptions : ISkillOptions
{
    public int ExtraC4BlowTime { get; init; } = 60;
}

public static class PlanterDefinition
{
    public static SkillDefinition<PlanterOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Planter,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#7d7d7d",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PlanterOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Planter.LoadSkill,
            EnableSkill = Planter.EnableSkill,
            DisableSkill = Planter.DisableSkill,
            OnTick = Planter.OnTick,
            NewRound = Planter.NewRound,
            BombBeginplant = Planter.BombBeginplant,
            BombAbortplant = Planter.BombAbortplant,
            BombPlanted = Planter.BombPlanted,
        },
    };
}
