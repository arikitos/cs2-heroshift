using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * ChillOut - The planted bomb needs extra time before it becomes armed.
     *
     * LOGIC
     *   BombBeginplant/BombPlanted: records the plant and delays the armed state.
     *   OnTick: counts down bombArmedTime before the C4 actually starts ticking.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   bombArmedTime = 10f
     *                     -> extra seconds before the planted C4 becomes armed
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.CounterTerrorist
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = 1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class ChillOut : ISkill
    {
        private const Skills skillName = Skills.ChillOut;
        private static ChillOutOptions Options => SkillConfigurationResolver.Get<ChillOutOptions>(BuiltInSkillIds.ChillOut);
        private static readonly ConcurrentDictionary<uint, float> plantingPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            plantingPlayers.Clear();
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            plantingPlayers.TryRemove(player.Index, out _);
            SkillUtils.ResetPrintHTML(player);
        }

        public static void BombAbortplant(EventBombAbortplant @event)
        {
            var user = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(user)) return;

            plantingPlayers.TryRemove(user.Index, out _);
            SkillUtils.ResetPrintHTML(user);
        }

        public static void BombBeginplant(EventBombBeginplant @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            plantingPlayers.TryAdd(player!.Index, Server.CurrentTime);

            if (!IsAnyOwnerAlive()) return;

            var bomb = PlayerManager.GetTickBomb();
            if (bomb == null || !bomb.IsValid) return;

            bomb.ArmedTime = Server.CurrentTime + Options.BombArmedTime;
        }

        private static bool IsAnyOwnerAlive()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player == null || !player.IsValid || !player.PawnIsAlive) continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index);
                if (playerInfo?.Skill == skillName) return true;
            }

            return false;
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            plantingPlayers.TryRemove(player!.Index, out _);
            SkillUtils.ResetPrintHTML(player);
        }

        public static void OnTick()
        {
            if (plantingPlayers.IsEmpty) return;

            float currentTime = Server.CurrentTime;
            float extraTime = Options.BombArmedTime;

            foreach (var player in PlayerManager.GetTickPlayers().Where(p => p.Team == CsTeam.Terrorist))
            {
                if (player == null || !player.IsValid) continue;

                var playerInfo = PlayerManager.GetPlayerByIndex((PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index));
                if (playerInfo == null) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;

                if (pawn.WeaponServices == null) continue;
                var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;
                if (activeWeapon == null || !activeWeapon.IsValid || activeWeapon.DesignerName != "weapon_c4") continue;

                if (plantingPlayers.TryGetValue(player.Index, out float plantTime))
                {
                    float remaining = plantTime + extraTime - currentTime;
                    playerInfo.PrintHTML = $"{player.GetTranslation("planter_planting", $"<font color='#00FF00'>{Math.Max(0, remaining):0.0}s</font>")}";
                }
            }
        }
    }
}