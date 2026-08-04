using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * Grenadier - You get a full set of grenades.
     *
     * LOGIC
     *   EnableSkill: gives the grenade loadout.
     *   GrenadeThrown: refills thrown grenades.
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
    public class Grenadier : ISkill
    {
        private const Skills skillName = Skills.Grenadier;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (!Instance.IsPlayerValid(player)) return;
            SkillUtils.TryGiveWeapon(player, CsItem.HEGrenade);
        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Weapon;
            if (weapon != "hegrenade" || !Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill == skillName)
                player!.GiveNamedItem($"weapon_{weapon}");
        }
    }
}