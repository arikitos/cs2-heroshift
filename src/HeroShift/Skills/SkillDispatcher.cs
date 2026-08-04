using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore;

/*
 * SkillDispatcher - typed replacement for the reflection-based fan-out in
 * previous fan-out in PlayerEvents.cs and EntityEvents.cs. See
 * docs/dispatch-semantics.md for the exact,
 * characterized rules this preserves.
 *
 * Deliberately decoupled from PlayerRuntimeState. Callers pass the distinct
 * active SkillIds in established player order, while the dispatcher owns only
 * definition lookup, hook invocation and exception isolation.
 *
 * Every hook invocation is wrapped so a thrown exception is caught and
 * logged rather than aborting the engine callback and skipping every later
 * skill in the same dispatch - matching legacy InvokeSkill's behavior
 * exactly (the documented architecture: "existing exception behavior unless it
 * can crash the server").
 */
public sealed class SkillDispatcher(SkillRegistry registry, Action<string>? onHookException = null)
{
    // Revive-on-lethal-damage heroes that must
    // observe the FINAL damage value after every other active skill's
    // OnTakeDamage/OnTakeDamagePost already ran this call.
    private static readonly SkillId[] LateDamageSkillIds =
    [
        BuiltInSkillIds.SecondLife,
        BuiltInSkillIds.Phoenix,
        BuiltInSkillIds.ReZombie,
    ];

    private void Invoke(SkillId skillId, string hookName, Action<SkillDefinition> invoke)
    {
        if (!registry.TryGet(skillId, out var definition)) return;

        try
        {
            invoke(definition);
        }
        catch (Exception ex)
        {
            onHookException?.Invoke($"{skillId}.{hookName} failed: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // ---- Single-skill lifecycle hooks. These are intentionally separate from
    // fan-out because the caller owns assignment bookkeeping and player state. ----

    public void InvokeLoadSkill(SkillId skillId) =>
        Invoke(skillId, nameof(SkillHookSet.LoadSkill), d => d.Hooks.LoadSkill?.Invoke());

    public void InvokeEnableSkill(SkillId skillId, CCSPlayerController player) =>
        Invoke(skillId, nameof(SkillHookSet.EnableSkill), d => d.Hooks.EnableSkill?.Invoke(player));

    public void InvokeDisableSkill(SkillId skillId, CCSPlayerController player) =>
        Invoke(skillId, nameof(SkillHookSet.DisableSkill), d => d.Hooks.DisableSkill?.Invoke(player));

    public void InvokeUseSkill(SkillId skillId, CCSPlayerController player) =>
        Invoke(skillId, nameof(SkillHookSet.UseSkill), d => d.Hooks.UseSkill?.Invoke(player));

    public void InvokeTypeSkill(SkillId skillId, CCSPlayerController player, string[] arguments) =>
        Invoke(skillId, nameof(SkillHookSet.TypeSkill), d => d.Hooks.TypeSkill?.Invoke(player, arguments));

    // OnTick has legacy caller-owned failure suppression: PlayerEvents logs a
    // failing skill only once per round, then continues with later skills. This
    // single-skill API therefore must not use Invoke(), which catches exceptions.
    public void InvokeTickUnchecked(SkillId skillId)
    {
        if (!registry.TryGet(skillId, out var definition)) return;
        definition.Hooks.OnTick?.Invoke();
    }

    // ---- Simple fan-out hooks: every distinct active skill, independently, in
    // the order given by the caller (already deduplicated upstream, matching
    // legacy DispatchToActiveSkills' `seen` HashSet<SkillId> collapsing). ----

    public void DispatchTick(IReadOnlyList<SkillId> activeSkillIds)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.OnTick), d => d.Hooks.OnTick?.Invoke());
    }

    public void DispatchNewRound(IReadOnlyList<SkillId> activeSkillIds)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.NewRound), d => d.Hooks.NewRound?.Invoke());
    }

    public void DispatchRoundEnd(IReadOnlyList<SkillId> activeSkillIds)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.RoundEnd), d => d.Hooks.RoundEnd?.Invoke());
    }

    public void DispatchPlayerBlind(IReadOnlyList<SkillId> activeSkillIds, EventPlayerBlind @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.PlayerBlind), d => d.Hooks.PlayerBlind?.Invoke(@event));
    }

    public void DispatchPlayerHurt(IReadOnlyList<SkillId> activeSkillIds, EventPlayerHurt @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.PlayerHurt), d => d.Hooks.PlayerHurt?.Invoke(@event));
    }

    public void DispatchPlayerDeath(IReadOnlyList<SkillId> activeSkillIds, EventPlayerDeath @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.PlayerDeath), d => d.Hooks.PlayerDeath?.Invoke(@event));
    }

    public void DispatchPlayerJump(IReadOnlyList<SkillId> activeSkillIds, EventPlayerJump @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.PlayerJump), d => d.Hooks.PlayerJump?.Invoke(@event));
    }

    public void DispatchBotTakeover(IReadOnlyList<SkillId> activeSkillIds, EventBotTakeover @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.BotTakeover), d => d.Hooks.BotTakeover?.Invoke(@event));
    }

    public void DispatchSwitchTeam(IReadOnlyList<SkillId> activeSkillIds, EventSwitchTeam @event, GameEventInfo info)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.SwitchTeam), d => d.Hooks.SwitchTeam?.Invoke(@event, info));
    }

    public void DispatchPlayerDisconnect(IReadOnlyList<SkillId> skillIds, uint playerIndex)
    {
        foreach (var id in skillIds)
            Invoke(id, nameof(SkillHookSet.PlayerDisconnect), d => d.Hooks.PlayerDisconnect?.Invoke(playerIndex));
    }

    public void DispatchWeaponFire(IReadOnlyList<SkillId> activeSkillIds, EventWeaponFire @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.WeaponFire), d => d.Hooks.WeaponFire?.Invoke(@event));
    }

    public void DispatchWeaponEquip(IReadOnlyList<SkillId> activeSkillIds, EventItemEquip @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.WeaponEquip), d => d.Hooks.WeaponEquip?.Invoke(@event));
    }

    public void DispatchWeaponPickup(IReadOnlyList<SkillId> activeSkillIds, EventItemPickup @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.WeaponPickup), d => d.Hooks.WeaponPickup?.Invoke(@event));
    }

    public void DispatchWeaponReload(IReadOnlyList<SkillId> activeSkillIds, EventWeaponReload @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.WeaponReload), d => d.Hooks.WeaponReload?.Invoke(@event));
    }

    public void DispatchGrenadeThrown(IReadOnlyList<SkillId> activeSkillIds, EventGrenadeThrown @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.GrenadeThrown), d => d.Hooks.GrenadeThrown?.Invoke(@event));
    }

    public void DispatchBulletImpact(IReadOnlyList<SkillId> activeSkillIds, EventBulletImpact @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.BulletImpact), d => d.Hooks.BulletImpact?.Invoke(@event));
    }

    public void DispatchPlayerMakeSound(IReadOnlyList<SkillId> activeSkillIds, CounterStrikeSharp.API.Modules.UserMessages.UserMessage message)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.PlayerMakeSound), d => d.Hooks.PlayerMakeSound?.Invoke(message));
    }

    public void DispatchCheckTransmit(IReadOnlyList<SkillId> activeSkillIds, CCheckTransmitInfoList infoList)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.CheckTransmit), d => d.Hooks.CheckTransmit?.Invoke(infoList));
    }

    public void DispatchOnEntitySpawned(IReadOnlyList<SkillId> activeSkillIds, CEntityInstance entity)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.OnEntitySpawned), d => d.Hooks.OnEntitySpawned?.Invoke(entity));
    }

    public void DispatchBombBeginplant(IReadOnlyList<SkillId> activeSkillIds, EventBombBeginplant @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.BombBeginplant), d => d.Hooks.BombBeginplant?.Invoke(@event));
    }

    public void DispatchBombAbortplant(IReadOnlyList<SkillId> activeSkillIds, EventBombAbortplant @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.BombAbortplant), d => d.Hooks.BombAbortplant?.Invoke(@event));
    }

    public void DispatchBombPlanted(IReadOnlyList<SkillId> activeSkillIds, EventBombPlanted @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.BombPlanted), d => d.Hooks.BombPlanted?.Invoke(@event));
    }

    public void DispatchBombBegindefuse(IReadOnlyList<SkillId> activeSkillIds, EventBombBegindefuse @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.BombBegindefuse), d => d.Hooks.BombBegindefuse?.Invoke(@event));
    }

    public void DispatchDecoyStarted(IReadOnlyList<SkillId> activeSkillIds, EventDecoyStarted @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.DecoyStarted), d => d.Hooks.DecoyStarted?.Invoke(@event));
    }

    public void DispatchDecoyDetonate(IReadOnlyList<SkillId> activeSkillIds, EventDecoyDetonate @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.DecoyDetonate), d => d.Hooks.DecoyDetonate?.Invoke(@event));
    }

    public void DispatchSmokegrenadeDetonate(IReadOnlyList<SkillId> activeSkillIds, EventSmokegrenadeDetonate @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.SmokegrenadeDetonate), d => d.Hooks.SmokegrenadeDetonate?.Invoke(@event));
    }

    public void DispatchSmokegrenadeExpired(IReadOnlyList<SkillId> activeSkillIds, EventSmokegrenadeExpired @event)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.SmokegrenadeExpired), d => d.Hooks.SmokegrenadeExpired?.Invoke(@event));
    }

    public void DispatchOnTriggerEnter(IReadOnlyList<SkillId> activeSkillIds, CBaseTrigger trigger, CBaseEntity entity)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.OnTriggerEnter), d => d.Hooks.OnTriggerEnter?.Invoke(trigger, entity));
    }

    public void DispatchOnTriggerExit(IReadOnlyList<SkillId> activeSkillIds, CBaseTrigger trigger, CBaseEntity entity)
    {
        foreach (var id in activeSkillIds)
            Invoke(id, nameof(SkillHookSet.OnTriggerExit), d => d.Hooks.OnTriggerExit?.Invoke(trigger, entity));
    }

    // ---- OnTakeDamage / OnTakeDamagePost: same fan-out, but LateDamageSkillIds
    // always run after every other active skill in this call (see class doc). ----

    public void DispatchOnTakeDamage(IReadOnlyList<SkillId> activeSkillIds, DynamicHook hook, bool post)
    {
        List<SkillId>? deferred = null;

        foreach (var id in activeSkillIds)
        {
            if (LateDamageSkillIds.Contains(id))
            {
                (deferred ??= []).Add(id);
                continue;
            }

            InvokeOnTakeDamage(id, hook, post);
        }

        if (deferred == null) return;
        foreach (var id in deferred)
            InvokeOnTakeDamage(id, hook, post);
    }

    private void InvokeOnTakeDamage(SkillId id, DynamicHook hook, bool post)
    {
        var hookName = post ? nameof(SkillHookSet.OnTakeDamagePost) : nameof(SkillHookSet.OnTakeDamage);
        Invoke(id, hookName, d => (post ? d.Hooks.OnTakeDamagePost : d.Hooks.OnTakeDamage)?.Invoke(hook));
    }

    // ---- PlayerHurtPre: victim's skill first; attacker's skill only if the
    // victim did NOT suppress AND the attacker holds a different skill. First
    // true wins; at most two skills are asked (see docs/dispatch-semantics.md). ----

    public bool DispatchPlayerHurtPre(SkillId victimSkillId, SkillId? attackerSkillId, EventPlayerHurt @event)
    {
        if (AskPlayerHurtPre(victimSkillId, @event)) return true;

        if (attackerSkillId is { } attacker && attacker != victimSkillId)
            return AskPlayerHurtPre(attacker, @event);

        return false;
    }

    private bool AskPlayerHurtPre(SkillId id, EventPlayerHurt @event)
    {
        if (!registry.TryGet(id, out var definition) || definition.Hooks.PlayerHurtPre == null)
            return false;

        try
        {
            return definition.Hooks.PlayerHurtPre(@event);
        }
        catch (Exception ex)
        {
            onHookException?.Invoke($"{id}.PlayerHurtPre failed: {ex.InnerException?.Message ?? ex.Message}");
            return false;
        }
    }

    // ---- OnWeaponCanAcquire: EVERY distinct active skill is asked (not just the
    // acquiring player's own skill); first true wins and short-circuits the rest
    // (see docs/dispatch-semantics.md). ----

    public bool DispatchOnWeaponCanAcquire(IReadOnlyList<SkillId> activeSkillIds, DynamicHook hook, CCSPlayerController player, CEconItemView item, CCSWeaponBaseVData vdata)
    {
        foreach (var id in activeSkillIds)
        {
            if (!registry.TryGet(id, out var definition) || definition.Hooks.OnWeaponCanAcquire == null)
                continue;

            bool result;
            try
            {
                result = definition.Hooks.OnWeaponCanAcquire(hook, player, item, vdata);
            }
            catch (Exception ex)
            {
                onHookException?.Invoke($"{id}.OnWeaponCanAcquire failed: {ex.InnerException?.Message ?? ex.Message}");
                continue;
            }

            if (result) return true;
        }

        return false;
    }
}
