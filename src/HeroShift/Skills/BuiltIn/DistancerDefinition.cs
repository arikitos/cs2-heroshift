using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DistancerOptions : ISkillOptions
{
}

public static class DistancerDefinition
{
    public static SkillDefinition<DistancerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Distancer,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#00f2ff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DistancerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Distancer.LoadSkill,
            EnableSkill = Distancer.EnableSkill,
            DisableSkill = Distancer.DisableSkill,
            OnTick = Distancer.OnTick,
            NewRound = Distancer.NewRound,
        },
    };
}
