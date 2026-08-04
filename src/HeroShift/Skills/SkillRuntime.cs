using src.Configuration;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore;

public static class SkillRuntime
{
    private static RuntimeConfigurationSnapshot? _snapshot;

    public static IReadOnlyList<EffectiveSkillConfiguration> All => Current.Skills.All;

    internal static void SetSnapshot(RuntimeConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Volatile.Write(ref _snapshot, snapshot);
        SkillConfigurationResolver.UseRuntimeSnapshot();
    }

    internal static void Reset()
    {
        Volatile.Write(ref _snapshot, null);
        SkillConfigurationResolver.UseRuntimeSnapshot();
    }

    public static SkillMetadata GetMetadata(SkillId id) => Current.Skills.Get(id).Metadata;

    public static float GetMaxDistance(SkillId skill) =>
        Current.Skills.Get(skill).Options is IMaxDistanceOptions options
            ? options.MaxDistance
            : 0f;

    private static RuntimeConfigurationSnapshot Current =>
        Volatile.Read(ref _snapshot)
        ?? throw new InvalidOperationException("Skill runtime configuration has not been initialized.");
}
