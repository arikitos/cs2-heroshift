using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Pyro - Fire heals you instead of hurting you, and you carry molotovs.
     *
     * LOGIC
     *   PlayerHurt: burn damage is converted into healing scaled by
     *     regenerationMultiplier.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   regenerationMultiplier = 1.5f
     *                              -> how much health fire gives you instead of
     *                                 damage (1.5 = 150%)
     *   grenadeLimit           = 2
     *                              -> how many molotov/incendiary grenades the
     *                                 hero gets
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
    public class Pyro : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Pyro;
        private static PyroOptions Options => SkillConfigurationResolver.Get<PyroOptions>(BuiltInSkillIds.Pyro);
        private readonly static ConcurrentDictionary<uint, int> playersWithSkill = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);
            int damage = @event.DmgHealth;
            string weapon = @event.Weapon;

            if (weapon != "inferno" || !Instance.IsPlayerValid(victim)) return;
            var victimInfo = PlayerManager.GetPlayerByIndex(victim!.Index);

            if (victimInfo == null || victimInfo.Skill != skillName) return;

            var pawn = victim!.PlayerPawn.Value;
            SkillUtils.AddHealth(pawn, (int)(damage * Options.RegenerationMultiplier));
        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Weapon;
            if (weapon != "molotov" && weapon != "incgrenade") return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
            {
                playersWithSkill[player.Index] = grenadesLeft - 1;
                player!.GiveNamedItem($"weapon_{weapon}");
                SkillUtils.UpdateGrenadeCount(player, CsItem.Molotov, grenadesLeft - 1);
                SkillUtils.UpdateGrenadeCount(player, CsItem.IncendiaryGrenade, grenadesLeft - 1);
            }
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Item;
            if (player == null || !player.IsValid) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
            {
                SkillUtils.UpdateGrenadeCount(player, CsItem.Molotov, grenadesLeft);
                SkillUtils.UpdateGrenadeCount(player, CsItem.IncendiaryGrenade, grenadesLeft);
            }
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

            var item = player.Team == CsTeam.CounterTerrorist ? CsItem.IncendiaryGrenade : CsItem.Molotov;

            SkillUtils.TryGiveWeapon(player, item);
            SkillUtils.UpdateGrenadeCount(player, item, grenadeLimit);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            playersWithSkill.TryRemove(player.Index, out _);
            SkillUtils.UpdateGrenadeCount(player, CsItem.Molotov, 1);
            SkillUtils.UpdateGrenadeCount(player, CsItem.IncendiaryGrenade, 1);
        }
    }
}