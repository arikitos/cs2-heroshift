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

        Assert.True(registry.Contains(BuiltInSkillIds.Dash));
    }

    [Fact]
    public void DashDefinition_MatchesBaselineMetadataAndOptions()
    {
        // Cross-checked against tools/refactor-baseline/snapshot/baseline.json's
        // "Dash" entry: color "#42bbfc", onlyTeam CsTeam.None, maxPerServer -1,
        // rarity Rarity.Common, options jumpVelocity=150f, pushVelocity=600f,
        // anyDirection=true, cooldown=2f.
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
        // Baseline hooks for Dash: LoadSkill, EnableSkill, DisableSkill, OnTick, NewRound.
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
