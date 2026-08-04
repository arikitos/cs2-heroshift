using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Teleporter - A chance that damage taken teleports you somewhere else.
     *
     * LOGIC
     *   EnableSkill: rolls the trigger chance between chanceFrom and chanceTo.
     *   PlayerHurt: on a successful roll, teleports you away from the attacker.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   chanceFrom = .5f
     *                  -> lowest trigger chance that can be rolled (0.5 = 50%)
     *   chanceTo   = .6f
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
    public class Teleporter : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Teleporter;

        private static TeleporterOptions Options => SkillConfigurationResolver.Get<TeleporterOptions>(BuiltInSkillIds.Teleporter);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
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

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);

            if (!Instance.IsPlayerValid(victim) || !Instance.IsPlayerValid(attacker)) return;
            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);

            if (attackerInfo?.Skill == skillName)
                if (Instance.Random.NextDouble() <= attackerInfo.SkillChance)
                    TeleportPlayers(attacker!, victim!);
        }

        private static void TeleportPlayers(CCSPlayerController attacker, CCSPlayerController victim)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            var victimPawn = victim.PlayerPawn.Value;
            if (attackerPawn == null || !attackerPawn.IsValid || victimPawn == null || !victimPawn.IsValid) return;
            if (attackerPawn.AbsOrigin == null || victimPawn.AbsOrigin == null || attackerPawn.AbsRotation == null || victimPawn.AbsRotation == null) return;

            Vector attackerPosition = new(attackerPawn.AbsOrigin.X, attackerPawn.AbsOrigin.Y, attackerPawn.AbsOrigin.Z);
            QAngle attackerAngles = new(attackerPawn.V_angle.X, attackerPawn.V_angle.Y, 0);
            Vector attackerVelocity = new(attackerPawn.AbsVelocity.X, attackerPawn.AbsVelocity.Y, attackerPawn.AbsVelocity.Z);

            Vector victimPosition = new(victimPawn.AbsOrigin.X, victimPawn.AbsOrigin.Y, victimPawn.AbsOrigin.Z);
            QAngle victimAngles = new(victimPawn.V_angle.X, victimPawn.V_angle.Y, 0);
            Vector victimVelocity = new(victimPawn.AbsVelocity.X, victimPawn.AbsVelocity.Y, victimPawn.AbsVelocity.Z);

            victimPawn.Teleport(attackerPosition, null, attackerVelocity);
            attackerPawn.Teleport(victimPosition, null, victimVelocity);

            victimPawn.Look(attackerAngles);
            attackerPawn.Look(victimAngles);
        }
    }
}
