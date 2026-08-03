using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * FragileBombOptions - typed replacement for the legacy FragileBomb.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record FragileBombOptions : ISkillOptions
{
    public int MaxBombHealth { get; init; } = 1000;
}

/*
 * FragileBombDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing FragileBomb gameplay implementation.
 */
public static class FragileBombDefinition
{
    public static SkillDefinition<FragileBombOptions> Create() => new()
    {
        Id = BuiltInSkillIds.FragileBomb,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#5d00ff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.CounterTerrorist,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new FragileBombOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = FragileBomb.LoadSkill,
            NewRound = FragileBomb.NewRound,
            BulletImpact = FragileBomb.BulletImpact,
            BombPlanted = FragileBomb.BombPlanted,
        },
    };
}
