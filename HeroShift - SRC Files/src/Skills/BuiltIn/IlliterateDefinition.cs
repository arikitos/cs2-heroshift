using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * IlliterateOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record IlliterateOptions : ISkillOptions
{
}

/*
 * IlliterateDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Illiterate gameplay implementation.
 */
public static class IlliterateDefinition
{
    public static SkillDefinition<IlliterateOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Illiterate,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#1466F5",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new IlliterateOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Illiterate.LoadSkill,
            EnableSkill = Illiterate.EnableSkill,
            NewRound = Illiterate.NewRound,
        },
    };
}
