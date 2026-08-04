namespace src.SkillsCore.Abstractions;

/*
 * SkillId - the canonical, stable identity for a skill.
 *
 * Replaces string-based lookups keyed on the Skills enum's ToString() or a
 * class/file name coincidence (see legacy src/player/ISkill.cs Skills enum
 * and src/HeroShift.cs SkillAction). Values are lowercase-invariant so
 * command-line input, JSON override keys and console output all compare
 * equal regardless of the caller's casing.
 *
 * During the migration (REFACTOR.md commits 2-13) this type is created
 * alongside the legacy Skills enum, not instead of it - existing code keeps
 * working until every skill and consumer has moved over (removal happens in
 * the "remove legacy configuration and dispatch" commit).
 */
public readonly record struct SkillId : IComparable<SkillId>
{
    public string Value { get; }

    private SkillId(string value)
    {
        Value = value;
    }

    public static SkillId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Skill ID cannot be empty.", nameof(value));

        return new SkillId(value.Trim().ToLowerInvariant());
    }

    // Case-insensitive parse for user/command input; returns false instead of
    // throwing so callers (e.g. !setskill) can report "unknown skill" cleanly.
    public static bool TryParse(string? value, out SkillId id)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            id = default;
            return false;
        }

        id = new SkillId(value.Trim().ToLowerInvariant());
        return true;
    }

    public int CompareTo(SkillId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}
