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
            BuiltInSkillIds.Aimbot,
            BuiltInSkillIds.AimLock,
            BuiltInSkillIds.Anomaly,
            BuiltInSkillIds.AntyFlash,
            BuiltInSkillIds.AntyHead,
            BuiltInSkillIds.AreaReaper,
            BuiltInSkillIds.Armored,
            BuiltInSkillIds.Assassin,
            BuiltInSkillIds.Astronaut,
            BuiltInSkillIds.Bankrupt,
            BuiltInSkillIds.Baseball,
            BuiltInSkillIds.Behind,
            BuiltInSkillIds.Berserker,
            BuiltInSkillIds.BladeMaster,
            BuiltInSkillIds.BunnyHop,
            BuiltInSkillIds.BlastShot,
            BuiltInSkillIds.C4Camouflage,
            BuiltInSkillIds.CarefulBullets,
            BuiltInSkillIds.Catapult,
            BuiltInSkillIds.Chicken,
            BuiltInSkillIds.ChillOut,
            BuiltInSkillIds.Cutter,
            BuiltInSkillIds.Cypher,
            BuiltInSkillIds.Darkness,
            BuiltInSkillIds.Deactivator,
            BuiltInSkillIds.Deaf,
            BuiltInSkillIds.DeathBomb,
            BuiltInSkillIds.DemonEye,
            BuiltInSkillIds.Disarmament,
            BuiltInSkillIds.Distancer,
            BuiltInSkillIds.Dash,
            BuiltInSkillIds.Dracula,
            BuiltInSkillIds.Duplicator,
            BuiltInSkillIds.Dwarf,
            BuiltInSkillIds.EnemySpawn,
            BuiltInSkillIds.ExpensiveAmmo,
            BuiltInSkillIds.ExplodingBarrel,
            BuiltInSkillIds.ExplosiveShot,
            BuiltInSkillIds.FalconEye,
            BuiltInSkillIds.FastReload,
            BuiltInSkillIds.FireRain,
            BuiltInSkillIds.Flash,
            BuiltInSkillIds.Flashlight,
            BuiltInSkillIds.Fortnite,
            BuiltInSkillIds.FragileBomb,
            BuiltInSkillIds.FriendlyFire,
            BuiltInSkillIds.FrozenDecoy,
            BuiltInSkillIds.Gambler,
            BuiltInSkillIds.Ghost,
            BuiltInSkillIds.Giant,
            BuiltInSkillIds.Glaz,
            BuiltInSkillIds.Glitch,
            BuiltInSkillIds.Glue,
            BuiltInSkillIds.GodMode,
            BuiltInSkillIds.Grenadier,
            BuiltInSkillIds.HealingChicken,
            BuiltInSkillIds.HealingSmoke,
            BuiltInSkillIds.Hermit,
            BuiltInSkillIds.HolyHandGrenade,
            BuiltInSkillIds.HomingNades,
            BuiltInSkillIds.HotBomb,
            BuiltInSkillIds.Iana,
            BuiltInSkillIds.Illiterate,
            BuiltInSkillIds.Illusionist,
            BuiltInSkillIds.Impostor,
            BuiltInSkillIds.InfiniteAmmo,
            BuiltInSkillIds.Jackal,
            BuiltInSkillIds.Jammer,
            BuiltInSkillIds.Jester,
            BuiltInSkillIds.JetKick,
            BuiltInSkillIds.JumpBan,
            BuiltInSkillIds.JumpCurse,
            BuiltInSkillIds.JumpingJack,
            BuiltInSkillIds.KillerFlash,
            BuiltInSkillIds.Knockback,
            BuiltInSkillIds.LastGasp,
            BuiltInSkillIds.LifeSwap,
            BuiltInSkillIds.LongKnife,
            BuiltInSkillIds.LongZeus,
            BuiltInSkillIds.MagneticDecoy,
            BuiltInSkillIds.Magneto,
            BuiltInSkillIds.Magnifier,
            BuiltInSkillIds.Medic,
            BuiltInSkillIds.Miner,
            BuiltInSkillIds.MoneySwap,
            BuiltInSkillIds.Nightmare,
            BuiltInSkillIds.Ninja,
            BuiltInSkillIds.NoNades,
            BuiltInSkillIds.NoRecoil,
            BuiltInSkillIds.Noclip,
            BuiltInSkillIds.OneShot,
            BuiltInSkillIds.OnlyHead,
            BuiltInSkillIds.PawelJumper,
            BuiltInSkillIds.Phoenix,
            BuiltInSkillIds.PsychicDefusing,
            BuiltInSkillIds.Pilot,
            BuiltInSkillIds.Planter,
            BuiltInSkillIds.Poison,
            BuiltInSkillIds.PrimaryBan,
            BuiltInSkillIds.Prosthesis,
            BuiltInSkillIds.Push,
            BuiltInSkillIds.Pyro,
            BuiltInSkillIds.QuickShot,
            BuiltInSkillIds.RadarHack,
            BuiltInSkillIds.Rambo,
            BuiltInSkillIds.RandomWeapon,
            BuiltInSkillIds.ReZombie,
            BuiltInSkillIds.ReactiveArmor,
            BuiltInSkillIds.Regeneration,
            BuiltInSkillIds.Replicator,
            BuiltInSkillIds.Retreat,
            BuiltInSkillIds.ReturnToSender,
            BuiltInSkillIds.RichBoy,
            BuiltInSkillIds.RobinHood,
            BuiltInSkillIds.Rubber,
            BuiltInSkillIds.Saper,
            BuiltInSkillIds.SecondLife,
            BuiltInSkillIds.Shade,
            BuiltInSkillIds.ShortBomb,
            BuiltInSkillIds.Silent,
            BuiltInSkillIds.Smoker,
            BuiltInSkillIds.SniperElite,
            BuiltInSkillIds.Soldier,
            BuiltInSkillIds.SoundMaker,
            BuiltInSkillIds.Spectator,
            BuiltInSkillIds.SwapPosition,
            BuiltInSkillIds.TakeAmmo,
            BuiltInSkillIds.TeamTeleport,
            BuiltInSkillIds.Teleporter,
            BuiltInSkillIds.Thief,
            BuiltInSkillIds.ThirdEye,
            BuiltInSkillIds.Thorns,
            BuiltInSkillIds.ThrowingKnife,
            BuiltInSkillIds.ToxicSmoke,
            BuiltInSkillIds.Tripwire,
            BuiltInSkillIds.Wallhack,
            BuiltInSkillIds.Watchmaker,
            BuiltInSkillIds.WeaponsSwap,
            BuiltInSkillIds.Weightless,
            BuiltInSkillIds.WildThrow,
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
