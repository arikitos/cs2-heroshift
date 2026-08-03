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
            BuiltInSkillIds.AimLock,
            BuiltInSkillIds.Anomaly,
            BuiltInSkillIds.AntyFlash,
            BuiltInSkillIds.AreaReaper,
            BuiltInSkillIds.Astronaut,
            BuiltInSkillIds.Bankrupt,
            BuiltInSkillIds.Behind,
            BuiltInSkillIds.BunnyHop,
            BuiltInSkillIds.C4Camouflage,
            BuiltInSkillIds.Catapult,
            BuiltInSkillIds.Chicken,
            BuiltInSkillIds.ChillOut,
            BuiltInSkillIds.Darkness,
            BuiltInSkillIds.Deactivator,
            BuiltInSkillIds.Deaf,
            BuiltInSkillIds.Disarmament,
            BuiltInSkillIds.Distancer,
            BuiltInSkillIds.Dash,
            BuiltInSkillIds.Dracula,
            BuiltInSkillIds.Duplicator,
            BuiltInSkillIds.Dwarf,
            BuiltInSkillIds.EnemySpawn,
            BuiltInSkillIds.ExpensiveAmmo,
            BuiltInSkillIds.FalconEye,
            BuiltInSkillIds.FastReload,
            BuiltInSkillIds.Flash,
            BuiltInSkillIds.FragileBomb,
            BuiltInSkillIds.FrozenDecoy,
            BuiltInSkillIds.Ghost,
            BuiltInSkillIds.Giant,
            BuiltInSkillIds.Glitch,
            BuiltInSkillIds.Grenadier,
            BuiltInSkillIds.HealingChicken,
            BuiltInSkillIds.Illiterate,
            BuiltInSkillIds.Impostor,
            BuiltInSkillIds.InfiniteAmmo,
            BuiltInSkillIds.Jammer,
            BuiltInSkillIds.JetKick,
            BuiltInSkillIds.JumpBan,
            BuiltInSkillIds.JumpCurse,
            BuiltInSkillIds.JumpingJack,
            BuiltInSkillIds.Knockback,
            BuiltInSkillIds.LifeSwap,
            BuiltInSkillIds.MagneticDecoy,
            BuiltInSkillIds.Magnifier,
            BuiltInSkillIds.Medic,
            BuiltInSkillIds.MoneySwap,
            BuiltInSkillIds.Ninja,
            BuiltInSkillIds.NoRecoil,
            BuiltInSkillIds.PawelJumper,
            BuiltInSkillIds.PsychicDefusing,
            BuiltInSkillIds.Pilot,
            BuiltInSkillIds.Planter,
            BuiltInSkillIds.PrimaryBan,
            BuiltInSkillIds.Push,
            BuiltInSkillIds.Pyro,
            BuiltInSkillIds.QuickShot,
            BuiltInSkillIds.RadarHack,
            BuiltInSkillIds.Rambo,
            BuiltInSkillIds.Regeneration,
            BuiltInSkillIds.Retreat,
            BuiltInSkillIds.ReturnToSender,
            BuiltInSkillIds.RichBoy,
            BuiltInSkillIds.RobinHood,
            BuiltInSkillIds.Rubber,
            BuiltInSkillIds.Saper,
            BuiltInSkillIds.ShortBomb,
            BuiltInSkillIds.Silent,
            BuiltInSkillIds.SoundMaker,
            BuiltInSkillIds.Spectator,
            BuiltInSkillIds.SwapPosition,
            BuiltInSkillIds.TakeAmmo,
            BuiltInSkillIds.Teleporter,
            BuiltInSkillIds.Thief,
            BuiltInSkillIds.ThirdEye,
            BuiltInSkillIds.WeaponsSwap,
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
