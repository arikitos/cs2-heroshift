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
     * Push - A chance that damage taken pushes the attacker away.
     *
     * LOGIC
     *   EnableSkill: rolls the trigger chance between chanceFrom and chanceTo.
     *   PlayerHurt: on a successful roll, applies the push velocities to the
     *     attacker.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   chanceFrom   = .3f
     *                    -> lowest trigger chance that can be rolled (0.3 = 30%)
     *   chanceTo     = .4f
     *                    -> highest trigger chance that can be rolled
     *   jumpVelocity = 300f
     *                    -> upward velocity applied to the attacker
     *   pushVelocity = 400f
     *                    -> horizontal velocity applied to the attacker
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
    public class Push : ISkill
    {
        private const Skills skillName = Skills.Push;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"), false);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim)
                return;

            var playerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
            {
                if (Instance.Random.NextDouble() <= playerInfo.SkillChance)
                    PushEnemy(victim, attacker!.PlayerPawn.Value!.EyeAngles);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;
            float newChance = (float)Instance.Random.NextDouble() * (SkillsInfo.GetValue<float>(skillName, "ChanceTo") - SkillsInfo.GetValue<float>(skillName, "ChanceFrom")) + SkillsInfo.GetValue<float>(skillName, "ChanceFrom");
            playerInfo.SkillChance = newChance;
            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, newChance)}",
                border: !PlayerManager.GetTickPlayers().Any(p => p.Team == player.Team && p != player) ? "tb" : "t");
        }

        private static void PushEnemy(CCSPlayerController player, QAngle attackerAngle)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.LifeState != (int)LifeState_t.LIFE_ALIVE)
                return;

            var currentPosition = playerPawn.AbsOrigin;

            Vector newVelocity = SkillUtils.GetForwardVector(attackerAngle) * SkillsInfo.GetValue<float>(skillName, "pushVelocity");
            newVelocity.Z = playerPawn.AbsVelocity.Z + SkillsInfo.GetValue<float>(skillName, "jumpVelocity");

            playerPawn.Teleport(currentPosition, null, newVelocity);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#1e9ab0", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, float chanceFrom = .3f, float chanceTo = .4f, float jumpVelocity = 300f, float pushVelocity = 400f) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float ChanceFrom { get; set; } = chanceFrom;
            public float ChanceTo { get; set; } = chanceTo;
            public float JumpVelocity { get; set; } = jumpVelocity;
            public float PushVelocity { get; set; } = pushVelocity;
        }
    }
}