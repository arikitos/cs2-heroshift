using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace src.Configuration;

/*
 * JsonMerge - small helpers shared by ConfigurationLoader for turning a
 * heroshift.json section (a JObject of overrides) into a fully-populated
 * typed record, and for surfacing unknown/mistyped fields with a JSON path
 * (REFACTOR.md section 14).
 */
public static class JsonMerge
{
    // heroshift.json uses camelCase property names throughout (matching every
    // example in REFACTOR.md, e.g. "gameMode", "cooldownSeconds"), while the C#
    // model properties are PascalCase. JObject.Merge matches properties by exact
    // key, so serializing defaults without this contract produces sibling
    // "healCommand"/"HealCommand" keys instead of merging into the same one -
    // silently leaving the override never applied. This contract is the fix.
    // ObjectCreationHandling.Replace is required alongside the contract resolver:
    // without it, Newtonsoft deserializes a nested reference-type property (e.g.
    // CommandOptions.HealCommand, itself a record with a non-null default value)
    // by populating INTO that existing default instance rather than constructing a
    // fresh one - hitting the exact same non-growable-collection truncation bug as
    // JsonSerializer.Populate (see MergeOnto's remarks) one level deeper, silently
    // dropping array overrides on any nested command/voting definition.
    private static readonly JsonSerializer CamelCaseSerializer = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    // Merges `overrideSection` onto `defaults` and deserializes a fresh TDefault
    // from the result. Deliberately does NOT use Newtonsoft's JsonSerializer.Populate:
    // Populate merges INTO existing nested objects/collections property-by-property,
    // which silently truncates array overrides shorter/longer than the default (e.g.
    // overriding a 1-item alias list with a 2-item list only updates index 0 and
    // drops the rest, since IReadOnlyList<string> isn't a growable collection
    // Populate can append to). JObject.Merge with array Replace instead treats any
    // property present in the override as a full replacement of that value, and
    // anything absent keeps the serialized default untouched.
    public static TDefault MergeOnto<TDefault>(JObject? overrideSection, TDefault defaults) where TDefault : class
    {
        if (overrideSection == null) return defaults;

        var mergedJson = JObject.FromObject(defaults, CamelCaseSerializer);
        mergedJson.Merge(overrideSection, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Merge,
        });

        return mergedJson.ToObject<TDefault>(CamelCaseSerializer) ?? defaults;
    }

    public static object MergeOnto(JObject? overrideSection, object defaults, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(modelType);
        if (overrideSection == null) return defaults;

        var mergedJson = JObject.FromObject(defaults, CamelCaseSerializer);
        mergedJson.Merge(overrideSection, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Merge,
        });

        return mergedJson.ToObject(modelType, CamelCaseSerializer) ?? defaults;
    }

    public static IEnumerable<string> FindUnknownProperties(JObject? section, Type modelType)
    {
        if (section == null) yield break;

        var known = modelType.GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var property in section.Properties())
            if (!known.Contains(property.Name))
                yield return property.Name;
    }

    // Returns every property name present in `section` that has no matching
    // property (case-insensitive) in TModel - used to report unknown fields
    // instead of silently ignoring an operator's typo.
    public static IEnumerable<string> FindUnknownProperties<TModel>(JObject? section)
    {
        if (section == null) yield break;

        var known = typeof(TModel).GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var property in section.Properties())
            if (!known.Contains(property.Name))
                yield return property.Name;
    }
}
