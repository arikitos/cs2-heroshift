using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Thorns - Attackers take back part of the damage they deal to you.
     *
     * LOGIC
     *   PlayerHurt: fires after you were damaged. The reflected amount is
     *     DmgHealth * healthTakenScale, then capped at maxTakenDamagePerShot, and
     *     applied to the attacker with the armor kill-feed icon. Self-damage is
     *     ignored (attacker index == victim index).
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   healthTakenScale      = .3f
     *                             -> share of the damage reflected back (0.3 =
     *                                30%)
     *   maxTakenDamagePerShot = 37
     *                             -> hard cap on reflected damage per single hit
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
    public class Thorns : ISkill
    {
        private const Skills skillName = Skills.Thorns;

        private static ThornsOptions Options => SkillConfigurationResolver.Get<ThornsOptions>(BuiltInSkillIds.Thorns);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            if (victim == null || !victim.IsValid) return;

            var attacker = @event.Attacker;
            if (attacker == null || !attacker.IsValid) return;

            var attackerEvent = PlayerManager.GetPlayerEvent(attacker);
            var victimEvent = PlayerManager.GetPlayerEvent(victim);

            if (attackerEvent == null || !attackerEvent.IsValid) return;
            if (victimEvent == null || !victimEvent.IsValid) return;

            var attackerPawn = attackerEvent.PlayerPawn.Value;
            if (attackerPawn == null || !attackerPawn.IsValid || attackerPawn.Health == 0) return;

            if (attackerEvent.Index == victimEvent.Index) return;

            var victimInfo = PlayerManager.GetPlayerByIndex(victimEvent!.Index);
            if (victimInfo?.Skill == skillName)
            {
                int damage = (int)(@event.DmgHealth * Options.HealthTakenScale);
                damage = Math.Min(damage, Options.MaxTakenDamagePerShot);

                SkillUtils.TakeHealth(attackerEvent.PlayerPawn.Value, damage, victimEvent, KillfeedIcons.Armor);
                attackerEvent.EmitSound("Player.DamageBody.Onlooker");
            }
        }
    }
}