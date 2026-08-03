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
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
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
        private const Skills skillName = Skills.JumpingJack;

        private static JumpingJackOptions Options => SkillConfigurationResolver.Get<JumpingJackOptions>(BuiltInSkillIds.JumpingJack);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
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

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#a86eff", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, int healthToAdd = 3) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public int HealthToAdd { get; set; } = healthToAdd;
        }
    }
}