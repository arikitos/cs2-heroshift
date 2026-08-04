using CounterStrikeSharp.API.Core;
using WASDMenuAPI.Classes;
using WASDSharedAPI;

namespace src.Infrastructure.Menu;

/// <summary>
/// Compatibility-preserving adapter for the bundled WASDMenuAPI project.
/// It deliberately keeps the current API, input semantics and DLL layout.
/// </summary>
public sealed class WasdGameMenuService : IGameMenuService
{
    private readonly Lazy<IWasdMenuManager> manager;

    public WasdGameMenuService(Func<IWasdMenuManager>? managerFactory = null)
    {
        manager = new Lazy<IWasdMenuManager>(
            managerFactory ?? (static () => new WasdManager()),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void Load(BasePlugin plugin, bool hotReload) =>
        WASDMenuAPI.WASDMenuAPI.LoadPlugin(plugin, hotReload);

    public IWasdMenu CreateMenu(string title, string itemText, string itemHoverText, string controlText) =>
        manager.Value.CreateMenu(title, itemText, itemHoverText, controlText);

    public void OpenMainMenu(CCSPlayerController? player, IWasdMenu? menu) =>
        manager.Value.OpenMainMenu(player, menu);

    public void CloseMenu(CCSPlayerController? player) => manager.Value.CloseMenu(player);

    public bool HasMenu(CCSPlayerController? player) => manager.Value.HasMenu(player);

    public bool SetPaused(CCSPlayerController? player, bool paused) =>
        manager.Value.SetMenuPaused(player, paused);

    public void UpdateActiveMenu(
        CCSPlayerController? player,
        Dictionary<string, Action<CCSPlayerController, IWasdMenuOption>> options) =>
        manager.Value.UpdateActiveMenu(player, options);
}
