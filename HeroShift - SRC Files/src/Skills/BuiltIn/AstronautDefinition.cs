using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * AstronautOptions - typed replacement for the legacy Astronaut.SkillConfig
 * tunables (src/player/skills/Astronaut.cs). Defaults transcribed verbatim
 * from that SkillConfig's constructor parameters.
 */
public sealed record AstronautOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .1f;
    public float ChanceTo { get; init; } = .7f;
}

/*
 * AstronautDefinition - typed SkillDefinition for Astronaut. Hooks reference
 * the skill's existing public static methods directly as delegates
 * (REFACTOR.md section 23) - Astronaut.cs's hook bodies are unchanged except
 * for the SkillsInfo.GetValue calls, which now read
 * SkillConfigurationResolver's typed AstronautOptions snapshot instead.
 */
public static class AstronautDefinition
{
    public static SkillDefinition<AstronautOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Astronaut,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#7E10AD",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: utils.Rarity.Common),
        DefaultOptions = new AstronautOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Astronaut.LoadSkill,
            EnableSkill = Astronaut.EnableSkill,
            NewRound = Astronaut.NewRound,
            DisableSkill = Astronaut.DisableSkill,
        },
    };
}
