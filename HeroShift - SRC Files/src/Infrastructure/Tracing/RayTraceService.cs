using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using RayTraceAPI;
using src.player;
using src.SkillsCore;
using src.utils;
using System.Drawing;
using TraceOptions = RayTraceAPI.TraceOptions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace src.Infrastructure.Tracing;

/// <summary>
/// Adapter for the external RayTrace capability. The capability key, collision
/// masks, native calls, failure behavior and operator warning are unchanged.
/// </summary>
public sealed class RayTraceService : ITraceService
{
        private readonly PluginCapability<CRayTraceInterface> capability =
            new("raytrace:craytraceinterface");
        private readonly Func<CRayTraceInterface?> interfaceResolver;
        private readonly Action<string> log;
        private int missingModuleLogged;

        public RayTraceService(
            Func<CRayTraceInterface?>? interfaceResolver = null,
            Action<string>? log = null)
        {
            this.interfaceResolver = interfaceResolver ?? ResolveCapability;
            this.log = log ?? Server.PrintToConsole;
        }

        private CRayTraceInterface? ResolveCapability()
        {
            try
            {
                return capability.Get();
            }
            catch
            {
                return null;
            }
        }

        private CRayTraceInterface? GetInterface()
        {
            var rayTrace = interfaceResolver();
            if (rayTrace != null) return rayTrace;

            if (Interlocked.Exchange(ref missingModuleLogged, 1) == 0)
            {
                log("[HeroShift] RayTrace module not found - skills that need it do nothing: " +
                    "LongZeus, LongKnife, Iana, Cypher, Noclip, Shade (and the skill-use button's aim check). " +
                    "Install RayTrace-CSS-API and RayTrace-MM: https://github.com/FUNPLAY-pro-CS2/Ray-Trace/releases");
            }

            return null;
        }

        // Heroes can check this to disable themselves cleanly when tracing is unavailable.
        public bool IsAvailable => GetInterface() != null;

        // Traces a 1-unit box from startPos to endPos, ignoring the given player's own
        // pawn. Returns null if the player is not alive/valid or the module is missing.
        public CustomTraceResult? TraceShape(CCSPlayerController player, Vector startPos, Vector endPos, ulong? mask = null, ulong? contents = null)
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
        public CustomTraceResult? EyeTrace(CCSPlayerController player)
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
        public CustomTraceResult? TraceHullShape(Vector startPos, Vector endPos, CCSPlayerController player, Vector? mins = null, Vector? maxs = null, ulong? mask = null, ulong? contents = null, QAngle? angle = null)
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
        private void DrawBoxEdges(Vector start, Vector end, QAngle angles, Vector mins, Vector maxs, Color color)
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
        private void AngleVectors(QAngle angles, out Vector forward, out Vector right, out Vector up)
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
        private void CreateBeamLine(Vector start, Vector end, Color color)
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

}
