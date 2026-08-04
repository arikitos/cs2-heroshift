using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;
using src.utils;
using System.Collections.Concurrent;

namespace src.player.skills
{
    /*
     * AntyFlash - You are immune to long flashes and get extra flashbangs.
     *
     * LOGIC
     *   PlayerBlind: if your blind time exceeds flashDuration it gets
     *     clamped/removed.
     *   GrenadeThrown/WeaponEquip/WeaponPickup: keeps the extra flashbang count
     *     in the HUD.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   flashDuration = 7f
     *                     -> max blind seconds you can suffer; above this you are
     *                        protected
     *   grenadeLimit  = 2
     *                     -> how many flashbangs the hero gets
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
    public class AntyFlash : ISkill
    {
        private const Skills skillName = Skills.AntyFlash;
        private static AntyFlashOptions Options => SkillConfigurationResolver.Get<AntyFlashOptions>(BuiltInSkillIds.AntyFlash);
        private readonly static ConcurrentDictionary<uint, int> playersWithSkill = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void PlayerBlind(EventPlayerBlind @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);

            if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);

            if (playerInfo?.Skill == skillName)
            {
                playerPawn.FlashDuration = 0.0f;
            }
            else if (attacker != null && attacker.IsValid)
            {
                var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
                if (attackerInfo?.Skill == skillName)
                    playerPawn.FlashDuration = Options.FlashDuration;
            }
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
            int grenadeLimit = Options.GrenadeLimit;

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

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#D6E6FF", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, float flashDuration = 7f, int grenadeLimit = 2) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float FlashDuration { get; set; } = flashDuration;
            public int GrenadeLimit { get; set; } = grenadeLimit;
        }
    }
}