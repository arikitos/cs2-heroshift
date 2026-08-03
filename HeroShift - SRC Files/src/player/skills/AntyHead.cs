using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

namespace src.player.skills
{
    /*
     * AntyHead - Headshots against you do not count as headshots.
     *
     * LOGIC
     *   PlayerHurtPre: returns true to block plugin handling when the hitgroup is
     *     the head.
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
    public class AntyHead : ISkill
    {
        private const Skills skillName = Skills.AntyHead;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static bool PlayerHurtPre(EventPlayerHurt @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);

            if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid || attacker == victim) return false;
            if (@event.Hitgroup != (int)HitGroup_t.HITGROUP_HEAD) return false;
            if (PlayerManager.GetPlayerByIndex(victim.Index)?.Skill != skillName) return false;

            SkillUtils.RestoreHealth(victim);
            return true;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#8B4513", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
        }
    }
}