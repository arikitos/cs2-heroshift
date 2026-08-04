namespace src.Configuration.Models;

/*
 * HudOptions - the canonical typed HUD configuration contract. Colours are plain
 * strings (hex or CS2 named colours like "white"/"orange"/"cyan"/"green" -
 * see the legacy defaults), sizes are the HTML HUD's own size tokens
 * (xxxl/xxl/xl/l/ml/m/sm/s/xs).
 */
public sealed record HudOptions
{
    public string HeaderLineColor { get; init; } = "#FFFFFF";
    public string HeaderLineSize { get; init; } = "";
    public string SkillLineSize { get; init; } = "l";
    public string InfoLineColor { get; init; } = "#FFFFFF";
    public string InfoLineSize { get; init; } = "sm";
    public string SkillDescriptionLineColor { get; init; } = "#999999";
    public string SkillDescriptionLineSize { get; init; } = "sm";
    public string WsadMenuSelectInfoLineColor { get; init; } = "#999999";
    public string WsadMenuSelectInfoLineSize { get; init; } = "sm";
    public string WsadMenuItemLineColor { get; init; } = "white";
    public string WsadMenuItemHoverLineColor { get; init; } = "orange";
    public string WsadMenuItemLineSize { get; init; } = "sm";
    public string WsadMenuControllsLineSize { get; init; } = "sm";
    public string WsadMenuControllsLineColor1 { get; init; } = "cyan";
    public string WsadMenuControllsLineColor2 { get; init; } = "white";
    public string WsadMenuControllsLineColor3 { get; init; } = "green";
}
