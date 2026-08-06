using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * QuickShot - Your weapon fires faster than normal.
     *
     * LOGIC
     *   OnTick: shortens the next-attack delay on your active weapon.
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
    public class QuickShot : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.QuickShot;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void OnTick()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player == null) continue;
                if (PlayerManager.GetPlayerByIndex(player.Index)?.Skill != skillName) continue;
                if (!Instance.IsPlayerValid(player)) continue;

                var pawn = player.PlayerPawn.Value!;
                var weaponServices = pawn.WeaponServices;
                if (weaponServices == null || weaponServices.ActiveWeapon == null || !weaponServices.ActiveWeapon.IsValid) continue;

                var weapon = weaponServices.ActiveWeapon.Value;
                if (weapon == null || !weapon.IsValid || pawn.CameraServices == null) continue;

                if (pawn.AimPunchServices != null)
                {
                    pawn.AimPunchServices.PredictableBaseTick = 0;
                    pawn.AimPunchServices.PredictableBaseTickInterpAmount = 0;
                    pawn.AimPunchServices.UnpredictableBaseTick = 0;
                }

                pawn.CameraServices.CsViewPunchAngleTick = 0;
                pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;

                Schema.SetSchemaValue<Int32>(weapon.Handle, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick", Server.TickCount);
                Schema.SetSchemaValue<Int32>(weapon.Handle, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick", Server.TickCount);
            }
        }
    }
}