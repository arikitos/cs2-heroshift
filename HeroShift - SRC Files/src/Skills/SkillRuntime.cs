using src.Configuration;
using src.player;
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

    public static SkillMetadata GetMetadata(Skills skill) => Current.Skills.Get(skill).Metadata;
    public static SkillMetadata GetMetadata(SkillId id) => Current.Skills.Get(id).Metadata;

    public static SkillId GetId(Skills skill) => Current.Skills.Get(skill).Id;

    public static float GetMaxDistance(Skills skill) =>
        Current.Skills.Get(skill).Options is IMaxDistanceOptions options
            ? options.MaxDistance
            : 0f;

    public static bool TryGetLegacySkill(SkillId id, out Skills skill)
    {
        if (Current.Skills.TryGet(id, out var resolved))
        {
            skill = resolved.LegacySkill;
            return true;
        }

        skill = default;
        return false;
    }

    private static RuntimeConfigurationSnapshot Current =>
        Volatile.Read(ref _snapshot)
        ?? throw new InvalidOperationException("Skill runtime configuration has not been initialized.");
}
