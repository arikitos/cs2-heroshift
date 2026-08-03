using Newtonsoft.Json.Linq;
using src.SkillsCore.Abstractions;
using src.utils;

namespace src.SkillsCore;

/*
 * Temporary migration bridge. It resolves the typed option records for skills
 * that have already moved to SkillDefinition<TOptions> from the currently
 * loaded legacy skillsInfo.json objects. This keeps server overrides effective
 * while migration batches land. The bridge is removed together with
 * SkillsInfo.cs once heroshift.json becomes the only runtime configuration.
 *
 * Reflection/deserialization happens only on startup and successful reload,
 * never in a gameplay or tick hot path.
 */
internal static class LegacySkillConfigurationBridge
{
    public static IReadOnlyDictionary<SkillId, ISkillOptions> Resolve(
        SkillRegistry registry,
        SkillsInfo.SkillsInfoModel legacyConfiguration)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(legacyConfiguration);

        var legacyByName = legacyConfiguration
            .ToDictionary(skill => skill.Name, StringComparer.OrdinalIgnoreCase);
        var resolved = new Dictionary<SkillId, ISkillOptions>();

        foreach (var definition in registry.All)
        {
            ISkillOptions options = definition.DefaultOptionsBoxed;

            if (legacyByName.TryGetValue(definition.Id.Value, out var legacy))
            {
                var converted = JObject.FromObject(legacy)
                    .ToObject(options.GetType()) as ISkillOptions;

                if (converted != null)
                    options = converted;
            }

            resolved.Add(definition.Id, options);
        }

        return resolved;
    }
}
