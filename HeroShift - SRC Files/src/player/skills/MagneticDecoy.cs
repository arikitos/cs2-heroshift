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
     * MagneticDecoy - Your decoys pull nearby players toward them.
     *
     * LOGIC
     *   DecoyStarted/DecoyDetonate: registers the active decoy.
     *   OnTick: drags players inside triggerRadius toward it with 'strenght'.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   triggerRadius = 180
     *                     -> radius in which players are pulled (game units)
     *   strenght      = 30
     *                     -> pull strength applied per tick
     *   grenadeLimit  = 3
     *                     -> how many decoys the hero gets
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
    public class MagneticDecoy : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.MagneticDecoy;
        private static MagneticDecoyOptions Options => SkillConfigurationResolver.Get<MagneticDecoyOptions>(BuiltInSkillIds.MagneticDecoy);
        private static readonly ConcurrentDictionary<Vector, byte> decoys = [];
        private readonly static ConcurrentDictionary<uint, int> playersWithSkill = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            decoys.Clear();
        }

        public static void DecoyStarted(EventDecoyStarted @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            decoys.TryAdd(new Vector(@event.X, @event.Y, @event.Z), 0);
        }

        public static void DecoyDetonate(EventDecoyDetonate @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            foreach (var decoy in decoys.Keys.Where(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z))
                decoys.TryRemove(decoy, out _);
        }

        public static void OnTick()
        {
            foreach (Vector decoyPos in decoys.Keys)
                foreach (var player in PlayerManager.GetTickPlayers().Where(p => p.IsValid && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist))
                {
                    var eventPlayer = PlayerManager.GetPlayerEvent(player);
                    if (eventPlayer == null || !eventPlayer.IsValid) continue;

                    var decoyRadius = Options.TriggerRadius;

                    var pawn = eventPlayer.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) continue;

                    double distance = SkillUtils.GetDistance(decoyPos, pawn.AbsOrigin);
                    if (distance <= decoyRadius && distance > 10)
                    {
                        Vector direction = new(decoyPos.X - pawn.AbsOrigin.X, decoyPos.Y - pawn.AbsOrigin.Y, 0);
                        float length = direction.Length();

                        Vector normalized = direction / length;
                        float ratio = 1 - (float)(distance / decoyRadius);
                        float strenght = Options.Strenght * ratio;

                        pawn.AbsVelocity.X += normalized.X * strenght;
                        pawn.AbsVelocity.Y += normalized.Y * strenght;
                    }
                }
        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Weapon;
            if (weapon != "decoy") return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
            {
                playersWithSkill[player.Index] = grenadesLeft - 1;
                player!.GiveNamedItem($"weapon_{weapon}");
                SkillUtils.UpdateGrenadeCount(player, CsItem.DecoyGrenade, grenadesLeft - 1);
            }
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Item;
            if (player == null || !player.IsValid) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.DecoyGrenade, grenadesLeft);
        }

        public static void WeaponPickup(EventItemPickup @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Item;
            if (string.IsNullOrEmpty(weapon) || weapon != "decoy") return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.DecoyGrenade, grenadesLeft);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            int grenadeLimit = Options.GrenadeLimit;
            playersWithSkill.TryAdd(player.Index, grenadeLimit);

            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
            SkillUtils.UpdateGrenadeCount(player, CsItem.DecoyGrenade, grenadeLimit);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            playersWithSkill.TryRemove(player.Index, out _);
            SkillUtils.UpdateGrenadeCount(player, CsItem.DecoyGrenade, 1);
        }
    }
}