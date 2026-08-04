using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * Zeus - You get a zeus that recharges instantly instead of being one-shot.
     *
     * LOGIC
     *   EnableSkill: gives the zeus (taser).
     *   WeaponFire: 0.1s after firing, resets LastAttackTick/FireTime so it can
     *     fire again.
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
    public class Zeus : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Zeus;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Zeus);
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);

            if (playerInfo?.Skill == skillName)
            {
                var pawn = player!.PlayerPawn!.Value!;
                if (pawn.WeaponServices == null || pawn.WeaponServices.ActiveWeapon == null || !pawn.WeaponServices.ActiveWeapon.IsValid) return;
                if (pawn.WeaponServices.ActiveWeapon.Value == null || !pawn.WeaponServices.ActiveWeapon.Value.IsValid) return;

                var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;
                if (activeWeapon.DesignerName != "weapon_taser") return;
                var taser = activeWeapon.As<CWeaponTaser>();
                Instance.AddTimer(.1f, () =>
                {
                    if (taser.IsValid)
                    {
                        taser.LastAttackTick = 0;
                        taser.FireTime = 0;
                    }
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }

        }
    }
}