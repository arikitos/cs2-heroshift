using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * HomingNades - Your grenades steer themselves toward the nearest enemy.
     *
     * LOGIC
     *   OnEntitySpawned: registers the thrown projectile.
     *   OnTick: steers it toward the closest enemy with 'strength' and detonates
     *     it once it is within detonationRange.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   strength        = 150
     *                       -> how aggressively the grenade turns toward the
     *                          target
     *   maxVelocity     = 2000
     *                       -> speed cap for the homing grenade (units/s)
     *   detonationRange = 130
     *                       -> distance to the target at which it explodes (game
     *                          units)
     *   grenadeLimit    = 2
     *                       -> how many grenades the hero gets
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.None
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = -1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class HomingNades : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.HomingNades;
        private static HomingNadesOptions Options => SkillConfigurationResolver.Get<HomingNadesOptions>(BuiltInSkillIds.HomingNades);
        private readonly static ConcurrentDictionary<uint, Vector> nades = [];
        private readonly static ConcurrentDictionary<uint, int> playersWithSkill = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            nades.Clear();
        }

        public static void OnTick()
        {
            if (Server.TickCount % 10 != 0) return;

            foreach (var index in nades.Keys.ToList())
            {
                if (!nades.TryGetValue(index, out var data)) continue;
                Vector oldPos = data;

                var nade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)index);
                if (nade == null || !nade.IsValid || nade.AbsOrigin == null)
                {
                    nades.TryRemove(index, out _);
                    continue;
                }

                Vector currentPos = new(nade.AbsOrigin.X, nade.AbsOrigin.Y, nade.AbsOrigin.Z);
                double distanceMoved = SkillUtils.GetDistance(currentPos, oldPos);
                Vector? calculatedVelocity = CalculateVelocity(nade, nade.TeamNum);

                bool isZero = calculatedVelocity?.IsZero() == true;

                if (distanceMoved < 4 || calculatedVelocity == null || isZero)
                {
                    nade.DetonateTime = isZero ? 0f : nade.CreateTime + 1.5f;
                    Utilities.SetStateChanged(nade, "CBaseGrenade", "m_flDetonateTime");

                    nades.TryRemove(index, out _);
                    continue;
                }

                Vector currentVel = new(nade.Velocity.X, nade.Velocity.Y, nade.Velocity.Z);
                float maxVelocity = Options.MaxVelocity;
                Vector newVelocity = currentVel + calculatedVelocity;

                float speed = newVelocity.Length();
                if (speed > maxVelocity)
                    newVelocity *= (maxVelocity / speed);

                nades[index] = currentPos;
                nade.Teleport(null, null, newVelocity);
            }
        }

        private static Vector? CalculateVelocity(CBaseCSGrenadeProjectile nade, int team)
        {
            if (nade.AbsOrigin == null) return null;

            Vector? closetEnemyPos = null;
            double minDistance = int.MaxValue;
            Vector nadePos = nade.AbsOrigin;

            foreach (var enemy in PlayerManager.GetTickPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.TeamNum != team))
            {
                var pawn = enemy.PlayerPawn.Value;
                if (pawn?.IsValid != true || pawn.AbsOrigin == null) continue;

                double dist = SkillUtils.GetDistance(nadePos, pawn.AbsOrigin);
                if (dist < Options.DetonationRange)
                {
                    nades.TryRemove(nade.Index, out _);
                    return Vector.Zero;
                }

                if (dist < minDistance)
                {
                    minDistance = dist;
                    closetEnemyPos = pawn.AbsOrigin;
                }
            }

            if (closetEnemyPos == null)
                return null;

            Vector direction = closetEnemyPos - nadePos;
            float length = direction.Length();

            if (length > 0)
            {
                float strength = Options.Strength;
                return new Vector(
                    (direction.X / length) * strength,
                    (direction.Y / length) * strength,
                    (direction.Z / length) * strength
                );
            }

            return Vector.Zero;
        }

        public static void OnEntitySpawned(CEntityInstance @event)
        {
            var name = @event.DesignerName;
            if (!name.EndsWith("_projectile") || name == "smokegrenade_projectile") return;

            var grenade = @event.As<CBaseCSGrenadeProjectile>();
            if (grenade == null || !grenade.IsValid) return;

            if (grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid) return;
            var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();

            if (pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;
            var player = pawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = PlayerManager.GetPlayerByIndex((PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index));
            if (playerInfo?.Skill != skillName) return;

            Vector pos = new(grenade.AbsOrigin?.X, grenade.AbsOrigin?.Y, grenade.AbsOrigin?.Z);
            nades.TryAdd(grenade.Index, pos);

            Server.NextWorldUpdate(() =>
            {
                if (grenade == null || !grenade.IsValid) return;
                grenade.DetonateTime += 30f;
                Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
            });
        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Weapon;
            if (weapon != "hegrenade") return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
            {
                playersWithSkill[player.Index] = grenadesLeft - 1;
                player!.GiveNamedItem($"weapon_{weapon}");
                SkillUtils.UpdateGrenadeCount(player, CsItem.HEGrenade, grenadesLeft - 1);
            }
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Item;
            if (player == null || !player.IsValid) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.HEGrenade, grenadesLeft);
        }

        public static void WeaponPickup(EventItemPickup @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Item;
            if (string.IsNullOrEmpty(weapon) || weapon != "hegrenade") return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.HEGrenade, grenadesLeft);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            int grenadeLimit = Options.GrenadeLimit;
            playersWithSkill.TryAdd(player.Index, grenadeLimit);

            SkillUtils.TryGiveWeapon(player, CsItem.HEGrenade);
            SkillUtils.UpdateGrenadeCount(player, CsItem.HEGrenade, grenadeLimit);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            playersWithSkill.TryRemove(player.Index, out _);
            SkillUtils.UpdateGrenadeCount(player, CsItem.HEGrenade, 1);
        }
    }
}