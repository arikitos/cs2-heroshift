using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Disarmament - A chance that damage taken makes the attacker drop their
     * weapon.
     *
     * LOGIC
     *   EnableSkill: rolls the trigger chance between chanceFrom and chanceTo.
     *   PlayerHurt: on a successful roll, forces the attacker to drop the active
     *     weapon.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   chanceFrom = .2f
     *                  -> lowest trigger chance that can be rolled (0.2 = 20%)
     *   chanceTo   = .35f
     *                  -> highest trigger chance that can be rolled
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
    public class Disarmament : ISkill
    {
        private const Skills skillName = Skills.Disarmament;

        private static DisarmamentOptions Options => SkillConfigurationResolver.Get<DisarmamentOptions>(BuiltInSkillIds.Disarmament);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;
            var playerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
            {
                if (Instance.Random.NextDouble() <= playerInfo?.SkillChance)
                {
                    var weaponServices = victim.PlayerPawn?.Value?.WeaponServices;
                    if (weaponServices?.ActiveWeapon == null) return;

                    var weaponName = weaponServices?.ActiveWeapon?.Value?.DesignerName;
                    if (weaponName != null && !weaponName.Contains("weapon_knife") && !weaponName.Contains("weapon_bayonet") && !weaponName.Contains("weapon_c4"))
                        victim.DropActiveWeapon();
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;
            float newChance = (float)Instance.Random.NextDouble() * (Options.ChanceTo - Options.ChanceFrom) + Options.ChanceFrom;
            playerInfo.SkillChance = newChance;
            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, newChance)}",
                border: !PlayerManager.GetTickPlayers().Any(p => p.Team == player.Team && p != player) ? "tb" : "t");
        }
    }
}