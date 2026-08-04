namespace src.SkillsCore.Abstractions;

/*
 * Marker interface for a skill's typed option record (e.g. DashOptions,
 * KillerFlashOptions). Every skill owns one sealed record implementing
 * this interface - see REFACTOR.md
 * section 9. Skills with no tunables beyond the shared SkillMetadata use
 * NoSkillOptions.
 */
public interface ISkillOptions
{
}

public interface IMaxDistanceOptions : ISkillOptions
{
    float MaxDistance { get; }
}

public sealed record NoSkillOptions : ISkillOptions
{
    public static readonly NoSkillOptions Instance = new();
}
