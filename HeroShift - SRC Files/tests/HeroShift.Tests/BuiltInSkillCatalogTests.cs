using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;

namespace HeroShift.Tests;

// Baseline equivalence checks (REFACTOR.md section 30): every migrated
// skill's typed defaults must match the Commit-1 baseline snapshot
// (tools/refactor-baseline/snapshot/baseline.json) semantically.
public class BuiltInSkillCatalogTests
{
    [Fact]
    public void BuildRegistry_RegistersEveryMigratedSkillExactlyOnce()
    {
        var registry = BuiltInSkillCatalog.BuildRegistry();

        Assert.Equal(10, registry.All.Count);
        Assert.True(registry.Contains(BuiltInSkillIds.AntyFlash));
        Assert.True(registry.Contains(BuiltInSkillIds.Astronaut));
        Assert.True(registry.Contains(BuiltInSkillIds.Behind));
        Assert.True(registry.Contains(BuiltInSkillIds.Dash));
        Assert.True(registry.Contains(BuiltInSkillIds.Dracula));
        Assert.True(registry.Contains(BuiltInSkillIds.Dwarf));
        Assert.True(registry.Contains(BuiltInSkillIds.FastReload));
        Assert.True(registry.Contains(BuiltInSkillIds.Illiterate));
        Assert.True(registry.Contains(BuiltInSkillIds.Push));
        Assert.True(registry.Contains(BuiltInSkillIds.RobinHood));
    }

    [Fact]
    public void DashDefinition_MatchesBaselineMetadataAndOptions()
    {
        var definition = DashDefinition.Create();

        Assert.Equal(BuiltInSkillIds.Dash, definition.Id);
        Assert.Equal("#42bbfc", definition.Metadata.Color);
        Assert.Equal(CounterStrikeSharp.API.Modules.Utils.CsTeam.None, definition.Metadata.OnlyTeam);
        Assert.Equal(-1, definition.Metadata.MaxPerServer);
        Assert.Equal(global::src.utils.Rarity.Common, definition.Metadata.Rarity);

        Assert.Equal(150f, definition.DefaultOptions.JumpVelocity);
        Assert.Equal(600f, definition.DefaultOptions.PushVelocity);
        Assert.True(definition.DefaultOptions.AnyDirection);
        Assert.Equal(2f, definition.DefaultOptions.Cooldown);
    }

    [Fact]
    public void DashDefinition_RegistersExpectedHooksMatchingBaseline()
    {
        var definition = DashDefinition.Create();

        Assert.NotNull(definition.Hooks.LoadSkill);
        Assert.NotNull(definition.Hooks.EnableSkill);
        Assert.NotNull(definition.Hooks.DisableSkill);
        Assert.NotNull(definition.Hooks.OnTick);
        Assert.NotNull(definition.Hooks.NewRound);

        Assert.Null(definition.Hooks.UseSkill);
        Assert.Null(definition.Hooks.TypeSkill);
        Assert.Null(definition.Hooks.PlayerHurtPre);
    }
}
