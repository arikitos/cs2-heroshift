using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * IlliterateOptions - typed replacement for the legacy Illiterate.SkillConfig
 * tunables (src/player/skills/Illiterate.cs). Illiterate has no skill-specific
 * tunables beyond the 11 shared SkillConfig parameters, so this record is
 * intentionally empty.
 */
public sealed record IlliterateOptions : ISkillOptions
{
}

/*
 * IlliterateDefinition - typed SkillDefinition for Illiterate. Hooks reference
 * the skill's existing public static methods directly as delegates
 * (REFACTOR.md section 23) - Illiterate.cs's hook bodies are unchanged.
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
            Rarity: utils.Rarity.Common),
        DefaultOptions = new IlliterateOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Illiterate.LoadSkill,
            NewRound = Illiterate.NewRound,
            EnableSkill = Illiterate.EnableSkill,
        },
    };
}
