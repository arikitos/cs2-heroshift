using src.SkillsCore.Abstractions;

namespace src.SkillsCore;

/*
 * SkillConfigurationResolver - typed replacement for
 * SkillsInfo.GetValue<T>(skillName, "key") (REFACTOR.md section 9).
 *
 * Skills are static classes with static hook methods (preserved as-is per
 * REFACTOR.md section 23 migration procedure - "preserve all static and
 * per-player state"), so there is no per-skill instance to inject a typed
 * options object into. This resolver is the static access point every
 * migrated skill's hook body calls instead:
 *
 *   SkillConfigurationResolver.Get<DashOptions>(BuiltInSkillIds.Dash).CooldownSeconds
 *
 * SetSnapshot is called once per effective configuration snapshot (initial
 * load and every successful !reload), matching REFACTOR.md section 14's
 * atomic-snapshot-replacement requirement - readers either see the fully old
 * or fully new options, never a partial mix.
 */
public static class SkillConfigurationResolver
{
    private static IReadOnlyDictionary<SkillId, ISkillOptions> _options = new Dictionary<SkillId, ISkillOptions>();

    public static void SetSnapshot(IReadOnlyDictionary<SkillId, ISkillOptions> options)
    {
        _options = options;
    }

    // Throws if the skill was never registered or its options type doesn't match
    // TOptions - both indicate a programming error (a skill referencing the
    // wrong options type), not a runtime/configuration problem, so this is
    // intentionally not a silent-default fallback like the legacy
    // SkillsInfo.GetValue<T> (REFACTOR.md section 9: "Invalid values must not
    // silently become default(T)").
    public static TOptions Get<TOptions>(SkillId id) where TOptions : class, ISkillOptions
    {
        if (!_options.TryGetValue(id, out var options))
            throw new InvalidOperationException($"No options snapshot registered for skill '{id}'. SkillConfigurationResolver.SetSnapshot must run before any skill hook executes.");

        if (options is not TOptions typed)
            throw new InvalidOperationException($"Skill '{id}' options are '{options.GetType().Name}', not the requested '{typeof(TOptions).Name}'.");

        return typed;
    }
}
