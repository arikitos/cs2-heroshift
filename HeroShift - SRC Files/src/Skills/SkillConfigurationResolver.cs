using src.Configuration;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore;

public static class SkillConfigurationResolver
{
    // Test override retained for isolated resolver tests. Runtime code always
    // reads the atomic ConfigurationStore snapshot.
    private static IReadOnlyDictionary<SkillId, ISkillOptions>? _overrideOptions;

    public static void SetSnapshot(IReadOnlyDictionary<SkillId, ISkillOptions> options) =>
        Volatile.Write(ref _overrideOptions, options ?? throw new ArgumentNullException(nameof(options)));

    public static void UseRuntimeSnapshot() => Volatile.Write(ref _overrideOptions, null);

    public static TOptions Get<TOptions>(SkillId id)
        where TOptions : class, ISkillOptions
    {
        var overrideOptions = Volatile.Read(ref _overrideOptions);
        ISkillOptions options;

        if (overrideOptions != null)
        {
            if (!overrideOptions.TryGetValue(id, out options!))
                throw new InvalidOperationException($"No options snapshot registered for skill '{id}'.");
        }
        else
        {
            options = ConfigurationStore.Current.Skills.Get(id).Options;
        }

        if (options is not TOptions typed)
            throw new InvalidOperationException(
                $"Skill '{id}' options are '{options.GetType().Name}', not the requested '{typeof(TOptions).Name}'.");

        return typed;
    }
}
