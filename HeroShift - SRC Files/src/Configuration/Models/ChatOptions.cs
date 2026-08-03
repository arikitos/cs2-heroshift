namespace src.Configuration.Models;

/*
 * ChatOptions - field-for-field equivalent of the legacy
 * src/utils/Config.ChatMessage nested class. The colour fields are CS2 chat
 * control characters (see CounterStrikeSharp.API.Modules.Utils.ChatColors),
 * not hex strings - \x02 and \x06 are two of those control codes, matching
 * the legacy hardcoded defaults exactly.
 */
public sealed record ChatOptions
{
    public float MaxWidth { get; init; } = 1280f;
    public char LineSymbol { get; init; } = '―';
    public string LineColor { get; init; } = "\x04";
    public bool LineShow { get; init; } = true;
    public string InfoPlayerNameColor { get; init; } = "\x02";
    public string InfoSkillColor { get; init; } = "\x06";
    public bool InfoMessageShow { get; init; } = true;
    public string TagFormat { get; init; } = "\x02◢◆◤ {TAG} ◥◆◣";
}
