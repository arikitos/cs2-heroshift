using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;
using src.utils;
using static src.HeroShift;

namespace src.player.skills
{
    /*
     * RobinHood - Damage you deal is converted into money.
     *
     * LOGIC
     *   PlayerHurt: pays you moneyMultiplier per point of damage dealt.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   moneyMultiplier = 35
     *                       -> money earned per damage point dealt
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
    public class RobinHood : ISkill
    {
        private const Skills skillName = Skills.RobinHood;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var damage = @event.DmgHealth;
            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;

            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
            if (attackerInfo?.Skill != skillName) return;

            int moneyToSteal = damage * SkillConfigurationResolver.Get<RobinHoodOptions>(BuiltInSkillIds.RobinHood).MoneyMultiplier;
            StealMoney(victim!, attacker!, moneyToSteal);
        }

        private static void StealMoney(CCSPlayerController victim, CCSPlayerController attacker, int money)
        {
            var victimMoneyServices = victim?.InGameMoneyServices;
            var attackerMoneyServices = attacker?.InGameMoneyServices;
            if (victimMoneyServices == null || attackerMoneyServices == null) return;

            var moneyToAdd = victimMoneyServices.Account < money ? victimMoneyServices.Account : money;
            victimMoneyServices.Account = Math.Max(victimMoneyServices.Account - money, 0);
            Utilities.SetStateChanged(victim!, "CCSPlayerController", "m_pInGameMoneyServices");

            attackerMoneyServices.Account = Math.Min(attackerMoneyServices.Account + moneyToAdd, 16000);
            Utilities.SetStateChanged(attacker!, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#119125", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, int moneyMultiplier = 35) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public int MoneyMultiplier { get; set; } = moneyMultiplier;
        }
    }
}