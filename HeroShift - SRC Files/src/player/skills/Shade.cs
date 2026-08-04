using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Collections.Concurrent;
using src.utils;
using static src.utils.RarityManager;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Shade - A chance that damage taken teleports you a short distance away.
     *
     * LOGIC
     *   EnableSkill: rolls the trigger chance between chanceFrom and chanceTo.
     *   PlayerHurt: on a successful roll, blinks you teleportDistance units away.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   teleportDistance = 100f
     *                        -> how far the blink moves you (game units)
     *   chanceFrom       = .3f
     *                        -> lowest trigger chance that can be rolled (0.3 =
     *                           30%)
     *   chanceTo         = .45f
     *                        -> highest trigger chance that can be rolled
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
    public class Shade : ISkill
    {
        private const Skills skillName = Skills.Shade;
        private static ShadeOptions Options => SkillConfigurationResolver.Get<ShadeOptions>(BuiltInSkillIds.Shade);
        private static readonly ConcurrentDictionary<uint, float> noSpace = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void NewRound()
        {
            noSpace.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim)) return;

            if (attacker!.Index == victim!.Index) return;

            var victimInfo = PlayerManager.GetPlayerByIndex(victim!.Index);
            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);

            if (attackerInfo?.Skill == skillName)
                if (Instance.Random.NextDouble() <= attackerInfo.SkillChance)
                    TeleportAttackerBehindVictim(attacker!, victim!);
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

        public static void DisableSkill(CCSPlayerController player)
        {
            noSpace.TryRemove(player.Index, out _);
            SkillUtils.ResetPrintHTML(player);
        }

        public static void OnTick()
        {
            foreach (var (playerIndex, time) in noSpace)
            {
                var player = Utilities.GetPlayerFromIndex((int)playerIndex);
                if (player == null || !player.IsValid) continue;

                if (time >= Server.TickCount)
                    UpdateHUD(player);
                else
                    SkillUtils.ResetPrintHTML(player);
            }
        }

        private static void UpdateHUD(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;
            playerInfo.PrintHTML = $"<font color='#FF0000'>{player.GetTranslation("shade_nospace")}</font>";
        }

        private unsafe static bool CheckTeleport(CCSPlayerController attacker, CCSPlayerController victim, Vector startPos, Vector endPos, QAngle angle)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            if (attackerPawn == null || !attackerPawn.IsValid) return false;

            var victimPawn = victim.PlayerPawn.Value;
            if (victimPawn == null || !victimPawn.IsValid) return false;

            var result = RayTrace.TraceHullShape(
                    startPos,
                    endPos,
                    victim,
                    attackerPawn.Collision.Mins,
                    attackerPawn.Collision.Maxs,
                    null,
                    null,
                    angle
                );

            if (!result.HasValue)
                return false;

            return !result.Value.DidHit;
        }

        private static void TeleportAttackerBehindVictim(CCSPlayerController attacker, CCSPlayerController victim)
        {
            var victimPawn = victim.PlayerPawn.Value;
            var attackerPawn = attacker.PlayerPawn.Value;

            if (victimPawn == null || attackerPawn == null || victimPawn.AbsOrigin == null || victimPawn.AbsRotation == null) return;

            Vector victimPos = new(victimPawn.AbsOrigin.X, victimPawn.AbsOrigin.Y, victimPawn.AbsOrigin.Z);
            QAngle victimAngles = new(victimPawn.AbsRotation.X, victimPawn.AbsRotation.Y, victimPawn.AbsRotation.Z);
            float distance = Options.TeleportDistance;

            int[] angles = [0, 90, -90];
            bool teleported = false;

            foreach (int extraAngle in angles)
            {
                QAngle targetAngle = new(0, victimAngles.Y + extraAngle, 0);
                Vector direction = SkillUtils.GetForwardVector(targetAngle);
                Vector targetPos = victimPos - (direction * distance);

                // Trace can only ignore one entity (victim); start a step out so the attacker's
                // own body in melee range doesn't clip the hull and report a false "no space".
                Vector traceStart = victimPos - (direction * 20f);

                if (CheckTeleport(attacker, victim, traceStart, targetPos, targetAngle))
                {
                    attackerPawn.Teleport(targetPos, targetAngle, Vector.Zero);
                    teleported = true;
                    break;
                }
            }

            if (!teleported)
                noSpace.AddOrUpdate(attacker.Index, Server.TickCount + (64 * 2), (_, _) => Server.TickCount + (64 * 2));
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#4d4d4d", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, float teleportDistance = 100f, float chanceFrom = .3f, float chanceTo = .45f) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float TeleportDistance { get; set; } = teleportDistance;
            public float ChanceFrom { get; set; } = chanceFrom;
            public float ChanceTo { get; set; } = chanceTo;
        }
    }
}
