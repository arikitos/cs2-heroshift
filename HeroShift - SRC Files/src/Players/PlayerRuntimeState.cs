using src.SkillsCore.Abstractions;

namespace src.Players;

public sealed class PlayerRuntimeState
{
    public required bool IsBot { get; set; }
    public required string PlayerName { get; set; }
    public required uint PlayerIndex { get; set; }
    public SkillId Skill { get; set; } = BuiltInSkillIds.None;
    public SkillId SpecialSkill { get; set; } = BuiltInSkillIds.None;
    public float? SkillChance { get; set; }
    public bool IsDrawing { get; set; }
    public DateTime SkillHudExpired { get; set; }
    public DateTime SkillDescriptionHudExpired { get; set; }
    public DateTime HudSuppressedUntil { get; set; }
    public string? PrintHTML { get; set; }
    public int HideHUD { get; set; }
    public bool SkillUsed { get; set; }
    public bool? HudOnDeathBlocked { get; set; }
}
