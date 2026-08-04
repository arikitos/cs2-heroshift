using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using System.Collections.Concurrent;
using src.utils;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * ReturnToSender - Damage dealt to you is sent back to the attacker.
     *
     * LOGIC
     *   PlayerHurt: mirrors the damage you received onto whoever caused it.
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
    public class ReturnToSender : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.ReturnToSender;
        private static readonly ConcurrentDictionary<nint, byte> playersToSender = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            playersToSender.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);
            int damage = @event.DmgHealth;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;
            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
            if (attackerInfo == null || attackerInfo.Skill != skillName) return;

            if (playersToSender.ContainsKey(victim!.Handle))
                return;

            var spawnpoint = SkillUtils.GetSpawnPointVector(victim);
            if (spawnpoint == null) return;

            victim!.PlayerPawn!.Value!.Teleport(spawnpoint);
            playersToSender.TryAdd(victim.Handle, 0);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            playersToSender.TryRemove(player.Handle, out _);
        }
    }
}