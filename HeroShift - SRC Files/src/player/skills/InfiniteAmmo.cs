using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * InfiniteAmmo - You never run out of ammo or grenades.
     *
     * LOGIC
     *   WeaponFire/WeaponReload: refills the clip and reserve after every shot.
     *   GrenadeThrown: gives the thrown grenade straight back.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
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
    public class InfiniteAmmo : ISkill
    {
        private const Skills skillName = Skills.InfiniteAmmo;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill == skillName)
                ApplyInfiniteAmmo(player!);

        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill == skillName)
                player!.GiveNamedItem($"weapon_{@event.Weapon}");

        }

        public static void WeaponReload(EventWeaponReload @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill == skillName)
                ApplyInfiniteAmmo(player!);
        }

        private static void ApplyInfiniteAmmo(CCSPlayerController player)
        {
            var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
            if (activeWeaponHandle?.Value != null)
                activeWeaponHandle.Value.Clip1 = 100;
        }
    }
}