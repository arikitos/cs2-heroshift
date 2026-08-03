using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * FastReloadOptions - typed replacement for the legacy FastReload.SkillConfig
 * tunables (src/player/skills/FastReload.cs). This skill has no
 * skill-specific tunables beyond the shared settings.
 */
public sealed record FastReloadOptions : ISkillOptions
{
}

/*
 * FastReloadDefinition - typed SkillDefinition for FastReload. Hooks reference
 * the skill's existing public static methods directly as delegates
 * (REFACTOR.md section 23) - FastReload.cs's hook bodies are unchanged
 * (it has no SkillsInfo.GetValue calls outside of LoadSkill's legacy color
 * lookup).
 */
public static class FastReloadDefinition
{
    public static SkillDefinition<FastReloadOptions> Create() => new()
    {
        Id = BuiltInSkillIds.FastReload,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffc061",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: utils.Rarity.Common),
        DefaultOptions = new FastReloadOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = FastReload.LoadSkill,
            UseSkill = FastReload.UseSkill,
        },
    };
}
