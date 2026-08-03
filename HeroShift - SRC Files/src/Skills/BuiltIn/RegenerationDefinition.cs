using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record RegenerationOptions : ISkillOptions
{
    public int HealthToAdd { get; init; } = 1;
    public float Cooldown { get; init; } = .25f;
}

public static class RegenerationDefinition
{
    public static SkillDefinition<RegenerationOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Regeneration,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff462e",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RegenerationOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Regeneration.LoadSkill,
            OnTick = Regeneration.OnTick,
        },
    };
}
