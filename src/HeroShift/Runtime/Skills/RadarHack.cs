using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * RadarHack - All enemies are permanently shown on your radar.
     *
     * LOGIC
     *   OnTick: marks every enemy as spotted for you.
     */
    public class RadarHack : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.RadarHack;
        private static readonly SkillId[] hidingSkills = [BuiltInSkillIds.Ghost, BuiltInSkillIds.Ninja, BuiltInSkillIds.C4Camouflage];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void OnTick()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                var playerEvent = PlayerManager.GetPlayerEvent(player);
                if (playerEvent == null) continue;
                if (PlayerManager.GetPlayerByIndex(playerEvent.Index)?.Skill != skillName) continue;
                if (!Instance.IsPlayerValid(playerEvent)) continue;

                SetEnemiesVisibleOnRadar(player);
            }
        }

        private static void SetEnemiesVisibleOnRadar(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null) return;

            // SpottedByMask is indexed by player slot (0-63), not entity index.
            int slot = player.Slot;

            foreach (var enemy in PlayerManager.GetTickPlayers())
            {
                if (enemy == null || enemy.Team == player.Team) continue;

                var enemyEvent = PlayerManager.GetPlayerEvent(enemy);
                if (enemyEvent == null || !enemyEvent.IsValid) continue;

                var enemyPawn = enemyEvent.PlayerPawn.Value;
                if (enemyPawn == null || !enemyPawn.IsValid) continue;

                var enemyInfo = PlayerManager.GetPlayerByIndex(enemyEvent.Index);
                if (enemyInfo != null && Array.IndexOf(hidingSkills, enemyInfo.Skill) >= 0 && enemyPawn.Render.A < 200)
                    continue;

                // Only the observer's slot bit — the Spotted bool would reveal to the whole team.
                enemyPawn.EntitySpottedState.SpottedByMask[0] |= (1u << (slot % 32));
            }

            var bomb = PlayerManager.GetTickBomb();
            if (bomb != null && bomb.IsValid)
                bomb.EntitySpottedState.SpottedByMask[0] |= (1u << (slot % 32));
        }
    }
}