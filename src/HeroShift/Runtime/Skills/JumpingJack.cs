using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * JumpingJack - Every jump you make heals you a little.
     *
     * LOGIC
     *   PlayerJump: adds healthToAdd to your health each time you jump.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   healthToAdd = 3
     *                   -> health gained per jump
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
    public class JumpingJack : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.JumpingJack;

        private static JumpingJackOptions Options => SkillConfigurationResolver.Get<JumpingJackOptions>(BuiltInSkillIds.JumpingJack);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;

            var playerEvent = PlayerManager.GetPlayerEvent(player);
            if (playerEvent == null || !playerEvent.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(playerEvent.Index);
            if (playerInfo?.Skill != skillName) return;

            SkillUtils.AddHealth(playerEvent.PlayerPawn.Value, Options.HealthToAdd);
        }
    }
}