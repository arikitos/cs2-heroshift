using Newtonsoft.Json;

namespace src.LocalizationCore;

/*
 * TranslationCatalog - a flat key -> string translation map, loaded from one
 * JSON file. Field-for-field equivalent to what the legacy
 * src/utils/Localization._translations dictionary held, just extracted into
 * its own loadable unit so a catalog can represent either the embedded
 * English resource or an external language file (REFACTOR.md section 16).
 */
public sealed class TranslationCatalog
{
    private readonly IReadOnlyDictionary<string, string> _entries;

    public IReadOnlyCollection<string> Keys => (IReadOnlyCollection<string>)_entries.Keys;

    private TranslationCatalog(IReadOnlyDictionary<string, string> entries)
    {
        _entries = entries;
    }

    public static TranslationCatalog FromJson(string json)
    {
        var entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
        return new TranslationCatalog(entries);
    }

    public static TranslationCatalog Empty { get; } = new(new Dictionary<string, string>());

    public bool TryGet(string key, out string value) => _entries.TryGetValue(key, out value!);
}
