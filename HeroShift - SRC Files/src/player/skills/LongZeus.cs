using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * LongZeus - Your zeus (taser) reaches across the whole map.
     *
     * LOGIC
     *   EnableSkill: gives the zeus and raises its range to maxDistance.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   maxDistance  = 4096f
     *                    -> taser reach in game units (4096 = practically
     *                       map-wide)
     *   friendlyFire = false
     *                    -> true = can also hit teammates
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
     *   rarity       = Rarity.Uncommon
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class LongZeus : ISkill
    {
        private const Skills skillName = Skills.LongZeus;

        private static LongZeusOptions Options => SkillConfigurationResolver.Get<LongZeusOptions>(BuiltInSkillIds.LongZeus);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public unsafe static void WeaponFire(EventWeaponFire @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.WeaponServices == null) return;

            var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;
            if (activeWeapon == null || !activeWeapon.IsValid || activeWeapon.DesignerName != "weapon_taser") return;

            var result = RayTrace.EyeTrace(player);
            if (result == null || !result.HasValue)
                return;

            if (!result.Value.HitPlayer(out CCSPlayerController? target) || target == null)
                return;

            if (target.Handle == player.Handle) return;
            if (!Options.FriendlyFire && player.Team == target.Team) return;

            SkillUtils.TakeHealth(target.PlayerPawn.Value, 9999, player, KillfeedIcons.Taser);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Zeus);
        }
    }
}