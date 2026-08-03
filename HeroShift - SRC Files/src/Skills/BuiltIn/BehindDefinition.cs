using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * BehindOptions - typed replacement for the legacy Behind.SkillConfig
 * tunables (src/player/skills/Behind.cs). Defaults transcribed verbatim
 * from that SkillConfig's constructor parameters.
 */
public sealed record BehindOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .2f;
    public float ChanceTo { get; init; } = .4f;
}

/*
 * BehindDefinition - typed SkillDefinition for Behind. Hooks reference the
 * skill's existing public static methods directly as delegates (REFACTOR.md
 * section 23) - Behind.cs's hook bodies are unchanged except for the
 * SkillsInfo.GetValue calls, which now read SkillConfigurationResolver's
 * typed BehindOptions snapshot instead.
 */
public static class BehindDefinition
{
    public static SkillDefinition<BehindOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Behind,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#00FF00",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: utils.Rarity.Common),
        DefaultOptions = new BehindOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Behind.LoadSkill,
            PlayerHurt = Behind.PlayerHurt,
            EnableSkill = Behind.EnableSkill,
        },
    };
}
