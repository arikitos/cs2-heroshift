using CounterStrikeSharp.API.Modules.Utils;
using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.utils;

namespace HeroShift.Tests;

public class SkillRegistryTests
{
    private static SkillMetadata DefaultMetadata() => new(
        Active: true,
        Color: "#ffffff",
        OnlyTeam: CsTeam.None,
        DisableOnFreezeTime: false,
        NeedsTeammates: false,
        RequiredPermission: "",
        HudDuration: null,
        DescriptionHudDuration: null,
        MaxPerServer: -1,
        Rarity: Rarity.Common);

    private static SkillDefinition<NoSkillOptions> MakeDefinition(SkillId id, SkillHookSet? hooks = null) => new()
    {
        Id = id,
        Metadata = DefaultMetadata(),
        Hooks = hooks ?? new SkillHookSet(),
        DefaultOptions = NoSkillOptions.Instance,
    };

    [Fact]
    public void Register_ThenGet_ReturnsSameDefinition()
    {
        var registry = new SkillRegistry();
        var definition = MakeDefinition(BuiltInSkillIds.Dash);
        registry.Register(definition);
        Assert.True(registry.Contains(BuiltInSkillIds.Dash));
        Assert.Same(definition, registry.Get(BuiltInSkillIds.Dash));
    }

    [Fact]
    public void Register_DuplicateId_Throws()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash));
        Assert.Throws<InvalidOperationException>(() => registry.Register(MakeDefinition(BuiltInSkillIds.Dash)));
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        var registry = new SkillRegistry();
        Assert.Throws<KeyNotFoundException>(() => registry.Get(BuiltInSkillIds.Dash));
    }

    [Fact]
    public void TryGet_UnknownId_ReturnsFalse()
    {
        var registry = new SkillRegistry();
        Assert.False(registry.TryGet(BuiltInSkillIds.Dash, out _));
    }

    [Fact]
    public void TickSkills_OnlyIncludesDefinitionsWithOnTickHook()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { OnTick = () => { } }));
        registry.Register(MakeDefinition(BuiltInSkillIds.None));
        Assert.Single(registry.TickSkills);
        Assert.Equal(BuiltInSkillIds.Dash, registry.TickSkills[0].Id);
    }

    [Fact]
    public void All_ReturnsEveryRegisteredDefinition()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash));
        registry.Register(MakeDefinition(BuiltInSkillIds.KillerFlash));
        Assert.Equal(2, registry.All.Count);
    }

    [Fact]
    public void Register_AfterHookIndexWasRead_InvalidatesCachedIndex()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.None));
        Assert.Empty(registry.TickSkills);
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { OnTick = () => { } }));
        Assert.Single(registry.TickSkills);
        Assert.Equal(BuiltInSkillIds.Dash, registry.TickSkills[0].Id);
    }
}
