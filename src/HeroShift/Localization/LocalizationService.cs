using System.Collections.Concurrent;
using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.LocalizationCore;

/*
 * LocalizationService - typed replacement for the static
 * Utilities/Localization facade (see that file for the fully-documented
 * original semantics; every behavior below is preserved verbatim).
 *
 * Catalog fallback chain:
 *   external selected-language file (languages/<code>.json)
 *     -> embedded English resource (this DLL's src.Localization.Resources.en.json)
 *     -> the lookup key itself.
 * The embedded English catalog always loads; an external file is optional and,
 * when present, is consulted FIRST for the same key so operators can override
 * or fully replace individual strings without needing a full translation.
 *
 * Illiterate remains a direct gameplay dependency because its text scrambling
 * is intentionally applied after fallback and formatting.
 */
public sealed class LocalizationService : ILocalizationService
{
    private readonly string? _externalLanguagePath;
    private readonly string? _alternativeSkillButton;

    private TranslationCatalog _external = TranslationCatalog.Empty;
    private TranslationCatalog _embeddedEnglish = TranslationCatalog.Empty;

    public string SourceDescription => _external.Keys.Count > 0
        ? $"external override '{_externalLanguagePath}' with embedded English fallback"
        : "embedded English";

    // Chat colours are control characters that cannot be typed into JSON, so the
    // literal token "CHATCOLORS.RED" is swapped for the real character at load
    // time - preserved verbatim from the legacy LoadLanguage().
    private static readonly string RedColorToken = ChatColors.Red.ToString();

    public LocalizationService(string? externalLanguagePath, string? alternativeSkillButton)
    {
        _externalLanguagePath = externalLanguagePath;
        _alternativeSkillButton = alternativeSkillButton;
        Reload();
    }

    public void Reload()
    {
        _embeddedEnglish = LoadEmbeddedEnglish();
        _external = LoadExternal(_externalLanguagePath);
        ValidateCatalogs();
    }

    private TranslationCatalog LoadEmbeddedEnglish()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("src.Localization.Resources.en.json");
        if (stream == null) return TranslationCatalog.Empty;

        using var reader = new StreamReader(stream);
        return ApplyLoadTimeSubstitutions(TranslationCatalog.FromJson(reader.ReadToEnd()));
    }

    private TranslationCatalog LoadExternal(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return TranslationCatalog.Empty;

        return ApplyLoadTimeSubstitutions(TranslationCatalog.FromJson(File.ReadAllText(path)));
    }

    private void ValidateCatalogs()
    {
        var errors = new List<string>();
        errors.AddRange(TranslationValidator.FindMalformedFormatStrings(_embeddedEnglish)
            .Select(error => $"embedded English {error}"));

        foreach (var skill in BuiltInSkillIds.All)
        {
            if (!_embeddedEnglish.TryGet(skill.Value, out _))
                errors.Add($"embedded English is missing skill name '{skill.Value}'");
            if (!_embeddedEnglish.TryGet($"{skill.Value}_desc", out _))
                errors.Add($"embedded English is missing skill description '{skill.Value}_desc'");
        }

        if (_external.Keys.Count > 0)
        {
            errors.AddRange(TranslationValidator.FindMalformedFormatStrings(_external)
                .Select(error => $"external language {error}"));
            errors.AddRange(TranslationValidator.FindUnknownExternalKeys(_embeddedEnglish, _external)
                .Select(key => $"external language contains unknown key '{key}'"));
            errors.AddRange(TranslationValidator.FindPlaceholderMismatches(_embeddedEnglish, _external)
                .Select(key => $"external language placeholder mismatch at '{key}'"));
        }

        if (errors.Count > 0)
            throw new InvalidDataException("HeroShift localization validation failed. " + string.Join(" ", errors));
    }

    // "CHATCOLORS.RED" -> the real control character; "css_useSkill" -> both
    // bind forms when an alternative skill button is configured. Identical to
    // the legacy per-key substitution loop in Localization.LoadLanguage().
    private TranslationCatalog ApplyLoadTimeSubstitutions(TranslationCatalog catalog)
    {
        if (string.IsNullOrEmpty(_alternativeSkillButton))
            return ReplaceInAll(catalog, "CHATCOLORS.RED", RedColorToken);

        var withColor = ReplaceInAll(catalog, "CHATCOLORS.RED", RedColorToken);
        return ReplaceInAll(withColor, "css_useSkill", $"css_useSkill/{_alternativeSkillButton}");
    }

    private static TranslationCatalog ReplaceInAll(TranslationCatalog catalog, string oldValue, string newValue)
    {
        var replaced = new Dictionary<string, string>();
        foreach (var key in catalog.Keys)
        {
            catalog.TryGet(key, out var value);
            replaced[key] = value.Replace(oldValue, newValue);
        }
        return TranslationCatalog.FromJson(Newtonsoft.Json.JsonConvert.SerializeObject(replaced));
    }

    // The single lookup every helper funnels into - same fallback order and
    // sentinel/formatting/illiterate behavior as legacy Localization.GetTranslation.
    public string GetTranslation(string key, CCSPlayerController? player = null, params object[] args)
    {
        if (!TryResolve(key, out var translation))
            return key;

        // Sentinel, not a real argument: callers pass literal "welcome" to get
        // the text back UNFORMATTED (it holds its own {PLAYER} placeholder that
        // the caller substitutes itself, which string.Format would throw on).
        if (args.Length != 0 && args[0].ToString() == "welcome")
            return translation;

        string output = args.Length == 0 ? translation : string.Format(translation, args);

        if (Illiterate.CheckIlliterateSkill(player))
            return Illiterate.GetRandomText(output)!;

        return output;
    }

    private bool TryResolve(string key, out string value)
    {
        if (_external.TryGet(key, out value!)) return true;
        if (_embeddedEnglish.TryGet(key, out value!)) return true;

        value = key;
        return false;
    }

    public void PrintTranslationToChatAll(string message, string[]? key, params object[][]? args)
    {
        foreach (var player in CounterStrikeSharp.API.Utilities.GetPlayers().Where(p => !p.IsBot))
        {
            if (key == null)
            {
                player.PrintToChat(message);
                continue;
            }

            List<string> translations = [];
            for (int i = 0; i < key.Length; i++)
            {
                object[] currentArgs = args != null && i < args.Length ? args[i] : [];
                translations.Add(GetTranslation(key[i], null, currentArgs));
            }
            player.PrintToChat(string.Format(message, [.. translations]));
        }
    }
}
