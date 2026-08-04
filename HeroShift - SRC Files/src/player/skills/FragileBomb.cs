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
     * FragileBomb - The planted C4 can be destroyed by shooting it.
     *
     * LOGIC
     *   BombPlanted: gives the bomb a health pool.
     *   BulletImpact: bullets hitting the C4 reduce that health until it is
     *     destroyed.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   maxBombHealth = 1000
     *                     -> hit points of the planted C4 before it is destroyed
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
    public class FragileBomb : ISkill
    {
        private const Skills skillName = Skills.FragileBomb;
        private static FragileBombOptions Options => SkillConfigurationResolver.Get<FragileBombOptions>(BuiltInSkillIds.FragileBomb);
        private static int bombHealth = 1000;
        private static int maxBombHealth = 1000;

        private static int lastTick = 0;
        private static Vector? plantedC4;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            bombHealth = Options.MaxBombHealth;
            maxBombHealth = Options.MaxBombHealth;
            plantedC4 = null;
        }

        public static void BombPlanted(EventBombPlanted _)
        {
            var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (plantedBomb == null || !plantedBomb.IsValid || plantedBomb.AbsOrigin == null) return;
            plantedC4 = new(plantedBomb.AbsOrigin.X, plantedBomb.AbsOrigin.Y, plantedBomb.AbsOrigin.Z);
        }

        private static void RemoveBomb()
        {
            plantedC4 = null;
            var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (plantedBomb != null && plantedBomb.IsValid)
                plantedBomb.AddEntityIOEvent("Kill", plantedBomb, delay: 0.1f);
            SkillUtils.TerminateRound(CsTeam.CounterTerrorist);
        }

        public static void BulletImpact(EventBulletImpact @event)
        {
            if (lastTick == Server.TickCount) return;

            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid || plantedC4 == null) return;

            var pos = new Vector(@event.X, @event.Y, @event.Z);

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null || playerInfo.Skill != skillName) return;

            if (SkillUtils.Distance(plantedC4, pos) >= 8)
                return;

            lastTick = Server.TickCount;
            bombHealth -= Instance.Random.Next(25, 42);

            if (bombHealth <= 0)
            {
                RemoveBomb();
                return;
            }

            Localization.PrintTranslationToChatAll($" {ChatColors.Gold}{{0}}: {ChatColors.Red}{bombHealth}{ChatColors.Gold}/{ChatColors.Green}{maxBombHealth}", ["fragilebomb_bomb_health"]);
        }
    }
}