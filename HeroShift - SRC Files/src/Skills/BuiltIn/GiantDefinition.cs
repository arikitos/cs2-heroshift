using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GiantOptions : ISkillOptions
{
    public float MinScale { get; init; } = 1.1f;
    public float MaxScale { get; init; } = 1.4f;
}

public static class GiantDefinition
{
    public static SkillDefinition<GiantOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Giant,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8ad3ff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new GiantOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Giant.LoadSkill,
            EnableSkill = Giant.EnableSkill,
            DisableSkill = Giant.DisableSkill,
            TypeSkill = Giant.TypeSkill,
            OnTick = Giant.OnTick,
            NewRound = Giant.NewRound,
        },
    };
}
