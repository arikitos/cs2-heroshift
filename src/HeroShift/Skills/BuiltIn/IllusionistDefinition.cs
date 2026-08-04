using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record IllusionistOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30f;
    public float DurationRun { get; init; } = 5;
    public float DurationCrouch { get; init; } = 12;
    public int YourTeamDamage { get; init; } = 10;
    public int EnemyTeamDamage { get; init; } = 20;
}

public static class IllusionistDefinition
{
    public static SkillDefinition<IllusionistOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Illusionist,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#42f5ef",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new IllusionistOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Illusionist.LoadSkill,
            EnableSkill = Illusionist.EnableSkill,
            DisableSkill = Illusionist.DisableSkill,
            UseSkill = Illusionist.UseSkill,
            OnTakeDamage = Illusionist.OnTakeDamage,
            OnTick = Illusionist.OnTick,
            NewRound = Illusionist.NewRound,
        },
    };
}
