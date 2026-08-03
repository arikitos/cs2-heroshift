using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record CypherOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30;
}

public static class CypherDefinition
{
    public static SkillDefinition<CypherOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Cypher,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#34ebd5",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new CypherOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Cypher.LoadSkill,
            EnableSkill = Cypher.EnableSkill,
            DisableSkill = Cypher.DisableSkill,
            UseSkill = Cypher.UseSkill,
            OnTakeDamage = Cypher.OnTakeDamage,
            OnTick = Cypher.OnTick,
            NewRound = Cypher.NewRound,
        },
    };
}
