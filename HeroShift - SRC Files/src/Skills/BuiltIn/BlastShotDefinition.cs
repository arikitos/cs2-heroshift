using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record BlastShotOptions : ISkillOptions
{
    public float ExplosionRadius { get; init; } = 400.0f;
    public int ExplosionDamage { get; init; } = 60;
    public float DmgReductionForTeamates { get; init; } = 0.5f;
    public float Cooldown { get; init; } = 10f;
    public float Force { get; init; } = 1000f;
}

public static class BlastShotDefinition
{
    public static SkillDefinition<BlastShotOptions> Create() => new()
    {
        Id = BuiltInSkillIds.BlastShot,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#7740c9",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new BlastShotOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = BlastShot.LoadSkill,
            EnableSkill = BlastShot.EnableSkill,
            DisableSkill = BlastShot.DisableSkill,
            OnTakeDamage = BlastShot.OnTakeDamage,
            OnEntitySpawned = BlastShot.OnEntitySpawned,
            OnTick = BlastShot.OnTick,
            NewRound = BlastShot.NewRound,
        },
    };
}
