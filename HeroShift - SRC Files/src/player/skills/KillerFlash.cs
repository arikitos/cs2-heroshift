using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

namespace src.player.skills
{
    /*
     * KillerFlash - Any enemy you blind for long enough dies instantly.
     *
     * LOGIC
     *   PlayerBlind: fires when someone is flashed. If YOU are the attacker and
     *     the victim's pawn FlashDuration is >= flashDuration, the victim is
     *     killed (TakeHealth 9999) with the flashbang kill-feed icon. Victims who
     *     have the AntyFlash skill are immune.
     *   GrenadeThrown/WeaponEquip/WeaponPickup: keeps the extra flashbang count
     *     in sync in the HUD when grenadeLimit is above the server limit.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   flashDuration = 1f
     *                     -> blind seconds required to trigger the instant kill
     *                        (lower = deadlier)
     *   friendlyFire  = true
     *                     -> true = also kills blinded teammates, false = enemies
     *                        only
     *   grenadeLimit  = 1
     *                     -> how many flashbangs the hero gets (uses
     *                        ammo_grenade_limit_flashbang as base)
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.None
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = 1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Epic
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class KillerFlash : ISkill
    {
        private const Skills skillName = Skills.KillerFlash;
        private readonly static ConcurrentDictionary<uint, int> playersWithSkill = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static void PlayerBlind(EventPlayerBlind @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            if (!Instance.IsPlayerValid(player) || !Instance.IsPlayerValid(attacker)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);

            if (!SkillsInfo.GetValue<bool>(skillName, "friendlyFire") && player!.Team == attacker!.Team) return;

            if (attackerInfo?.Skill == skillName && playerInfo?.Skill != Skills.AntyFlash && player!.PlayerPawn.Value!.FlashDuration >= SkillsInfo.GetValue<float>(skillName, "flashDuration"))
                SkillUtils.TakeHealth(player.PlayerPawn.Value, 9999, attacker, KillfeedIcons.Flashbang);
        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Weapon;
            if (weapon != "flashbang") return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
            {
                playersWithSkill[player.Index] = grenadesLeft - 1;
                player!.GiveNamedItem($"weapon_{weapon}");
                SkillUtils.UpdateGrenadeCount(player, CsItem.FlashbangGrenade, grenadesLeft - 1);
            }
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Item;
            if (player == null || !player.IsValid) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.FlashbangGrenade, grenadesLeft);
        }

        public static void WeaponPickup(EventItemPickup @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Item;
            if (string.IsNullOrEmpty(weapon) || weapon != "flashbang") return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.FlashbangGrenade, grenadesLeft);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            int flashbangLimit = ConVar.Find("ammo_grenade_limit_flashbang")?.GetPrimitiveValue<int>() ?? 2;
            int grenadeLimit = SkillsInfo.GetValue<int>(skillName, "grenadeLimit");

            if (grenadeLimit > flashbangLimit)
            {
                playersWithSkill.TryAdd(player.Index, grenadeLimit);
                SkillUtils.TryGiveWeapon(player, CsItem.FlashbangGrenade);
                SkillUtils.UpdateGrenadeCount(player, CsItem.FlashbangGrenade, grenadeLimit);
            }
            else
                SkillUtils.TryGiveWeapon(player, CsItem.FlashbangGrenade, grenadeLimit, false);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            playersWithSkill.TryRemove(player.Index, out _);
            SkillUtils.UpdateGrenadeCount(player, CsItem.FlashbangGrenade, 1);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#57bcff", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = 1, Rarity rarity = Rarity.Epic, float flashDuration = 1f, bool friendlyFire = true, int grenadeLimit = 1) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float FlashDuration { get; set; } = flashDuration;
            public bool FriendlyFire { get; set; } = friendlyFire;
            public int GrenadeLimit { get; set; } = grenadeLimit;
        }
    }
}