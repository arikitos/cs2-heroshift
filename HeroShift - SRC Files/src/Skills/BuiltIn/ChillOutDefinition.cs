using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ChillOutOptions : ISkillOptions
{
    public float BombArmedTime { get; init; } = 10f;
}

public static class ChillOutDefinition
{
    public static SkillDefinition<ChillOutOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ChillOut,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#343deb",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.CounterTerrorist,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ChillOutOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ChillOut.LoadSkill,
            DisableSkill = ChillOut.DisableSkill,
            OnTick = ChillOut.OnTick,
            NewRound = ChillOut.NewRound,
            BombBeginplant = ChillOut.BombBeginplant,
            BombAbortplant = ChillOut.BombAbortplant,
            BombPlanted = ChillOut.BombPlanted,
        },
    };
}
