using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GamblerOptions : ISkillOptions
{
    public int RefreshPrice { get; init; } = 150;
}

public static class GamblerDefinition
{
    public static SkillDefinition<GamblerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Gambler,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#7eff47",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new GamblerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Gambler.LoadSkill,
            EnableSkill = Gambler.EnableSkill,
            DisableSkill = Gambler.DisableSkill,
            TypeSkill = Gambler.TypeSkill,
            NewRound = Gambler.NewRound,
        },
    };
}
