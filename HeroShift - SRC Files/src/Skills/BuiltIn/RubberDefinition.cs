using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record RubberOptions : ISkillOptions
{
    public float SlownessTime { get; init; } = 2f;
    public float SlownessModifier { get; init; } = .2f;
}

public static class RubberDefinition
{
    public static SkillDefinition<RubberOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Rubber,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8B4513",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RubberOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Rubber.LoadSkill,
            OnTick = Rubber.OnTick,
            NewRound = Rubber.NewRound,
            PlayerHurt = Rubber.PlayerHurt,
        },
    };
}
