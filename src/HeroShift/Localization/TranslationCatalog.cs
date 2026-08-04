using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace src.LocalizationCore;

/*
 * TranslationCatalog - a flat key -> string translation map, loaded from one
 * JSON file. Field-for-field equivalent to what the legacy
 * Utilities/Localization translation dictionary held, just extracted into
 * its own loadable unit so a catalog can represent either the embedded
 * English resource or an external language file.
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
        using var stringReader = new StringReader(json);
        using var jsonReader = new JsonTextReader(stringReader);
        var root = JObject.Load(jsonReader, new JsonLoadSettings
        {
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
        });

        var entries = root.Properties().ToDictionary(
            property => property.Name,
            property => property.Value.Type == JTokenType.String
                ? property.Value.Value<string>()!
                : throw new JsonException($"Translation '{property.Name}' must be a string."),
            StringComparer.Ordinal);
        return new TranslationCatalog(entries);
    }

    public static TranslationCatalog Empty { get; } = new(new Dictionary<string, string>());

    public bool TryGet(string key, out string value) => _entries.TryGetValue(key, out value!);
}
