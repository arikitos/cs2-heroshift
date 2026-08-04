using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DeathBombOptions : ISkillOptions
{
    public float ExplosionRadius { get; init; } = 500.0f;
    public int ExplosionDamage { get; init; } = 999;
    public float DmgReductionForTeamates { get; init; } = 0.5f;
}

public static class DeathBombDefinition
{
    public static SkillDefinition<DeathBombOptions> Create() => new()
    {
        Id = BuiltInSkillIds.DeathBomb,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#F5CB42",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DeathBombOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = DeathBomb.LoadSkill,
            OnTakeDamage = DeathBomb.OnTakeDamage,
            OnEntitySpawned = DeathBomb.OnEntitySpawned,
            NewRound = DeathBomb.NewRound,
            PlayerDeath = DeathBomb.PlayerDeath,
        },
    };
}
