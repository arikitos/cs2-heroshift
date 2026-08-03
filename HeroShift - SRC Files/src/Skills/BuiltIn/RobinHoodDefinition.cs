using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * RobinHoodOptions - typed replacement for the legacy RobinHood.SkillConfig
 * tunables (src/player/skills/RobinHood.cs). Defaults transcribed verbatim
 * from that SkillConfig's constructor parameters.
 */
public sealed record RobinHoodOptions : ISkillOptions
{
    public int MoneyMultiplier { get; init; } = 35;
}

/*
 * RobinHoodDefinition - typed SkillDefinition for RobinHood. Hooks reference
 * the skill's existing public static methods directly as delegates
 * (REFACTOR.md section 23) - RobinHood.cs's hook bodies are unchanged except
 * for the SkillsInfo.GetValue calls, which now read
 * SkillConfigurationResolver's typed RobinHoodOptions snapshot instead.
 */
public static class RobinHoodDefinition
{
    public static SkillDefinition<RobinHoodOptions> Create() => new()
    {
        Id = BuiltInSkillIds.RobinHood,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#119125",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: utils.Rarity.Common),
        DefaultOptions = new RobinHoodOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = RobinHood.LoadSkill,
            PlayerHurt = RobinHood.PlayerHurt,
        },
    };
}
