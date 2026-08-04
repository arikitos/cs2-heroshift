using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record CarefulBulletsOptions : ISkillOptions
{
    public int DamageAfterMiss { get; init; } = 5;
}

public static class CarefulBulletsDefinition
{
    public static SkillDefinition<CarefulBulletsOptions> Create() => new()
    {
        Id = BuiltInSkillIds.CarefulBullets,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#db6c35",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new CarefulBulletsOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = CarefulBullets.LoadSkill,
            EnableSkill = CarefulBullets.EnableSkill,
            DisableSkill = CarefulBullets.DisableSkill,
            TypeSkill = CarefulBullets.TypeSkill,
            OnTakeDamage = CarefulBullets.OnTakeDamage,
            OnTick = CarefulBullets.OnTick,
            NewRound = CarefulBullets.NewRound,
            PlayerDeath = CarefulBullets.PlayerDeath,
            BulletImpact = CarefulBullets.BulletImpact,
        },
    };
}
