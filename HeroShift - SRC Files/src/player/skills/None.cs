using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * None - Placeholder 'no skill' entry - used when a player has no hero
     * assigned.
     *
     * LOGIC
     *   Registered so the skill list/HUD has a valid default; it has no
     *     behaviour.
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
    public class None : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.None;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }
    }
}