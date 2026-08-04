using CounterStrikeSharp.API.Core;
using WASDSharedAPI;

namespace src.Infrastructure.Menu;

/// <summary>
/// Internal boundary around the bundled WASDMenu implementation. Gameplay code
/// depends on this contract instead of constructing or storing a WasdManager.
/// </summary>
public interface IGameMenuService
{
    void Load(BasePlugin plugin, bool hotReload);
    IWasdMenu CreateMenu(string title, string itemText, string itemHoverText, string controlText);
    void OpenMainMenu(CCSPlayerController? player, IWasdMenu? menu);
    void CloseMenu(CCSPlayerController? player);
    bool HasMenu(CCSPlayerController? player);
    bool SetPaused(CCSPlayerController? player, bool paused);
    void UpdateActiveMenu(
        CCSPlayerController? player,
        Dictionary<string, Action<CCSPlayerController, IWasdMenuOption>> options);
}
