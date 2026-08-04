using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using System.Collections.Concurrent;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Rubber - Damage taken slows you down briefly instead of hurting as much.
     *
     * LOGIC
     *   PlayerHurt: starts the slow effect on you.
     *   OnTick: applies slownessModifier for slownessTime seconds, then restores
     *     speed.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   slownessTime     = 2f
     *                        -> how long (seconds) the slow lasts
     *   slownessModifier = .2f
     *                        -> speed multiplier while slowed (0.2 = 20% speed)
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
    public class Rubber : ISkill
    {
        private const Skills skillName = Skills.Rubber;
        private static RubberOptions Options => SkillConfigurationResolver.Get<RubberOptions>(BuiltInSkillIds.Rubber);
        private static readonly ConcurrentDictionary<uint, float> playersToSlow = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            playersToSlow.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;
            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);

            var victimPawn = victim!.PlayerPawn.Value;
            if (victimPawn == null || !victimPawn.IsValid) return;

            var rubberTime = Options.SlownessTime;
            if (attackerInfo?.Skill == skillName)
                playersToSlow.AddOrUpdate(victim.Index, Server.TickCount + (64 * rubberTime), (k, v) => Server.TickCount + (64 * rubberTime));
        }

        public static void OnTick()
        {
            foreach (var item in playersToSlow)
            {
                var playerIndex = item.Key;
                var time = item.Value;

                var player = Utilities.GetPlayerFromIndex((int)playerIndex);
                if (player == null || !player.IsValid) continue;

                if (time >= Server.TickCount)
                    ChangeVelocity(player);
                else
                    playersToSlow.TryRemove(item.Key, out _);
            }
        }

        private static void ChangeVelocity(CCSPlayerController player)
        {
            if (player.PlayerPawn == null) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            pawn.VelocityModifier = Options.SlownessModifier;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#8B4513", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, float slownessTime = 2f, float slownessModifier = .2f) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float SlownessTime { get; set; } = slownessTime;
            public float SlownessModifier { get; set; } = slownessModifier;
        }
    }
}