using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * DemonEye - Looking at an enemy slowly drains their health.
     *
     * LOGIC
     *   OnTick: every secondCooldown, traces where you are aiming and damages
     *     that enemy.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   secondCooldown = 1f
     *                      -> seconds between each damage tick while you stare at
     *                         a target
     *   damage         = 5
     *                      -> health drained per tick
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
    public class DemonEye : ISkill
    {
        private const Skills skillName = Skills.DemonEye;
        private static DemonEyeOptions Options => SkillConfigurationResolver.Get<DemonEyeOptions>(BuiltInSkillIds.DemonEye);
        private static readonly Skills[] hidingSkills = [Skills.Ghost, Skills.Ninja, Skills.C4Camouflage];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void OnTick()
        {
            int tickCooldown = (int)(64 * Options.SecondCooldown);
            if (Server.TickCount % tickCooldown != 0) return;

            int damage = Options.Damage;

            foreach (var player in PlayerManager.GetTickPlayers())
            {
                var playerEvent = PlayerManager.GetPlayerEvent(player);
                if (!Instance.IsPlayerValid(playerEvent)) continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(playerEvent!.Index);
                if (playerInfo?.Skill == skillName)
                    HitEnemies(playerEvent, damage);
            }
        }

        private static void HitEnemies(CCSPlayerController player, int damage)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null) return;
            
            int playerSlot = player.Slot;
            int playerBit = playerSlot % 32;

            if (playerSlot < 0)
                return;

            foreach (var enemy in PlayerManager.GetTickPlayers().FindAll(p => p != null && p.IsValid && p.Team != player.Team))
            {
                var enemyEvent = PlayerManager.GetPlayerEvent(enemy);
                if (enemyEvent == null || !enemyEvent.IsValid) continue;

                var enemyPawn = enemyEvent.PlayerPawn?.Value;
                if (enemyPawn == null || !enemyPawn.IsValid || enemyPawn.Health <= 0) continue;

                int enemySlot = enemy.Slot;
                if (enemySlot < 0)
                    return;

                int enemyBit = enemySlot % 32;
                uint playerMask = 1u << playerBit;
                uint enemyMask = 1u << enemyBit;

                bool playerSeesEnemy = (enemyPawn.EntitySpottedState.SpottedByMask[0] & playerMask) != 0;

                if (playerSeesEnemy)
                {
                    SkillUtils.TakeHealth(enemyPawn, damage, player, KillfeedIcons.Fist);
                    enemyEvent.EmitSound("Player.DamageBody.Onlooker");
                }
            }
        }
    }
}