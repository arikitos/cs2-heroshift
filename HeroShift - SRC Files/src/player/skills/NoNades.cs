using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * NoNades - Grenades cannot hurt you.
     *
     * LOGIC
     *   PlayerHurtPre: returns true to block handling when the damage came from a
     *     grenade.
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
    public class NoNades : ISkill
    {
        private const Skills skillName = Skills.NoNades;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
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
    }
}