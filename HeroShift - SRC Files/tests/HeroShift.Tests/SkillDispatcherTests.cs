using CounterStrikeSharp.API.Modules.Utils;
using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.utils;

namespace HeroShift.Tests;

// Pins the exact boolean-hook and fan-out semantics documented in
// src/Skills/BOOLEAN_HOOK_SEMANTICS.md, characterized from the legacy
// dispatch before SkillDispatcher replaced it (REFACTOR.md section 11).
public class SkillDispatcherTests
{
    private static SkillMetadata DefaultMetadata() => new(
        Active: true, Color: "#fff", OnlyTeam: CsTeam.None, DisableOnFreezeTime: false,
        NeedsTeammates: false, RequiredPermission: "", HudDuration: null,
        DescriptionHudDuration: null, MaxPerServer: -1, Rarity: Rarity.Common);

    private static SkillDefinition<NoSkillOptions> MakeDefinition(SkillId id, SkillHookSet hooks) => new()
    {
        Id = id,
        Metadata = DefaultMetadata(),
        Hooks = hooks,
        DefaultOptions = NoSkillOptions.Instance,
    };

    // ---- Single-skill lifecycle and state cleanup -----------------------

    [Fact]
    public void InvokeLifecycle_CallsOnlyRequestedSkillInOrder()
    {
        var registry = new SkillRegistry();
        var calls = new List<string>();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet
        {
            LoadSkill = () => calls.Add("load"),
            EnableSkill = _ => calls.Add("enable"),
            UseSkill = _ => calls.Add("use"),
            TypeSkill = (_, args) => calls.Add($"type:{args[0]}"),
            DisableSkill = _ => calls.Add("disable"),
        }));

        var dispatcher = new SkillDispatcher(registry);
        dispatcher.InvokeLoadSkill(BuiltInSkillIds.Dash);
        dispatcher.InvokeEnableSkill(BuiltInSkillIds.Dash, null!);
        dispatcher.InvokeUseSkill(BuiltInSkillIds.Dash, null!);
        dispatcher.InvokeTypeSkill(BuiltInSkillIds.Dash, null!, ["target"]);
        dispatcher.InvokeDisableSkill(BuiltInSkillIds.Dash, null!);

        Assert.Equal(["load", "enable", "use", "type:target", "disable"], calls);
    }

    [Fact]
    public void DispatchPlayerDisconnect_NotifiesEveryProvidedSkillInOrder()
    {
        var registry = new SkillRegistry();
        var calls = new List<string>();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { PlayerDisconnect = index => calls.Add($"dash:{index}") }));
        registry.Register(MakeDefinition(BuiltInSkillIds.KillerFlash, new SkillHookSet { PlayerDisconnect = index => calls.Add($"killerflash:{index}") }));

        var dispatcher = new SkillDispatcher(registry);
        dispatcher.DispatchPlayerDisconnect([BuiltInSkillIds.Dash, BuiltInSkillIds.KillerFlash], 17);

        Assert.Equal(["dash:17", "killerflash:17"], calls);
    }

    [Fact]
    public void DispatchWorldHooks_RoutesTypedArguments()
    {
        var registry = new SkillRegistry();
        var calls = new List<string>();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet
        {
            OnEntitySpawned = _ => calls.Add("entity"),
            BombPlanted = _ => calls.Add("bomb"),
            SmokegrenadeExpired = _ => calls.Add("smoke"),
            OnTriggerEnter = (_, _) => calls.Add("trigger"),
        }));

        var dispatcher = new SkillDispatcher(registry);
        dispatcher.DispatchOnEntitySpawned([BuiltInSkillIds.Dash], null!);
        dispatcher.DispatchBombPlanted([BuiltInSkillIds.Dash], null!);
        dispatcher.DispatchSmokegrenadeExpired([BuiltInSkillIds.Dash], null!);
        dispatcher.DispatchOnTriggerEnter([BuiltInSkillIds.Dash], null!, null!);

        Assert.Equal(["entity", "bomb", "smoke", "trigger"], calls);
    }

    // ---- OnTick / generic fan-out: every distinct active skill runs, independently ----

    [Fact]
    public void InvokeTickUnchecked_PropagatesHookFailureToCaller()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet
        {
            OnTick = () => throw new InvalidOperationException("tick failed"),
        }));

        var dispatcher = new SkillDispatcher(registry);

        var exception = Assert.Throws<InvalidOperationException>(
            () => dispatcher.InvokeTickUnchecked(BuiltInSkillIds.Dash));
        Assert.Equal("tick failed", exception.Message);
    }

    [Fact]
    public void DispatchTick_CallsEveryActiveSkillsOnTickHook()
    {
        var registry = new SkillRegistry();
        var calls = new List<string>();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { OnTick = () => calls.Add("dash") }));
        registry.Register(MakeDefinition(BuiltInSkillIds.KillerFlash, new SkillHookSet { OnTick = () => calls.Add("killerflash") }));

        var dispatcher = new SkillDispatcher(registry);
        dispatcher.DispatchTick([BuiltInSkillIds.Dash, BuiltInSkillIds.KillerFlash]);

        Assert.Equal(["dash", "killerflash"], calls);
    }

    [Fact]
    public void DispatchTick_SkillWithNoOnTickHook_IsSilentlySkipped()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.None, new SkillHookSet()));

        var dispatcher = new SkillDispatcher(registry);
        var exception = Record.Exception(() => dispatcher.DispatchTick([BuiltInSkillIds.None]));

        Assert.Null(exception);
    }

    [Fact]
    public void Dispatch_UnregisteredSkillId_IsSilentlySkipped()
    {
        var registry = new SkillRegistry();
        var dispatcher = new SkillDispatcher(registry);

        var exception = Record.Exception(() => dispatcher.DispatchTick([BuiltInSkillIds.Dash]));

        Assert.Null(exception);
    }

    [Fact]
    public void Dispatch_HookThrows_IsCaughtAndDoesNotAbortLaterSkills()
    {
        var registry = new SkillRegistry();
        var calls = new List<string>();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { OnTick = () => throw new InvalidOperationException("boom") }));
        registry.Register(MakeDefinition(BuiltInSkillIds.KillerFlash, new SkillHookSet { OnTick = () => calls.Add("killerflash") }));

        var exceptions = new List<string>();
        var dispatcher = new SkillDispatcher(registry, onHookException: exceptions.Add);

        dispatcher.DispatchTick([BuiltInSkillIds.Dash, BuiltInSkillIds.KillerFlash]);

        Assert.Equal(["killerflash"], calls);
        Assert.Single(exceptions);
        Assert.Contains("boom", exceptions[0]);
    }

    // ---- OnTakeDamage: late-damage skills always run after every other active skill ----

    [Fact]
    public void DispatchOnTakeDamage_LateDamageSkillsAlwaysRunLast()
    {
        var registry = new SkillRegistry();
        var order = new List<string>();
        registry.Register(MakeDefinition(BuiltInSkillIds.SecondLife, new SkillHookSet { OnTakeDamage = _ => order.Add("secondlife") }));
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { OnTakeDamage = _ => order.Add("dash") }));
        registry.Register(MakeDefinition(BuiltInSkillIds.Phoenix, new SkillHookSet { OnTakeDamage = _ => order.Add("phoenix") }));

        var dispatcher = new SkillDispatcher(registry);
        // SecondLife listed FIRST in input order, but must still run LAST.
        dispatcher.DispatchOnTakeDamage([BuiltInSkillIds.SecondLife, BuiltInSkillIds.Dash, BuiltInSkillIds.Phoenix], null!, post: false);

        Assert.Equal(["dash", "secondlife", "phoenix"], order);
    }

    // ---- PlayerHurtPre: victim asked first; attacker only if victim didn't suppress
    // AND holds a different skill; first true wins ----

    [Fact]
    public void DispatchPlayerHurtPre_VictimSuppresses_AttackerNeverAsked()
    {
        var registry = new SkillRegistry();
        bool attackerAsked = false;
        registry.Register(MakeDefinition(BuiltInSkillIds.AntyFlash, new SkillHookSet { PlayerHurtPre = _ => true }));
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { PlayerHurtPre = _ => { attackerAsked = true; return false; } }));

        var dispatcher = new SkillDispatcher(registry);
        var suppressed = dispatcher.DispatchPlayerHurtPre(BuiltInSkillIds.AntyFlash, BuiltInSkillIds.Dash, null!);

        Assert.True(suppressed);
        Assert.False(attackerAsked);
    }

    [Fact]
    public void DispatchPlayerHurtPre_VictimDoesNotSuppress_AttackerAskedIfDifferentSkill()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.AntyFlash, new SkillHookSet { PlayerHurtPre = _ => false }));
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { PlayerHurtPre = _ => true }));

        var dispatcher = new SkillDispatcher(registry);
        var suppressed = dispatcher.DispatchPlayerHurtPre(BuiltInSkillIds.AntyFlash, BuiltInSkillIds.Dash, null!);

        Assert.True(suppressed);
    }

    [Fact]
    public void DispatchPlayerHurtPre_AttackerHasSameSkillAsVictim_AttackerNotAskedAgain()
    {
        var registry = new SkillRegistry();
        int callCount = 0;
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { PlayerHurtPre = _ => { callCount++; return false; } }));

        var dispatcher = new SkillDispatcher(registry);
        dispatcher.DispatchPlayerHurtPre(BuiltInSkillIds.Dash, BuiltInSkillIds.Dash, null!);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void DispatchPlayerHurtPre_NoAttacker_OnlyVictimAsked()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { PlayerHurtPre = _ => false }));

        var dispatcher = new SkillDispatcher(registry);
        var suppressed = dispatcher.DispatchPlayerHurtPre(BuiltInSkillIds.Dash, attackerSkillId: null, null!);

        Assert.False(suppressed);
    }

    // ---- OnWeaponCanAcquire: every distinct active skill asked, not just one player's;
    // first true wins and short-circuits ----

    [Fact]
    public void DispatchOnWeaponCanAcquire_AsksEveryActiveSkillUntilFirstTrue()
    {
        var registry = new SkillRegistry();
        var asked = new List<string>();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { OnWeaponCanAcquire = (_, _, _, _) => { asked.Add("dash"); return false; } }));
        registry.Register(MakeDefinition(BuiltInSkillIds.Iana, new SkillHookSet { OnWeaponCanAcquire = (_, _, _, _) => { asked.Add("iana"); return true; } }));
        registry.Register(MakeDefinition(BuiltInSkillIds.KillerFlash, new SkillHookSet { OnWeaponCanAcquire = (_, _, _, _) => { asked.Add("killerflash"); return false; } }));

        var dispatcher = new SkillDispatcher(registry);
        var blocked = dispatcher.DispatchOnWeaponCanAcquire(
            [BuiltInSkillIds.Dash, BuiltInSkillIds.Iana, BuiltInSkillIds.KillerFlash], null!, null!, null!, null!);

        Assert.True(blocked);
        Assert.Equal(["dash", "iana"], asked);
    }

    [Fact]
    public void DispatchOnWeaponCanAcquire_NoSkillBlocks_ReturnsFalse()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet { OnWeaponCanAcquire = (_, _, _, _) => false }));

        var dispatcher = new SkillDispatcher(registry);
        var blocked = dispatcher.DispatchOnWeaponCanAcquire([BuiltInSkillIds.Dash], null!, null!, null!, null!);

        Assert.False(blocked);
    }
}
