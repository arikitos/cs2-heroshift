using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record AssassinOptions : ISkillOptions
{
    public float DamageMultiplier { get; init; } = 2f;
    public float ToleranceDeg { get; init; } = 45f;
}

public static class AssassinDefinition
{
    public static SkillDefinition<AssassinOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Assassin,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#d9d9d9",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AssassinOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Assassin.LoadSkill,
            OnTakeDamage = Assassin.OnTakeDamage,
        },
    };
}
