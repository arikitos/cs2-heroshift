using System.Text.RegularExpressions;

namespace src.LocalizationCore;

/*
 * TranslationValidator - build/test-time checks for translation catalogs
 * (REFACTOR.md section 17). Compares an external catalog's keys and
 * placeholder sets against the embedded English baseline:
 *   - keys present in the external file but missing from English are
 *     reported (unknown external keys) - the caller decides whether that is
 *     a hard error or a warning.
 *   - keys present in English but missing from the external file are NOT an
 *     error: LocalizationService already falls back to English for them.
 *   - a key present in both, but whose placeholder set (0, {1}, ...) differs,
 *     is reported - that key would format incorrectly or throw at runtime.
 * "Placeholder" here means a numbered string.Format token; literal braces
 * that aren't {<digits>} are ignored, matching legacy formatting behavior.
 */
public static class TranslationValidator
{
    private static readonly Regex PlaceholderPattern = new(@"\{(\d+)\}", RegexOptions.Compiled);

    public static IReadOnlyList<string> FindUnknownExternalKeys(TranslationCatalog baseline, TranslationCatalog external)
    {
        var baselineKeys = baseline.Keys.ToHashSet(StringComparer.Ordinal);
        return external.Keys.Where(k => !baselineKeys.Contains(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();
    }

    public static IReadOnlyList<string> FindPlaceholderMismatches(TranslationCatalog baseline, TranslationCatalog external)
    {
        var mismatches = new List<string>();

        foreach (var key in external.Keys)
        {
            if (!baseline.TryGet(key, out var baseValue)) continue;
            external.TryGet(key, out var externalValue);

            var baselinePlaceholders = ExtractPlaceholders(baseValue);
            var externalPlaceholders = ExtractPlaceholders(externalValue);

            if (!baselinePlaceholders.SetEquals(externalPlaceholders))
                mismatches.Add(key);
        }

        return mismatches.OrderBy(k => k, StringComparer.Ordinal).ToList();
    }

    private static HashSet<int> ExtractPlaceholders(string value) =>
        PlaceholderPattern.Matches(value).Select(m => int.Parse(m.Groups[1].Value)).ToHashSet();
}
