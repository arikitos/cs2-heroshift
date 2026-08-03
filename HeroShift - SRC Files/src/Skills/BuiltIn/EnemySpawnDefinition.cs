using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record EnemySpawnOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 15f;
    public float CooldownBeforeUse { get; init; } = 10f;
}

public static class EnemySpawnDefinition
{
    public static SkillDefinition<EnemySpawnOptions> Create() => new()
    {
        Id = BuiltInSkillIds.EnemySpawn,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff8c92",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new EnemySpawnOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = EnemySpawn.LoadSkill,
            EnableSkill = EnemySpawn.EnableSkill,
            DisableSkill = EnemySpawn.DisableSkill,
            UseSkill = EnemySpawn.UseSkill,
            OnTick = EnemySpawn.OnTick,
            NewRound = EnemySpawn.NewRound,
        },
    };
}
