using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

namespace src.player.skills
{
    /*
     * NoNades - Grenades cannot hurt you.
     *
     * LOGIC
     *   PlayerHurtPre: returns true to block handling when the damage came from a
     *     grenade.
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
    public class NoNades : ISkill
    {
        private const Skills skillName = Skills.NoNades;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        private static readonly HashSet<string> blockedWeapons =
            ["hegrenade", "inferno", "decoy", "flashbang", "smokegrenade", "molotov"];

        public static bool PlayerHurtPre(EventPlayerHurt @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return false;

            if (!blockedWeapons.Contains(@event.Weapon)) return false;
            if (PlayerManager.GetPlayerByIndex(player!.Index)?.Skill != skillName) return false;

            SkillUtils.RestoreHealth(player);
            return true;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#a38c1a", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
        }
    }
}