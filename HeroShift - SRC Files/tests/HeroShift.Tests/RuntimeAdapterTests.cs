using CounterStrikeSharp.API.Core;
using src.Infrastructure.Menu;
using src.Infrastructure.Tracing;
using WASDSharedAPI;

namespace HeroShift.Tests;

public sealed class RuntimeAdapterTests
{
    [Fact]
    public void WasdAdapterCreatesManagerLazilyAndDelegatesOperations()
    {
        var manager = new FakeWasdMenuManager();
        var created = 0;
        var service = new WasdGameMenuService(() =>
        {
            created++;
            return manager;
        });

        Assert.Equal(0, created);
        Assert.Equal("not loaded", service.Status);
        Assert.False(service.HasMenu(null));
        Assert.Equal(1, created);

        service.CloseMenu(null);
        service.SetPaused(null, true);
        service.Unload();

        Assert.Equal(1, manager.CloseCalls);
        Assert.Equal(1, manager.PauseCalls);
        Assert.Equal(1, created);
    }

    [Fact]
    public void TraceAdapterLogsMissingCapabilityOnlyOnce()
    {
        var resolutionAttempts = 0;
        var messages = new List<string>();
        var service = new RayTraceService(
            () =>
            {
                resolutionAttempts++;
                return null;
            },
            messages.Add);

        Assert.False(service.IsAvailable);
        Assert.False(service.IsAvailable);

        Assert.Equal(2, resolutionAttempts);
        Assert.Single(messages);
        Assert.Contains("RayTrace module not found", messages[0]);
    }

    private sealed class FakeWasdMenuManager : IWasdMenuManager
    {
        public int CloseCalls { get; private set; }
        public int PauseCalls { get; private set; }

        public void OpenMainMenu(CCSPlayerController? player, IWasdMenu? menu) { }
        public void CloseMenu(CCSPlayerController? player) => CloseCalls++;
        public void CloseSubMenu(CCSPlayerController? player) { }
        public void CloseAllSubMenus(CCSPlayerController? player) { }
        public void OpenSubMenu(CCSPlayerController? player, IWasdMenu? menu) { }
        public IWasdMenu CreateMenu(string title, string itemText, string itemHoverText, string ControlText) =>
            throw new NotSupportedException();
        public bool HasMenu(CCSPlayerController? player) => false;
        public void UpdateActiveMenu(CCSPlayerController? player, Dictionary<string, Action<CCSPlayerController, IWasdMenuOption>> list) { }
        public bool SetMenuPaused(CCSPlayerController? player, bool pause)
        {
            PauseCalls++;
            return false;
        }
    }
}
