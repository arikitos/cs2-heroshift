namespace src.SkillsCore.Abstractions;

/*
 * SkillDefinition - one canonical, typed definition per skill: identity,
 * shared metadata defaults, skill-specific option defaults and its
 * registered hooks. Replaces the combination of:
 *   - a stable SkillId (identity)
 *   - explicit typed hook delegates (dispatch target)
 *   - a typed options record owned by the canonical definition
 * with one object a registry can look up by SkillId (REFACTOR.md section 8).
 *
 * SkillDefinition<TOptions> is the type-safe entry point authors use; the
 * non-generic SkillDefinition base lets the registry store every skill in a
 * single heterogeneous collection while still allowing typed option access
 * through SkillOptions.Get<TOptions>(id) at the call site.
 */
public abstract record SkillDefinition
{
    public required SkillId Id { get; init; }
    public required SkillMetadata Metadata { get; init; }
    public required SkillHookSet Hooks { get; init; }

    // Boxed as ISkillOptions so the non-generic registry can hold every skill;
    // callers retrieve the typed value back via SkillDefinition<T>.DefaultOptions
    // or SkillOptionsResolver, never by casting this directly.
    public abstract ISkillOptions DefaultOptionsBoxed { get; }
    public abstract IReadOnlyList<string> ValidateOptionsBoxed(ISkillOptions options);
}

public sealed record SkillDefinition<TOptions> : SkillDefinition
    where TOptions : class, ISkillOptions
{
    public required TOptions DefaultOptions { get; init; }
    public Func<TOptions, IReadOnlyList<string>>? OptionsValidator { get; init; }

    public override ISkillOptions DefaultOptionsBoxed => DefaultOptions;
    public override IReadOnlyList<string> ValidateOptionsBoxed(ISkillOptions options) =>
        OptionsValidator?.Invoke((TOptions)options) ?? [];
}

public static class SkillOptionRules
{
    public static IReadOnlyList<string> Ordered(float minimum, float maximum, string minimumName, string maximumName) =>
        minimum <= maximum ? [] : [$"{minimumName} must be less than or equal to {maximumName}"];

    public static IReadOnlyList<string> Ordered(int minimum, int maximum, string minimumName, string maximumName) =>
        minimum <= maximum ? [] : [$"{minimumName} must be less than or equal to {maximumName}"];
}
