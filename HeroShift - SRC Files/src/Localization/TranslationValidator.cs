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

            if (!TryExtractPlaceholders(baseValue, out var baselinePlaceholders, out _) ||
                !TryExtractPlaceholders(externalValue, out var externalPlaceholders, out _))
                continue;

            if (!baselinePlaceholders.SetEquals(externalPlaceholders))
                mismatches.Add(key);
        }

        return mismatches.OrderBy(k => k, StringComparer.Ordinal).ToList();
    }

    public static IReadOnlyList<string> FindMalformedFormatStrings(TranslationCatalog catalog)
    {
        var malformed = new List<string>();
        foreach (var key in catalog.Keys)
        {
            catalog.TryGet(key, out var value);
            if (!TryExtractPlaceholders(value, out _, out var error))
                malformed.Add($"{key}: {error}");
        }

        return malformed.OrderBy(value => value, StringComparer.Ordinal).ToList();
    }

    private static bool TryExtractPlaceholders(string value, out HashSet<string> placeholders, out string? error)
    {
        placeholders = new HashSet<string>(StringComparer.Ordinal);
        error = null;

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] == '{')
            {
                if (index + 1 < value.Length && value[index + 1] == '{')
                {
                    index++;
                    continue;
                }

                int close = value.IndexOf('}', index + 1);
                if (close < 0)
                {
                    error = "unclosed opening brace";
                    return false;
                }

                string token = value[(index + 1)..close];
                int separator = token.IndexOfAny([',', ':']);
                string name = (separator < 0 ? token : token[..separator]).Trim();
                bool numbered = name.Length > 0 && name.All(char.IsAsciiDigit);
                bool named = name.Length > 0 && name.All(character => char.IsAsciiLetter(character) || character == '_');
                if (!numbered && !named)
                {
                    error = $"invalid placeholder '{{{token}}}'";
                    return false;
                }

                placeholders.Add(name);
                index = close;
            }
            else if (value[index] == '}')
            {
                if (index + 1 < value.Length && value[index + 1] == '}')
                {
                    index++;
                    continue;
                }

                error = "unmatched closing brace";
                return false;
            }
        }

        return true;
    }
}
