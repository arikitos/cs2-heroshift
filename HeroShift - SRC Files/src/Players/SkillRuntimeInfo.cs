using src.SkillsCore.Abstractions;

namespace src.Players;

public sealed class SkillRuntimeInfo(SkillId skill, string color, bool display)
{
    public SkillId Skill { get; } = skill;
    public string Color { get; set; } = color;
    public bool Display { get; } = display;

    public static implicit operator SkillId(SkillRuntimeInfo? value) => value?.Skill ?? BuiltInSkillIds.None;
}
