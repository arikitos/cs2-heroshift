using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using src.player.skills;
using src.utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static src.HeroShift;

namespace src.player
{
    /*
     * EntityEvents.cs - world/entity-side routing for the `Event` partial class.
     *
     * Same job as PlayerEvents.cs (fan a game callback out to the heroes in play via
     * DispatchToActiveSkills -> reflection into src/player/skills/<Name>.cs) but for
     * everything that is about the WORLD rather than a player:
     *   objective     - BombBeginplant / BombAbortplant / BombPlanted / BombBegindefuse
     *   grenades      - Decoy and Smokegrenade start/detonate/expire
     *   damage        - OnTakeDamage (Pre) and OnTakeDamagePost, native TakeDamage hook
     *   triggers      - OnTriggerEnter / OnTriggerExit, native CBaseTrigger touch hooks
     *   items         - OnWeaponCanAcquire, native pickup/buy gate
     *   spawning      - OnEntitySpawned
     *   visibility    - CheckTransmit
     *
     * NATIVE HOOKS vs GAME EVENTS
     *   The methods taking a DynamicHook are hooks on engine functions, not game
     *   events. Their arguments come out positionally through hook.GetParam<T>(n),
     *   so the index IS the C++ parameter order:
     *     TakeDamage  - 0 = victim CEntityInstance, 1 = CTakeDamageInfo
     *     trigger     - 0 = the CBaseTrigger, 1 = the touching entity
     *     CanAcquire  - 0 = CCSPlayer_ItemServices, 1 = CEconItemView
     *   Unlike a game event, returning HookResult.Handled from a native hook actually
     *   BLOCKS the engine call - that is how OnWeaponCanAcquire denies a weapon.
     *
     * CHECKTRANSMIT
     *   CheckTransmit runs per client per tick and decides which entities each client
     *   is even told about - it is how invisibility and per-player visuals work, and
     *   it is the most expensive callback in the plugin. It is therefore registered
     *   only on demand: RoundStart removes it, EnableTransmit() re-adds it (guarded by
     *   isTransmitRegistered so it is never double-registered).
     */
    public static partial class Event
    {
        // Objective events below are plain pass-through routers to the heroes.
        private static HookResult BombBeginplant(EventBombBeginplant @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchBombBeginplant(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult BombAbortplant(EventBombAbortplant @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchBombAbortplant(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult BombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchBombPlanted(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult BombBegindefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchBombBegindefuse(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        // Grenade lifecycle routers - used by heroes that replace or react to utility.
        private static HookResult DecoyStarted(EventDecoyStarted @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchDecoyStarted(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult DecoyDetonate(EventDecoyDetonate @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchDecoyDetonate(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult SmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchSmokegrenadeDetonate(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult SmokegrenadeExpired(EventSmokegrenadeExpired @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchSmokegrenadeExpired(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        // Native TakeDamage, PRE stage: heroes can still edit CTakeDamageInfo.Damage
        // here (reduce, amplify, zero it). Ordering matters, so the actual fan-out lives
        // in DispatchOnTakeDamage (PlayerEvents.cs), which runs the revive-style heroes
        // last so they see the final damage value.
        private static HookResult OnTakeDamage(DynamicHook h)
        {
            lock (setLock)
            {
                DispatchOnTakeDamage(h);

                // Fortnite spawns barricade walls that absorb damage, and those walls
                // outlive their owner's hero: skillInThisRound stays true until NewRound.
                // DispatchOnTakeDamage only walks heroes players CURRENTLY hold, so once
                // nobody holds Fortnite its walls would stop absorbing - hence the explicit
                // call. The Any() guard is what prevents a double invoke while it is held.
                if (Fortnite.skillInThisRound == true &&
                    !Instance.SkillPlayer.Any(p => !p.IsDrawing && p.Skill == BuiltInSkillIds.Fortnite))
                    Instance.SkillDispatcher.DispatchOnTakeDamage([BuiltInSkillIds.Fortnite], h, post: false);

                return HookResult.Continue;
            }
        }

        // POST stage: the damage is already applied, so this is for reacting to the
        // outcome (lifesteal, on-hit effects), not for changing the number.
        private static HookResult OnTakeDamagePost(DynamicHook h)
        {
            lock (setLock)
            {
                DispatchOnTakeDamage(h, true);
                return HookResult.Continue;
            }
        }

        // Trigger touch routers. Heroes create their own trigger volumes (zones, traps,
        // teleports) via SkillUtils.CreateTrigger and identify their own here.
        // Enter is hooked Post (the touch already registered), Exit is hooked Pre.
        private static HookResult OnTriggerEnter(DynamicHook hook)
        {
            lock (setLock)
            {
                CBaseTrigger trigger = hook.GetParam<CBaseTrigger>(0);
                CBaseEntity entity = hook.GetParam<CBaseEntity>(1);

                Instance.SkillDispatcher.DispatchOnTriggerEnter(GetActiveSkillIds(), trigger, entity);
                return HookResult.Continue;
            }
        }

        private static HookResult OnTriggerExit(DynamicHook hook)
        {
            lock (setLock)
            {
                CBaseTrigger trigger = hook.GetParam<CBaseTrigger>(0);
                CBaseEntity entity = hook.GetParam<CBaseEntity>(1);

                Instance.SkillDispatcher.DispatchOnTriggerExit(GetActiveSkillIds(), trigger, entity);
                return HookResult.Continue;
            }
        }

        // Gate on picking up or buying a weapon. Resolves the item and the acquiring
        // player, then asks each hero in play; the FIRST hero to return true wins and
        // the call returns Handled, which blocks the acquisition in the engine.
        // The playerInfo lookup below uses player.Index directly - the controller owning
        // the ItemServices - instead of routing through PlayerManager.GetPlayerEvent.
        // Its value is never read; it only serves as a "this player is tracked by the
        // plugin" guard, so heroes are not consulted for an unregistered controller.
        private static HookResult OnWeaponCanAcquire(DynamicHook hook)
        {
            lock (setLock)
            {
                CCSPlayer_ItemServices itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
                if (itemServices == null || itemServices.Pawn.Value == null || !itemServices.Pawn.Value.IsValid) return HookResult.Continue;

                CEconItemView econItem = hook.GetParam<CEconItemView>(1);
                if (econItem == null) return HookResult.Continue;

                CBasePlayerPawn pawn = itemServices.Pawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return HookResult.Continue;

                CCSPlayerController player = pawn.Controller.Value.As<CCSPlayerController>();
                if (player == null || !player.IsValid) return HookResult.Continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(player.Index);
                if (playerInfo == null) return HookResult.Continue;

                // CEconItemView only carries the definition index, so the weapon's actual
                // stats (slot, type, price) are looked up from the game's weapon data
                // table and passed to the heroes, letting them decide by category rather
                // than by hardcoded item ids.
                CCSWeaponBaseVData vdata = VirtualFunctions.GetCSWeaponDataFromKeyFunc.Invoke(-1, econItem.ItemDefinitionIndex.ToString());
                if (vdata == null || vdata.Handle == IntPtr.Zero) return HookResult.Continue;

                bool block = Instance.SkillDispatcher.DispatchOnWeaponCanAcquire(
                    GetActiveSkillIds(), hook, player, econItem, vdata);

                return block ? HookResult.Handled : HookResult.Continue;
            }
        }

        // Every entity the map or the game creates passes through here, so heroes that
        // need to react to (or reskin/reparent) grenades, projectiles and props filter by
        // DesignerName themselves. Fires for the plugin's own spawns too.
        private static void EntitySpawned(CEntityInstance entity)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchOnEntitySpawned(GetActiveSkillIds(), entity);
            }
        }

        // Per-client visibility filter, called once per tick with one TransmitEntities
        // set per receiving client. Removing an index from a client's set means that
        // client is never told the entity exists - the mechanism behind invisibility and
        // per-player visuals.
        //
        // The built-in part handles entity DELETION: an entity killed via EntityManager
        // is only actually freed a moment later by the engine, and during that window
        // clients can still receive it and render a ghost. So recently-destroyed indices
        // are stripped from every client's set until the kill lands. Hero-specific
        // filtering then runs through the normal fan-out.
        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            long perfStart = PerfLog.Start();
            lock (setLock)
            {
                try
                {
                    // Keep dying entities out of snapshots until the engine processes the kill.
                    var dying = EntityManager.GetRecentlyDestroyedSnapshot();
                    if (dying.Count > 0)
                    {
                        foreach (var (info, player) in infoList)
                        {
                            if (player == null || !player.IsValid) continue;
                            foreach (var entityIndex in dying)
                                if (info.TransmitEntities.Contains(entityIndex))
                                    info.TransmitEntities.Remove(entityIndex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Server.PrintToConsole($"[HeroShift] CheckTransmit dying-filter failed: {ex.Message}");
                }

                Instance.SkillDispatcher.DispatchCheckTransmit(GetActiveSkillIds(), infoList);
            }
            PerfLog.Sample("CheckTransmit", perfStart);
        }

        // Registers the CheckTransmit listener on demand. The isTransmitRegistered guard
        // is required because CounterStrikeSharp would otherwise stack duplicate
        // registrations and run the whole filter several times per tick.
        public static void EnableTransmit()
        {
            if (!isTransmitRegistered)
            {
                Instance?.RegisterListener<CheckTransmit>(CheckTransmit);
                isTransmitRegistered = true;
            }
        }
    }
}
