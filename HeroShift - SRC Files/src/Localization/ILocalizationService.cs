using CounterStrikeSharp.API.Core;

namespace src.LocalizationCore;

/*
 * ILocalizationService - typed replacement for the static
 * src/utils/Localization class. Behavior (see that file for the original,
 * fully-documented semantics) must match exactly:
 *   - Unknown keys return the key itself (never throw) - a missing
 *     translation is visible as raw key text rather than crashing a hook.
 *   - "CHATCOLORS.RED" and "css_useSkill" load-time substitutions.
 *   - The "welcome" sentinel arg returns the raw unformatted translation.
 *   - Illiterate players get every formatted string scrambled and never
 *     cached (see IIlliterateTextScrambler).
 *   - Percentage values in a '%'-bearing string are rendered as whole
 *     percent (0.35 -> 35), not the raw fraction.
 * Fallback order: external selected language -> embedded English -> key.
 */
public interface ILocalizationService
{
    // Re-reads every catalog and clears caches - called on plugin load and
    // !reload, matching legacy Localization.Load() semantics.
    void Reload();

    string GetTranslation(string key, CCSPlayerController? player = null, params object[] args);

    void PrintTranslationToChatAll(string message, string[]? key, params object[][]? args);
}
