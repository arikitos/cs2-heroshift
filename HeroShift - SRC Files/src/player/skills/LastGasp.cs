using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;
using static System.Net.Mime.MediaTypeNames;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * LastGasp - On death you deal a final burst of damage to your killer.
     *
     * LOGIC
     *   PlayerDeath: applies damageAfterDeath to the killer.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   damageAfterDeath = 30
     *                        -> damage dealt to the killer as you die
     *   canKill          = true
     *                        -> true = this damage can finish the killer off,
     *                           false = leaves them at 1 HP
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
     *   rarity       = Rarity.Rare
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class LastGasp : ISkill
    {
        private const Skills skillName = Skills.LastGasp;

        private static LastGaspOptions Options => SkillConfigurationResolver.Get<LastGaspOptions>(BuiltInSkillIds.LastGasp);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);

            if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid)
                return;

            var victimPawn = victim.PlayerPawn?.Value;
            var attackerPawn = attacker.PlayerPawn?.Value;

            if (victimPawn == null || !victimPawn.IsValid || attackerPawn == null || !attackerPawn.IsValid)
                return;

            var victimInfo = PlayerManager.GetPlayerByIndex(victim!.Index);
            if (victimInfo?.Skill != skillName)
                return;

            int damageAfterDeath = Options.DamageAfterDeath;
            bool canKill = Options.CanKill;

            if (!canKill)
            {
                int newHealth = (int)(attackerPawn.Health - damageAfterDeath);
                if (newHealth <= 0)
                    damageAfterDeath = damageAfterDeath - 1 + newHealth;

            }

            SkillUtils.TakeHealth(attackerPawn, damageAfterDeath, victim, KillfeedIcons.Fist);
            PlayerManager.GetPlayerFromEvent(attacker)?.ExecuteClientCommand($"play player/player_damagebody_0{HeroShift.Instance.Random.Next(4, 8)}");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#88bdba", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Rare, int damageAfterDeath = 30, bool canKill = true) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public int DamageAfterDeath { get; set; } = damageAfterDeath;
            public bool CanKill { get; set; } = canKill;

        }
    }
}