using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

namespace src.player.skills
{
    /*
     * QuickShot - Your weapon fires faster than normal.
     *
     * LOGIC
     *   OnTick: shortens the next-attack delay on your active weapon.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
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
        private const Skills skillName = Skills.QuickShot;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static void OnTick()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;
                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);

                if (playerInfo?.Skill == skillName)
                {
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

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#8a42f5", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
        }
    }
}