using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;
using static src.HeroShift;

namespace src.utils
{
    /*
     * EntityManager - ownership tracking and cleanup for every entity the plugin
     * spawns (beams, particles, props, chickens, triggers).
     *
     * Why it exists: CS2 entities the plugin creates are not tied to the round.
     * Without tracking, a hero that spawns a beam or a trigger leaves it in the
     * world forever, entity indexes run out, and the server eventually breaks. So
     * the rule for hero authors is: never create a plugin entity with
     * Utilities.CreateEntityByName directly - use one of the CreateTracked*
     * helpers, and the round-end cleanup handles the rest.
     *
     *   CreateTrackedParticleSystem / CreateTrackedDynamicProp /
     *   CreateTrackedPropOverride / CreateTrackedChicken /
     *   CreateTrackedPhysicsProp / CreateTrackedTrigger / CreateTrackedBeam
     *
     * Each takes a playerIndex - the owner. Pass the owning player's entity index
     * for per-player things, or the SystemOwnerIndex constant (uint.MaxValue) for
     * plugin-owned entities that belong to no particular player. GetEntityOwner
     * deliberately returns null for SystemOwnerIndex, so "who owns this?" answers
     * "nobody" rather than a bogus player index. RegisterExisting() adopts an
     * entity that was created elsewhere.
     *
     * Cleanup:
     *   DestroyEntity()         - one entity
     *   DestroyPlayerEntities() - everything one owner made
     *   DestroyAllTracked()/Clear() - everything, used between rounds
     *
     * Engine details worth knowing before touching this file:
     *   - Entities are not freed synchronously. DestroyEntity queues a "Kill" IO
     *     event with a small delay, and first sends "ClearParent" so a child is
     *     never left attached to a parent that is about to be freed. Order is
     *     child-first (reverse creation order) for the same reason.
     *   - Between the Kill request and the engine actually removing the entity, the
     *     entity still exists and would be sent to clients. recentlyDestroyed keeps
     *     those indexes for delay + 2s and Event.CheckTransmit reads the
     *     GetRecentlyDestroyedSnapshot() list to keep them out of transmit, which
     *     avoids clients rendering a corpse entity.
     *   - EntityBudget (3500) is a self-imposed ceiling below the engine's entity
     *     limit. Every CreateTracked* call checks OverBudget() first and returns
     *     null instead of spawning, so heroes must handle a null return. The count
     *     is cached for 64 ticks because enumerating all entities is expensive.
     *     Note CreateTrackedBeam does not perform this check.
     *   - SuppressKills makes DestroyEntity untrack but not kill. RoundEvents raises
     *     it around the map-change sweep, where the level change already destroyed
     *     everything and killing freed handles would only log errors.
     */
    public static class EntityManager
    {
        // Owner value for entities that belong to the plugin rather than to a player.
        // uint.MaxValue can never collide with a real entity index.
        public const uint SystemOwnerIndex = uint.MaxValue;
        // Entity index -> ownership record.
        private static readonly ConcurrentDictionary<uint, EntityData> trackedEntities = [];

        private struct EntityData
        {
            public uint EntityIndex;
            public uint PlayerIndex;
            public string EntityType;
            public DateTime CreatedAt;
        }

        // Self-imposed ceiling, deliberately below the engine's hard entity limit so
        // that map entities and normal gameplay entities still have room.
        private const int EntityBudget = 3500;
        private static int _cachedCount;
        private static int _cachedCountTick = -1000000;

        // True when the server is at/over the entity budget, in which case the
        // CreateTracked* helpers refuse to spawn. Enumerating all entities is far too
        // slow to do per spawn, so the result is cached for 64 ticks.
        public static bool OverBudget()
        {
            int tick = Server.TickCount;
            // Second condition catches TickCount going backwards (map change/restart),
            // which would otherwise leave the cache valid for a very long time.
            if (tick - _cachedCountTick > 64 || tick < _cachedCountTick)
            {
                _cachedCountTick = tick;
                try { _cachedCount = Utilities.GetAllEntities().Count(); }
                catch { _cachedCount = 0; }
            }
            return _cachedCount >= EntityBudget;
        }

        // Records ownership of an already-created entity. Index 0 is the worldspawn /
        // invalid slot and is never tracked.
        public static void RegisterEntity(uint entityIndex, uint playerIndex, string entityType)
        {
            if (entityIndex == 0) return;

            trackedEntities[entityIndex] = new EntityData
            {
                EntityIndex = entityIndex,
                PlayerIndex = playerIndex,
                EntityType = entityType,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Adopts an entity created outside this class (e.g. by a native call) so it
        // still gets cleaned up with everything else.
        public static void RegisterExisting(CBaseEntity? entity, uint playerIndex, string entityType)
        {
            if (entity == null || !entity.IsValid) return;
            RegisterEntity(entity.Index, playerIndex, entityType);
        }

        // All entity indexes owned by one player, optionally filtered to a single
        // entityType string (the same label passed at creation time).
        public static List<uint> GetPlayerEntities(uint playerIndex, string? entityType = null)
        {
            return [.. trackedEntities
                .Where(kvp => kvp.Value.PlayerIndex == playerIndex && (string.IsNullOrEmpty(entityType) || kvp.Value.EntityType == entityType))
                .Select(kvp => kvp.Key)];
        }

        public static int GetTrackedCount(uint playerIndex) => GetPlayerEntities(playerIndex).Count;

        // Owning player index, or null both when the entity is untracked and when it is
        // plugin-owned (SystemOwnerIndex) - callers only ever want a real player here.
        public static uint? GetEntityOwner(uint entityIndex)
        {
            if (!trackedEntities.TryGetValue(entityIndex, out var data)) return null;
            return data.PlayerIndex == SystemOwnerIndex ? null : data.PlayerIndex;
        }

        public static (int totalTracked, int ownerCount) GetStatistics()
        {
            return (trackedEntities.Count, trackedEntities.Values.Select(e => e.PlayerIndex).Distinct().Count());
        }


        // Spawns an info_particle_system playing particleName (a .vpcf path), already
        // started. Pass autoDestroySeconds for a one-shot effect that removes itself.
        // Returns null when over budget or the spawn failed - callers must check.
        public static CParticleSystem? CreateTrackedParticleSystem(uint playerIndex, string particleName, float? autoDestroySeconds = null)
        {
            try
            {
                if (OverBudget()) return null;
                var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
                if (particle == null || !particle.IsValid) return null;

                particle.EffectName = particleName;
                particle.StartActive = true;
                particle.DispatchSpawn();

                RegisterEntity(particle.Index, playerIndex, "particle_system");
                ScheduleAutoDestroy(particle.Index, autoDestroySeconds);
                return particle;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[EntityManager] CreateTrackedParticleSystem: {ex.Message}");
                return null;
            }
        }

        // Spawns a prop but deliberately does NOT DispatchSpawn it: the caller still has
        // to set the model and then spawn it. The other helpers here do spawn for you.
        public static CDynamicProp? CreateTrackedDynamicProp(uint playerIndex, string designerName = "prop_dynamic")
        {
            try
            {
                if (OverBudget()) return null;
                var prop = Utilities.CreateEntityByName<CDynamicProp>(designerName);
                if (prop == null || !prop.IsValid) return null;

                RegisterEntity(prop.Index, playerIndex, designerName);
                return prop;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[EntityManager] CreateTrackedDynamicProp: {ex.Message}");
                return null;
            }
        }

        // prop_dynamic_override variant - accepts models that plain prop_dynamic
        // rejects. Also left unspawned for the caller.
        public static CDynamicProp? CreateTrackedPropOverride(uint playerIndex)
        {
            return CreateTrackedDynamicProp(playerIndex, "prop_dynamic_override");
        }

        // Spawns a live chicken (spawned immediately, unlike the prop helpers).
        public static CChicken? CreateTrackedChicken(uint playerIndex)
        {
            try
            {
                if (OverBudget()) return null;
                var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
                if (chicken == null || !chicken.IsValid) return null;

                chicken.DispatchSpawn();
                RegisterEntity(chicken.Index, playerIndex, "chicken");
                return chicken;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[EntityManager] CreateTrackedChicken: {ex.Message}");
                return null;
            }
        }

        // Physics-simulated prop; also left unspawned so the caller can set model and
        // physics properties before DispatchSpawn.
        public static CPhysicsPropMultiplayer? CreateTrackedPhysicsProp(uint playerIndex)
        {
            try
            {
                if (OverBudget()) return null;
                var prop = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
                if (prop == null || !prop.IsValid) return null;

                RegisterEntity(prop.Index, playerIndex, "prop_physics_multiplayer");
                return prop;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[EntityManager] CreateTrackedPhysicsProp: {ex.Message}");
                return null;
            }
        }

        // Spawns a spherical (capsule) trigger_multiple of the given radius at pos -
        // the "did anyone walk into my area?" primitive used by area heroes.
        //
        // The collision fields below are the combination CS2 requires for a
        // script-created trigger to actually fire touch events; a trigger created
        // without them exists but never triggers. Globalname is made unique by
        // appending the entity index so a hero can identify its own trigger in the
        // touch output.
        public static CTriggerMultiple? CreateTrackedTrigger(uint playerIndex, string name, float radius, Vector pos)
        {
            if (pos == null) return null;

            try
            {
                if (OverBudget()) return null;
                var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");
                if (trigger == null || trigger.AbsOrigin == null) return null;

                trigger.Collision.SolidType = SolidType_t.SOLID_CAPSULE;
                trigger.Collision.SolidFlags = 0;
                // Spawnflags 1 = trigger responds to players (clients).
                trigger.Spawnflags = 1;
                trigger.Globalname = $"{name}_{trigger.Index}";
                // SolidFlags is assigned twice; only this second assignment (1) takes
                // effect, the 0 above is overwritten.
                trigger.Collision.SolidFlags = 1;

                // Written field-by-field rather than via Teleport() because the trigger
                // has not been spawned yet.
                trigger.AbsOrigin.X = pos.X;
                trigger.AbsOrigin.Y = pos.Y;
                trigger.AbsOrigin.Z = pos.Z;

                // Both the capsule radius and the bounding radius must be set, otherwise
                // the broadphase bounds do not match the capsule and touches are missed.
                trigger.Collision.CapsuleRadius = radius;
                trigger.Collision.BoundingRadius = radius;
                trigger.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_TRIGGER;
                trigger.Collision.EnablePhysics = 1;
                trigger.Collision.TriggerBloat = 0;
                trigger.Collision.SurroundType = SurroundingBoundsType_t.USE_OBB_COLLISION_BOUNDS;
                // Raw engine collision-attribute values; 39 as the function mask and 2 as
                // the collision group are the values a working CS2 trigger reports. They
                // have no named enum exposed here, so they are written as literals.
                trigger.Collision.CollisionAttribute.CollisionFunctionMask = 39;
                trigger.Collision.CollisionAttribute.CollisionGroup = 2;

                trigger.DispatchSpawn();
                RegisterEntity(trigger.Index, playerIndex, "trigger_multiple");
                return trigger;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[EntityManager] CreateTrackedTrigger: {ex.Message}");
                return null;
            }
        }

        // Spawns a "beam" entity as a coloured line from start to end - the standard way
        // heroes draw laser/tripwire/tracer visuals. The start point is set with
        // Teleport and the end point by writing EndPos directly, because a beam's two
        // endpoints are separate fields rather than a position plus an angle.
        // Unlike the other helpers this one does not check OverBudget().
        public static CBeam? CreateTrackedBeam(uint playerIndex, Vector start, Vector end, Color color)
        {
            try
            {
                var beam = Utilities.CreateEntityByName<CBeam>("beam");
                if (beam == null || !beam.IsValid) return null;

                beam.Render = color;
                beam.Width = 2.0f;
                beam.EndWidth = 2.0f;
                beam.Teleport(start);

                beam.EndPos.X = end.X;
                beam.EndPos.Y = end.Y;
                beam.EndPos.Z = end.Z;

                beam.DispatchSpawn();
                RegisterEntity(beam.Index, playerIndex, "beam");
                return beam;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[EntityManager] CreateTrackedBeam: {ex.Message}");
                return null;
            }
        }

        // Dying entities stay out of transmit until the engine processes the kill (Event.CheckTransmit).
        private static readonly ConcurrentDictionary<uint, DateTime> recentlyDestroyed = new();

        // Indexes that have been asked to die but may still exist server-side.
        // Event.CheckTransmit hides these from clients. Doubles as the cleanup pass for
        // the dictionary: entries whose grace period has elapsed are dropped here.
        public static List<uint> GetRecentlyDestroyedSnapshot()
        {
            if (recentlyDestroyed.IsEmpty) return [];

            var now = DateTime.UtcNow;
            var result = new List<uint>();
            foreach (var kvp in recentlyDestroyed)
            {
                if (now > kvp.Value) recentlyDestroyed.TryRemove(kvp.Key, out _);
                else result.Add(kvp.Key);
            }
            return result;
        }

        // While true, DestroyEntity only untracks and never touches the engine.
        public static bool SuppressKills = false;

        // Untracks the entity and asks the engine to remove it after `delay` seconds.
        // Untracking happens first and unconditionally, so a failed or suppressed kill
        // never leaves a stale ownership record behind.
        public static bool DestroyEntity(uint entityIndex, float delay = 0.1f)
        {
            trackedEntities.TryRemove(entityIndex, out _);

            if (SuppressKills)
                return false;

            try
            {
                var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)entityIndex);
                if (entity != null && entity.IsValid)
                {
                    // Kept hidden for the kill delay plus a 2s safety margin, since the
                    // engine processes the Kill input asynchronously.
                    recentlyDestroyed[entityIndex] = DateTime.UtcNow.AddSeconds(delay + 2.0);
                    // Detach first so no follower is left on a freed parent.
                    entity.AcceptInput("ClearParent");
                    entity.AddEntityIOEvent("Kill", entity, delay: delay);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[EntityManager] DestroyEntity {entityIndex}: {ex.Message}");
            }

            return false;
        }

        // Convenience overload for nullable indexes; the type parameter is not used
        // beyond constraining callers.
        public static bool DestroyEntity<T>(uint? entityIndex) where T : CBaseEntity
        {
            if (entityIndex == null) return false;
            return DestroyEntity(entityIndex.Value);
        }

        // Removes everything one owner created. Call this when a player dies,
        // disconnects or loses their hero.
        public static void DestroyPlayerEntities(uint playerIndex)
        {
            // Children die first (reverse creation order), same frame.
            var ordered = trackedEntities
                .Where(kvp => kvp.Value.PlayerIndex == playerIndex)
                .OrderByDescending(kvp => kvp.Value.CreatedAt)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var entityIndex in ordered)
                DestroyEntity(entityIndex);
        }

        // Round/map cleanup: removes every tracked entity. Kills are staggered across
        // owners (0.1s plus up to 15 * 0.03s) so a full server's worth of entities is
        // not removed in a single frame, which would cause a visible hitch.
        public static void DestroyAllTracked()
        {
            // Stagger between owners; each owner's chain dies child-first in one frame.
            int group = 0;
            foreach (var owner in trackedEntities.Values.Select(e => e.PlayerIndex).Distinct().ToList())
            {
                float delay = 0.1f + (group++ % 16) * 0.03f;
                var ordered = trackedEntities
                    .Where(kvp => kvp.Value.PlayerIndex == owner)
                    .OrderByDescending(kvp => kvp.Value.CreatedAt)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var entityIndex in ordered)
                    DestroyEntity(entityIndex, delay);
            }

            trackedEntities.Clear();
        }

        // Alias for DestroyAllTracked().
        public static void Clear()
        {
            DestroyAllTracked();
        }

        // Arms the optional self-destruct timer used by CreateTrackedParticleSystem.
        // STOP_ON_MAPCHANGE prevents the timer firing against an index that belongs to a
        // different entity on the next map.
        private static void ScheduleAutoDestroy(uint entityIndex, float? seconds)
        {
            if (seconds is not > 0) return;
            Instance?.AddTimer(seconds.Value, () => DestroyEntity(entityIndex), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }
    }
}
