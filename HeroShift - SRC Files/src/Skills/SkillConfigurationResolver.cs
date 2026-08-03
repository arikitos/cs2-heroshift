using src.SkillsCore.Abstractions;
using src.utils;

namespace src.SkillsCore;

/*
 * SkillConfigurationResolver - typed replacement for
 * SkillsInfo.GetValue<T>(skillName, "key") (REFACTOR.md section 9).
 *
 * During migration, skillsInfo.json remains the active server configuration.
 * The first typed read builds an immutable typed snapshot for every migrated
 * definition. A successful legacy reload replaces SkillsInfo.LoadedConfig;
 * reference comparison detects that change and rebuilds the snapshot once.
 * No reflection or JSON conversion occurs in gameplay hot paths after that.
 *
 * The final heroshift.json bootstrap calls SetSnapshot, which permanently
 * disables this temporary legacy source for the running plugin instance.
 */
public static class SkillConfigurationResolver
{
    private static readonly object SnapshotLock = new();
    private static IReadOnlyDictionary<SkillId, ISkillOptions> _options =
        new Dictionary<SkillId, ISkillOptions>();
    private static SkillsInfo.SkillsInfoModel? _legacySource;
    private static bool _usesLegacySource = true;

    public static void SetSnapshot(IReadOnlyDictionary<SkillId, ISkillOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (SnapshotLock)
        {
            _options = options;
            _legacySource = null;
            _usesLegacySource = false;
        }
    }

    // Throws if the skill was never registered or its options type doesn't
    // match TOptions. Both indicate a programming error, not a recoverable
    // server configuration problem.
    public static TOptions Get<TOptions>(SkillId id)
        where TOptions : class, ISkillOptions
    {
        EnsureMigrationSnapshot();

        if (!_options.TryGetValue(id, out var options))
            throw new InvalidOperationException(
                $"No options snapshot registered for skill '{id}'.");

        if (options is not TOptions typed)
            throw new InvalidOperationException(
                $"Skill '{id}' options are '{options.GetType().Name}', not the requested '{typeof(TOptions).Name}'.");

        return typed;
    }

    private static void EnsureMigrationSnapshot()
    {
        if (!_usesLegacySource) return;

        var currentLegacySource = SkillsInfo.LoadedConfig;
        if (ReferenceEquals(_legacySource, currentLegacySource)) return;

        lock (SnapshotLock)
        {
            if (!_usesLegacySource || ReferenceEquals(_legacySource, currentLegacySource))
                return;

            var registry = BuiltInSkillCatalog.BuildRegistry();
            _options = LegacySkillConfigurationBridge.Resolve(registry, currentLegacySource);
            _legacySource = currentLegacySource;
        }
    }
}
