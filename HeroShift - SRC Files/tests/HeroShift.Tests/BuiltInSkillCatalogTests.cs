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
        SkillId[] expected =
        [
            BuiltInSkillIds.None,
            BuiltInSkillIds.AntyFlash,
            BuiltInSkillIds.Astronaut,
            BuiltInSkillIds.Behind,
            BuiltInSkillIds.Catapult,
            BuiltInSkillIds.Disarmament,
            BuiltInSkillIds.Dash,
            BuiltInSkillIds.Dracula,
            BuiltInSkillIds.Dwarf,
            BuiltInSkillIds.FastReload,
            BuiltInSkillIds.FragileBomb,
            BuiltInSkillIds.Grenadier,
            BuiltInSkillIds.Illiterate,
            BuiltInSkillIds.Impostor,
            BuiltInSkillIds.InfiniteAmmo,
            BuiltInSkillIds.JumpingJack,
            BuiltInSkillIds.Knockback,
            BuiltInSkillIds.Push,
            BuiltInSkillIds.Pyro,
            BuiltInSkillIds.Rambo,
            BuiltInSkillIds.ReturnToSender,
            BuiltInSkillIds.RichBoy,
            BuiltInSkillIds.RobinHood,
            BuiltInSkillIds.Saper,
            BuiltInSkillIds.ShortBomb,
            BuiltInSkillIds.Silent,
            BuiltInSkillIds.Teleporter,
            BuiltInSkillIds.Zeus,
        ];

        Assert.Equal(expected.Length, registry.All.Count);
        Assert.Equal(expected, registry.All.Select(definition => definition.Id));
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
