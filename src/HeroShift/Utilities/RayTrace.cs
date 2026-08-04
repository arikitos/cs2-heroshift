using CounterStrikeSharp.API.Core;
using HeroShift.src.utils;
using System.Numerics;

namespace src.utils;

/// <summary>
/// Pure helpers for interpreting an immutable trace result. Native tracing is
/// owned by ITraceService/RayTraceService.
/// </summary>
public static class RayTraceResultExtensions
{
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
