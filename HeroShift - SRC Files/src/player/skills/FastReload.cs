using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * FastReload - Instantly reloads your weapon.
     *
     * LOGIC
     *   UseSkill: refills the magazine without the reload animation.
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
    public class FastReload : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.FastReload;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;
            if (!player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            InstaReload(playerPawn);
        }

        private static void InstaReload(CCSPlayerPawn pawn)
        {
            if (pawn == null || !pawn.IsValid) return;
            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null || weaponServices.ActiveWeapon == null || !weaponServices.ActiveWeapon.IsValid) return;

            var activeWeapon = weaponServices.ActiveWeapon.Value;
            if (activeWeapon == null || !activeWeapon.IsValid || activeWeapon.VData == null) return;

            activeWeapon.Clip1 = activeWeapon.VData.MaxClip1;
            Utilities.SetStateChanged(activeWeapon, "CBasePlayerWeapon", "m_iClip1");
        }
    }
}