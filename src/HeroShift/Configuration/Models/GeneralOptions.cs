namespace src.Configuration.Models;

/*
 * GeneralOptions - server-wide plugin behaviour that isn't HUD, chat, command
 * or voting specific. Field-for-field equivalent to the general fields of the
 * previous global settings contract; defaults are now canonical here - "ConfigName" is intentionally omitted here since it
 * was a cosmetic console label, not gameplay behaviour.
 */
public sealed record GeneralOptions
{
    public GameMode GameMode { get; init; } = GameMode.NoRepeat;

    public bool YourSkillChatInfo { get; init; } = true;
    public bool KillerSkillChatInfo { get; init; } = true;
    public bool TeamMateSkillChatInfo { get; init; } = true;
    public bool SummaryAfterTheRound { get; init; } = true;

    public bool EnableBotSkills { get; init; } = true;
    public bool EnableBotKickDebug { get; init; }
    public bool EnableFullForceUpdate { get; init; }

    public bool DebugMode { get; init; }
    public bool PerfMode { get; init; }

    public string? AlternativeSkillButton { get; init; }
    public string Language { get; init; } = "en";

    public float SkillTimeBeforeStart { get; init; } = 7f;

    public float SkillHudDuration { get; init; } = -1f;
    public float SkillDescriptionDuration { get; init; } = 7f;
    public bool DisplayAlwaysDescription { get; init; }

    public bool DisableSpectateHUD { get; init; }
    public bool HideHudForOtherPlugins { get; init; } = true;
    public bool EnableFlashingHtmlHudFix { get; init; }
    public bool TraceRayBeam { get; init; }

    public string DisableHUDOnDeathPermission { get; init; } = "@HeroShift/death";
    public bool DisableSkillsOnRoundEnd { get; init; }
    public int? CurseSkillPerPlayer { get; init; }
}

/*
 * Mirrors the established GameMode contract exactly (same underlying values -
 * command handlers and any persisted/serialized int must keep comparing equal).
 */
public enum GameMode
{
    Normal = 0,
    TeamSkills = 1,
    SameSkills = 2,
    NoRepeat = 3,
    FullRandom = 4,
    Debug = 5,
}
