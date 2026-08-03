using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * ShortBombOptions - typed replacement for the legacy ShortBomb.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record ShortBombOptions : ISkillOptions
{
    public int DetonationTime { get; init; } = 20;
}

/*
 * ShortBombDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing ShortBomb gameplay implementation.
 */
public static class ShortBombDefinition
{
    public static SkillDefinition<ShortBombOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ShortBomb,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#f5b74c",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ShortBombOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ShortBomb.LoadSkill,
            EnableSkill = ShortBomb.EnableSkill,
            NewRound = ShortBomb.NewRound,
            BombPlanted = ShortBomb.BombPlanted,
        },
    };
}
