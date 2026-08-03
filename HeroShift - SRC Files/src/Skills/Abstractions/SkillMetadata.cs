using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

namespace src.SkillsCore.Abstractions;

/*
 * SkillMetadata - the settings every skill has, regardless of what it does.
 *
 * Field-for-field equivalent to the legacy src/utils/SkillsInfo.DefaultSkillInfo
 * (see that type for the original semantics of each field). This is the typed
 * replacement for the reflection-populated base class every legacy SkillConfig
 * derives from.
 */
public sealed record SkillMetadata(
    bool Active,
    string Color,
    CsTeam OnlyTeam,
    bool DisableOnFreezeTime,
    bool NeedsTeammates,
    string RequiredPermission,
    float? HudDuration,
    float? DescriptionHudDuration,
    int MaxPerServer,
    Rarity Rarity);
