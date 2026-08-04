using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

namespace src.SkillsCore.Abstractions;

/*
 * SkillMetadata - the settings every skill has, regardless of what it does.
 *
 * Canonical metadata shared by every skill definition. Runtime overrides are
 * merged into this record when the immutable configuration snapshot is built.
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
