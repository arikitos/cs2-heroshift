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
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
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
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
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

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#6effc7", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Uncommon, float maxDistance = 4096f, bool friendlyFire = false) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float MaxDistance { get; set; } = maxDistance;
            public bool FriendlyFire { get; set; } = friendlyFire;
        }
    }
}