using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * SaperOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record SaperOptions : ISkillOptions
{
}

/*
 * SaperDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Saper gameplay implementation.
 */
public static class SaperDefinition
{
    public static SkillDefinition<SaperOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Saper,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8A2BE2",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SaperOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Saper.LoadSkill,
            BombBeginplant = Saper.BombBeginplant,
            BombBegindefuse = Saper.BombBegindefuse,
        },
    };
}
