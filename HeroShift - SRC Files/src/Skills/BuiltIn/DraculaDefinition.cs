using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * DraculaOptions - typed replacement for the legacy Dracula.SkillConfig tunables
 * (src/player/skills/Dracula.cs). Defaults transcribed verbatim from that
 * SkillConfig's constructor parameters.
 */
public sealed record DraculaOptions : ISkillOptions
{
    public float HealthRegainScale { get; init; } = .3f;
}

/*
 * DraculaDefinition - typed SkillDefinition for Dracula. Hooks reference the
 * skill's existing public static methods directly as delegates (REFACTOR.md
 * section 23) - Dracula.cs's hook bodies are unchanged except for the
 * SkillsInfo.GetValue calls, which now read SkillConfigurationResolver's
 * typed DraculaOptions snapshot instead.
 */
public static class DraculaDefinition
{
    public static SkillDefinition<DraculaOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Dracula,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FA050D",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: utils.Rarity.Common),
        DefaultOptions = new DraculaOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Dracula.LoadSkill,
            DisableSkill = Dracula.DisableSkill,
            PlayerHurt = Dracula.PlayerHurt,
        },
    };
}
