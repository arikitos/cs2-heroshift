using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * AntyHead - Headshots against you do not count as headshots.
     *
     * LOGIC
     *   PlayerHurtPre: returns true to block plugin handling when the hitgroup is
     *     the head.
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
    public class AntyHead : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.AntyHead;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
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
    }
}