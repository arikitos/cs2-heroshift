using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * Saper - You plant and defuse the bomb instantly.
     *
     * LOGIC
     *   BombBeginplant/BombBegindefuse: completes the action immediately.
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
    public class Saper : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Saper;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void BombBegindefuse(EventBombBegindefuse @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (Instance.IsPlayerValid(player))
            {
                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill == skillName)
                {
                    var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                    if (plantedBomb != null)
                        Server.NextFrame(() =>
                        {
                            if (plantedBomb != null && plantedBomb.IsValid)
                                plantedBomb.DefuseCountDown = 0;
                        });
                }
            }
        }

        public static void BombBeginplant(EventBombBeginplant @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill == skillName)
            {
                var bomb = PlayerManager.GetTickBomb();
                if (bomb != null && bomb.IsValid)
                {
                    bomb.BombPlacedAnimation = false;
                    bomb.ArmedTime = 0.0f;
                }
            }
        }
    }
}