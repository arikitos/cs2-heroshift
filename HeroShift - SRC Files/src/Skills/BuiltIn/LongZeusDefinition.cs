using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record LongZeusOptions : IMaxDistanceOptions
{
    public float MaxDistance { get; init; } = 4096f;
    public bool FriendlyFire { get; init; } = false;
}

public static class LongZeusDefinition
{
    public static SkillDefinition<LongZeusOptions> Create() => new()
    {
        Id = BuiltInSkillIds.LongZeus,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#6effc7",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Uncommon),
        DefaultOptions = new LongZeusOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = LongZeus.LoadSkill,
            EnableSkill = LongZeus.EnableSkill,
        },
    };
}
