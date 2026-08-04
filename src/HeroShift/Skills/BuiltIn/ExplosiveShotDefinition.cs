using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ExplosiveShotOptions : ISkillOptions
{
    public float Damage { get; init; } = 25f;
    public float DamageRadius { get; init; } = 210f;
    public float ChanceFrom { get; init; } = .15f;
    public float ChanceTo { get; init; } = .3f;
}

public static class ExplosiveShotDefinition
{
    public static SkillDefinition<ExplosiveShotOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ExplosiveShot,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#9c0000",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ExplosiveShotOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.ChanceFrom, options.ChanceTo, "chanceFrom", "chanceTo"),
        Hooks = new SkillHookSet
        {
            LoadSkill = ExplosiveShot.LoadSkill,
            EnableSkill = ExplosiveShot.EnableSkill,
            OnEntitySpawned = ExplosiveShot.OnEntitySpawned,
            BulletImpact = ExplosiveShot.BulletImpact,
        },
    };
}
