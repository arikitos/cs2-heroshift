using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record BunnyHopOptions : ISkillOptions
{
    public float MaxSpeed { get; init; } = 500f;
    public float JumpVelocity { get; init; } = 300f;
    public float JumpBoost { get; init; } = 2f;
}

public static class BunnyHopDefinition
{
    public static SkillDefinition<BunnyHopOptions> Create() => new()
    {
        Id = BuiltInSkillIds.BunnyHop,
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
        DefaultOptions = new BunnyHopOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = BunnyHop.LoadSkill,
            OnTick = BunnyHop.OnTick,
            NewRound = BunnyHop.NewRound,
        },
    };
}
