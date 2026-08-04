using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ReplicatorOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 15f;
    public int YourTeamDamage { get; init; } = 10;
    public int EnemyTeamDamage { get; init; } = 20;
}

public static class ReplicatorDefinition
{
    public static SkillDefinition<ReplicatorOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Replicator,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a3000b",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ReplicatorOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Replicator.LoadSkill,
            EnableSkill = Replicator.EnableSkill,
            DisableSkill = Replicator.DisableSkill,
            UseSkill = Replicator.UseSkill,
            OnTakeDamage = Replicator.OnTakeDamage,
            OnTick = Replicator.OnTick,
            NewRound = Replicator.NewRound,
        },
    };
}
