using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ExplodingBarrelOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 20f;
    public float ExplosionRadius { get; init; } = 600f;
    public int ExplosionDamage { get; init; } = 50;
    public string PropModel { get; init; } = "models/props/de_train/hr_t/barrel_a/barrel_a.vmdl";
    public float DmgReductionForTeamates { get; init; } = 0.5f;
}

public static class ExplodingBarrelDefinition
{
    public static SkillDefinition<ExplodingBarrelOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ExplodingBarrel,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#c0392b",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ExplodingBarrelOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ExplodingBarrel.LoadSkill,
            EnableSkill = ExplodingBarrel.EnableSkill,
            DisableSkill = ExplodingBarrel.DisableSkill,
            UseSkill = ExplodingBarrel.UseSkill,
            OnTakeDamage = ExplodingBarrel.OnTakeDamage,
            OnEntitySpawned = ExplodingBarrel.OnEntitySpawned,
            OnTick = ExplodingBarrel.OnTick,
            NewRound = ExplodingBarrel.NewRound,
        },
    };
}
