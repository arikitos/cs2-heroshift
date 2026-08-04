using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using RayTraceAPI;
using System.Drawing;
using System.Numerics;
using TraceOptions = RayTraceAPI.TraceOptions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

using src.SkillsCore;
namespace src.utils
{
    /*
     * RayTrace - "what is this player looking at / what is between these two
     * points?" This is what every aim-based hero is built on (aimbot, demon eye,
     * teleport-to-crosshair, tripwire and so on).
     *
     * IMPORTANT: tracing is NOT built into CounterStrikeSharp. It is provided by an
     * external RayTrace module, obtained here through the plugin capability
     * "raytrace:craytraceinterface". If the operator has not installed it,
     * GetInterface() returns null, every entry point below returns null, and the
     * dependent heroes silently do nothing - GetInterface() logs the install hint
     * (with the list of affected heroes) exactly once per server session so the
     * console is not spammed. Check RayTrace.IsAvailable if a hero needs to degrade
     * gracefully.
     *
     * Entry points:
     *   EyeTrace(player)  - the common case. Traces from the player's eye position
     *                       (AbsOrigin + ViewOffset.Z, since AbsOrigin is at the
     *                       feet) along EyeAngles for up to the hero's "maxDistance"
     *                       value from the typed skill options, defaulting to 4096 units.
     *   TraceShape(...)   - arbitrary start/end, using a tiny 1-unit box.
     *   TraceHullShape(..)- arbitrary start/end with a caller-supplied hull, default
     *                       the player's own collision bounds. Use this to ask "does
     *                       a player-sized body fit / travel through here?", e.g.
     *                       for teleport destination checks.
     *
     * All three return a CustomTraceResult? - null means "could not trace at all"
     * (dead/invalid player, module missing, native error), which is NOT the same as
     * "hit nothing". Read result.DidHit for that, then use the Hit* helpers at the
     * bottom of this file to find out what was hit: HitPlayer, HitWeapon, HitWorld,
     * HitDoor, HitChicken, HitSky, HitGrenade, HitPlantedC4, ... Each is an
     * extension method that matches the hit entity's DesignerName and casts it, so
     * a hero can write `if (trace.HitPlayer(out var victim))`.
     *
     * Masks/contents (InteractsWith / InteractsExclude) decide what the ray
     * collides with. When a hero passes no mask, TraceShape derives it from the
     * player's own collision attributes and then adds Hitboxes and removes
     * PlayerClip - that combination is what makes the trace hit player bodies
     * (rather than just world geometry) while ignoring the invisible clip brushes
     * that block players but should not block aim.
     *
     * Setting ConfigurationStore.Settings.General.TraceRayBeam draws the ray (and, for
     * TraceHullShape, the hull's box edges) with env_beam entities. That is a debug
     * aid only. Note those debug beams are created directly here and are NOT
     * registered with EntityManager, so they are not cleaned up between rounds -
     * leave TraceRayBeam off on a live server.
     */
    public static class RayTrace
    {
        // Capability published by the external RayTrace module; resolved on each use
        // rather than cached, because the module may load after this plugin.
        private static PluginCapability<CRayTraceInterface> RayTraceInterface { get; } = new("raytrace:craytraceinterface");

        // Ensures the "module not installed" hint is printed only once per session.
        private static bool missingModuleLogged;

        // Returns the trace interface, or null when the module is absent. Get() throwing
        // is treated the same as absent.
        private static CRayTraceInterface? GetInterface()
        {
            try
            {
                var rayTrace = RayTraceInterface.Get();
                if (rayTrace != null) return rayTrace;
            }
            catch { }

            if (!missingModuleLogged)
            {
                missingModuleLogged = true;
                Server.PrintToConsole("[HeroShift] RayTrace module not found - skills that need it do nothing: " +
                    "LongZeus, LongKnife, Iana, Cypher, Noclip, Shade (and the skill-use button's aim check). " +
                    "Install RayTrace-CSS-API and RayTrace-MM: https://github.com/FUNPLAY-pro-CS2/Ray-Trace/releases");
            }

            return null;
        }

        // Heroes can check this to disable themselves cleanly when tracing is unavailable.
        public static bool IsAvailable => GetInterface() != null;

        // Traces a 1-unit box from startPos to endPos, ignoring the given player's own
        // pawn. Returns null if the player is not alive/valid or the module is missing.
        public static CustomTraceResult? TraceShape(CCSPlayerController player, Vector startPos, Vector endPos, ulong? mask = null, ulong? contents = null)
        {
            if (player == null || !player.IsValid) return null;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null ||
                !playerPawn.IsValid ||
                playerPawn.Handle == IntPtr.Zero ||
                playerPawn.Collision == null ||
                playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE ||
                playerPawn.CBodyComponent?.SceneNode == null)
                return null;

            var rayTrace = GetInterface();
            if (rayTrace == null)
                return null;

            // Default mask: start from whatever the player's own body collides with, then
            // add Hitboxes so the ray can hit player models, and clear PlayerClip so the
            // invisible brushes that block movement do not block aim. Falls back to
            // Solid|Hitboxes if the collision attributes cannot be read.
            if (mask == null)
            {
                try
                {
                    if (playerPawn.Collision?.CollisionAttribute != null)
                    {
                        mask = playerPawn.Collision.CollisionAttribute.InteractsWith | (ulong)InteractionLayers.Hitboxes;
                        mask &= ~(ulong)InteractionLayers.PlayerClip;
                    }
                    else
                        mask = (ulong)(InteractionLayers.Solid | InteractionLayers.Hitboxes);
                }
                catch
                {
                    mask = (ulong)(InteractionLayers.Solid | InteractionLayers.Hitboxes);
                }
            }
            contents ??= 0;

            bool drawBeam = ConfigurationStore.Settings.General.TraceRayBeam;

            TraceOptions options = new()
            {
                InteractsWith = (ulong)mask,
                InteractsExclude = (ulong)contents,
                DrawBeam = drawBeam == true ? 1 : 0,
            };

            // A 1x1x1 unit box rather than a true zero-width ray: a hull trace is more
            // forgiving at long range and avoids slipping through geometry seams.
            Vector mins = new(-0.5f, -0.5f, -0.5f);
            Vector maxs = new(0.5f, 0.5f, 0.5f);

            TraceResult result = default;

            try
            {
                rayTrace.TraceHullShape(startPos, endPos, mins, maxs, playerPawn, options, out result);
            }
            // The trace crosses into native code, so a bad handle surfaces as a managed
            // exception here; it is swallowed so one bad trace cannot take the server down.
            catch (Exception ex)
            {
                Console.WriteLine($"[HeroShift] A memory error was caught during RayTrace: {ex.Message}");
                return null;
            }

            return new CustomTraceResult(result, startPos, (ulong)mask, (ulong)contents, drawBeam);
        }

        // The main entry point for aim-based heroes: traces from the player's eyes along
        // their view angles and reports the first thing hit.
        public static CustomTraceResult? EyeTrace(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return null;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
                return null;

            var playerInfo = PlayerManager.GetPlayerByIndex(player.Index);
            if (playerInfo == null) return null;

            // Range comes from the acting hero's own "maxDistance" key in
            // typed options; 0/absent falls back to 4096 units.
            float maxDistance = SkillRuntime.GetMaxDistance(playerInfo.Skill);
            if (maxDistance == 0) maxDistance = 4096f;

            // AbsOrigin sits at the pawn's feet, so ViewOffset.Z must be added to get the
            // eye position - tracing from AbsOrigin would start inside the floor.
            Vector startPos = new(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z + playerPawn.ViewOffset.Z);
            // EyeAngles converted to a unit direction, scaled to the range.
            Vector endPos = startPos + SkillUtils.GetForwardVector(playerPawn.EyeAngles) * maxDistance;

            return TraceShape(player, startPos, endPos);
        }

        // Hull trace with a caller-chosen box. Defaults to the player's own collision
        // Mins/Maxs, which makes it the "would a player fit / could a player walk this
        // path?" test used for teleport destination validation. Unlike TraceShape it does
        // not add Hitboxes to the default mask, so with default arguments it collides
        // with world geometry the way a player body does.
        public static CustomTraceResult? TraceHullShape(Vector startPos, Vector endPos, CCSPlayerController player, Vector? mins = null, Vector? maxs = null, ulong? mask = null, ulong? contents = null, QAngle? angle = null)
        {
            if (player == null || !player.IsValid) return null;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null ||
                !playerPawn.IsValid ||
                playerPawn.Handle == IntPtr.Zero ||
                playerPawn.Collision == null ||
                playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE ||
                playerPawn.CBodyComponent?.SceneNode == null)
                return null;

            var rayTrace = GetInterface();
            if (rayTrace == null)
                return null;

            Vector safeMins = mins ?? playerPawn.Collision.Mins;
            Vector safeMaxs = maxs ?? playerPawn.Collision.Maxs;

            ulong safeMask = mask ?? playerPawn.Collision.CollisionAttribute.InteractsWith;
            ulong safeContents = contents ?? 0;

            bool drawBeam = ConfigurationStore.Settings.General.TraceRayBeam;

            TraceResult result = default;
            TraceOptions options = new()
            {
                InteractsWith = safeMask,
                InteractsExclude = safeContents,
                DrawBeam = drawBeam == true ? 1 : 0,
            };

            // The tracing player's own pawn is excluded so the ray does not immediately
            // hit the body it starts inside. (The guard above already required the pawn
            // to be alive, so this is effectively always the pawn.)
            CEntityInstance? entityToIgnore = (playerPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                                      ? playerPawn
                                      : null;

            try
            {
                rayTrace.TraceHullShape(startPos, endPos, safeMins, safeMaxs, entityToIgnore, options, out result);
            }
            catch (Exception)
            {
                return null;
            }

            if (drawBeam)
            {
                angle ??= new(0, playerPawn.EyeAngles.Y, 0);
                DrawBoxEdges(startPos, endPos, angle, safeMins, safeMaxs, Color.Green);
            }

            return new CustomTraceResult(result, startPos, safeMask, safeContents, drawBeam);
        }

        // Debug visual only: draws the swept hull as a wireframe box (4 edges at the start
        // face, 4 at the end face, 4 connecting them) using env_beam entities.
        private static void DrawBoxEdges(Vector start, Vector end, QAngle angles, Vector mins, Vector maxs, Color color)
        {
            AngleVectors(angles, out Vector forward, out Vector right, out Vector up);

            // The hull extends half its depth beyond each endpoint along the sweep
            // direction, so the drawn faces are pushed out to match the real swept volume.
            float halfLength = (maxs.X - mins.X) / 2.0f;

            Vector visualStart = start - (forward * halfLength);
            Vector visualEnd = end + (forward * halfLength);

            Vector GetVertex(Vector center, float rx, float ux)
            {
                return center + (right * rx) + (up * ux);
            }

            Vector[] s = [
                GetVertex(visualStart, mins.Y, mins.Z),
                GetVertex(visualStart, maxs.Y, mins.Z),
                GetVertex(visualStart, maxs.Y, maxs.Z),
                GetVertex(visualStart, mins.Y, maxs.Z)
            ];

            Vector[] e = [
                GetVertex(visualEnd, mins.Y, mins.Z),
                GetVertex(visualEnd, maxs.Y, mins.Z),
                GetVertex(visualEnd, maxs.Y, maxs.Z),
                GetVertex(visualEnd, mins.Y, maxs.Z)
            ];

            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                CreateBeamLine(s[i], s[next], color);
                CreateBeamLine(e[i], e[next], color);
                CreateBeamLine(s[i], e[i], color);
            }
        }

        // Standard Source engine angle-to-basis conversion. QAngle is (pitch, yaw, roll)
        // in degrees, hence the * PI / 180 conversions, and note that a positive pitch
        // points DOWN in Source - that is why forward.Z is -sin(pitch) rather than
        // +sin(pitch). Returns the three orthogonal axes of the rotated frame.
        private static void AngleVectors(QAngle angles, out Vector forward, out Vector right, out Vector up)
        {
            float sp, sy, cp, cy, sr, cr;

            float pitch = angles.X * (MathF.PI / 180.0f);
            float yaw = angles.Y * (MathF.PI / 180.0f);
            float roll = angles.Z * (MathF.PI / 180.0f);

            sp = MathF.Sin(pitch); cp = MathF.Cos(pitch);
            sy = MathF.Sin(yaw); cy = MathF.Cos(yaw);
            sr = MathF.Sin(roll); cr = MathF.Cos(roll);

            forward = new Vector(cp * cy, cp * sy, -sp);
            right = new Vector(-1 * sr * sp * cy + cr * sy, -1 * sr * sp * sy - cr * cy, -1 * sr * cp);
            up = new Vector(cr * sp * cy + sr * sy, cr * sp * sy - sr * cy, cr * cp);
        }

        // Draws one debug line. These beams are intentionally not tracked by
        // EntityManager, so they persist until the map changes - debug use only.
        private static void CreateBeamLine(Vector start, Vector end, Color color)
        {
            var beam = Utilities.CreateEntityByName<CBeam>("env_beam");
            if (beam == null || !beam.IsValid) return;

            beam.Render = color;
            beam.Width = 1.3f;

            beam.Teleport(start, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            beam.EndPos.X = end.X;
            beam.EndPos.Y = end.Y;
            beam.EndPos.Z = end.Z;

            beam.DispatchSpawn();
        }

        // Units from the trace start to where it stopped - i.e. distance to the thing hit,
        // or the full ray length when nothing was hit.
        public static float Distance(this CustomTraceResult result)
        {
            return Vector3.Distance(result.StartPos, result.EndPos);
        }

        // Subtracts the surface Normal from the end position and normalises. Note this
        // mixes a point and a direction, so it is not the ray's travel direction
        // (StartPos -> EndPos); prefer computing that yourself if that is what you need.
        public static Vector3 Direction(this CustomTraceResult result)
        {
            return Vector3.Normalize(result.EndPos - result.Normal);
        }

        // Shared implementation behind every Hit* helper: wraps the raw hit handle in T and
        // checks its DesignerName against designerName with the requested match mode.
        // CustomTraceResult only stores the hit entity as a raw nint, so the handle is
        // materialised into a typed wrapper via Activator.CreateInstance here.
        public static bool HitEntityByDesignerName<T>(this CustomTraceResult result, out T? entity, string designerName, DesignerNameMatchType matchType = DesignerNameMatchType.Equals) where T : CEntityInstance
        {
            T? val = (T?)Activator.CreateInstance(typeof(T), result.HitEntity);
            if ((object?)val != null && matchType switch
            {
                DesignerNameMatchType.Equals => val.DesignerName == designerName,
                DesignerNameMatchType.StartsWith => val.DesignerName.StartsWith(designerName, StringComparison.OrdinalIgnoreCase),
                DesignerNameMatchType.EndsWith => val.DesignerName.EndsWith(designerName, StringComparison.OrdinalIgnoreCase),
                _ => false,
            })
            {
                entity = val;
                return true;
            }

            entity = null;
            return false;
        }

        // "Did the ray hit any entity at all?" An empty DesignerName is treated as no
        // entity, which is how a miss shows up.
        public static bool HitEntity(this CustomTraceResult result, out CBaseEntity? entity)
        {
            CEntityInstance entityInstance = new(result.HitEntity);
            if (string.IsNullOrEmpty(entityInstance.DesignerName))
            {
                entity = null;
                return false;
            }

            entity = entityInstance.As<CBaseEntity>();
            return entity != null;
        }

        // The helper most heroes want: matches the "player" pawn and resolves it back to a
        // controller via OriginalController. Returns false when the ray hit something that
        // is not a player.
        public static bool HitPlayer(this CustomTraceResult result, out CCSPlayerController? player)
        {
            if (result.HitEntityByDesignerName<CCSPlayerPawn>(out CCSPlayerPawn? entity, "player"))
            {
                player = entity?.OriginalController.Value;
                return player != null;
            }

            player = null;
            return false;
        }

        // Any dropped/held weapon: all weapon classnames share the "weapon_" prefix, so
        // this is a StartsWith match rather than an exact one.
        public static bool HitWeapon(this CustomTraceResult result, out CBasePlayerWeapon? weapon)
        {
            return result.HitEntityByDesignerName<CBasePlayerWeapon>(out weapon, "weapon_", DesignerNameMatchType.StartsWith);
        }

        // The remaining helpers are all thin DesignerName matches over the same mechanism;
        // each names the CS2 entity classname it looks for.
        public static bool HitChicken(this CustomTraceResult result, out CChicken? chicken)
        {
            return result.HitEntityByDesignerName<CChicken>(out chicken, "chicken");
        }

        // Note: matches "func_door", not "func_button", so this currently detects the same
        // entity class as HitDoor below and only differs in the type it hands back.
        public static bool HitButton(this CustomTraceResult result, out CBaseButton? button)
        {
            return result.HitEntityByDesignerName<CBaseButton>(out button, "func_door");
        }

        public static bool HitBuyzone(this CustomTraceResult result, out CBuyZone? buyzone)
        {
            return result.HitEntityByDesignerName<CBuyZone>(out buyzone, "func_buyzone");
        }

        public static bool HitSky(this CustomTraceResult result, out CEnvSky? sky)
        {
            return result.HitEntityByDesignerName<CEnvSky>(out sky, "env_sky");
        }

        // Two overloads, distinguished only by the out type: sliding doors (func_door) and
        // rotating doors (func_door_rotating) are different entity classes in CS2.
        public static bool HitDoor(this CustomTraceResult result, out CBaseDoor? door)
        {
            return result.HitEntityByDesignerName<CBaseDoor>(out door, "func_door");
        }

        public static bool HitDoor(this CustomTraceResult result, out CRotDoor? door)
        {
            return result.HitEntityByDesignerName<CRotDoor>(out door, "func_door_rotating");
        }

        public static bool HitLadder(this CustomTraceResult result, out CFuncLadder? ladder)
        {
            return result.HitEntityByDesignerName<CFuncLadder>(out ladder, "func_ladder");
        }

        public static bool HitGrenade(this CustomTraceResult result, out CBaseCSGrenade? grenade)
        {
            return result.HitEntityByDesignerName<CBaseCSGrenade>(out grenade, "grenade");
        }

        public static bool HitPlantedC4(this CustomTraceResult result, out CPlantedC4? c4)
        {
            return result.HitEntityByDesignerName<CPlantedC4>(out c4, "planted_c4");
        }

        public static bool HitPointWorldText(this CustomTraceResult result, out CPointWorldText? pointWorldText)
        {
            return result.HitEntityByDesignerName<CPointWorldText>(out pointWorldText, "point_worldtext");
        }

        // The carryable bomb (weapon_c4), as opposed to HitPlantedC4's planted_c4.
        public static bool HitC4(this CustomTraceResult result, out CC4? c4)
        {
            return result.HitEntityByDesignerName<CC4>(out c4, "weapon_c4");
        }

        // True when the ray stopped on static map geometry ("worldent") rather than on any
        // dynamic entity - i.e. the player is aiming at a wall/floor.
        public static bool HitWorld(this CustomTraceResult result, out CWorld? world)
        {
            return result.HitEntityByDesignerName<CWorld>(out world, "worldent");
        }

        // How HitEntityByDesignerName compares the classname. StartsWith/EndsWith are
        // case-insensitive; Equals is an exact, case-sensitive comparison.
        public enum DesignerNameMatchType
        {
            Equals,
            StartsWith,
            EndsWith
        }
    }
}