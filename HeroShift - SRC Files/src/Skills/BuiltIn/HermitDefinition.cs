using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record HermitOptions : ISkillOptions
{
    public int HealthToAdd { get; init; } = 100;
    public float EffectDuration { get; init; } = 1.0f;
}

public static class HermitDefinition
{
    public static SkillDefinition<HermitOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Hermit,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ded678",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new HermitOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Hermit.LoadSkill,
            PlayerDeath = Hermit.PlayerDeath,
        },
    };
}
