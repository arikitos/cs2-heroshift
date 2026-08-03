using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

namespace src.player.skills
{
    /*
     * PawelJumper - You get extra mid-air jumps (double/triple jump).
     *
     * LOGIC
     *   EnableSkill: rolls how many extra jumps you get this round.
     *   OnTick: allows a new jump in the air until that count is used up.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   extraJumpsMin = 1
     *                     -> fewest extra mid-air jumps that can be rolled
     *   extraJumpsMax = 4
     *                     -> most extra mid-air jumps that can be rolled
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
    public class PawelJumper : ISkill
    {
        private const Skills skillName = Skills.PawelJumper;
        private static readonly int?[] J = new int?[64];
        private static readonly PlayerButtons[] LB = new PlayerButtons[64];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"), false);
        }

        public static void OnTick()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player == null || !player.IsValid) return;
                var playerEvent = PlayerManager.GetPlayerEvent(player);
                var playerInfo = PlayerManager.GetPlayerByIndex(playerEvent!.Index);

                if (playerInfo?.Skill == skillName)
                    GiveAdditionalJump(player, playerEvent);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerPawn == null || playerInfo == null) return;

            var skillConfig = SkillsInfo.LoadedConfig.FirstOrDefault(s => s.Name == skillName.ToString());
            if (skillConfig == null) return;

            float extraJumps = (float)Instance.Random.Next(SkillsInfo.GetValue<int>(skillName, "extraJumpsMin"), SkillsInfo.GetValue<int>(skillName, "extraJumpsMax") + 1);
            playerInfo.SkillChance = extraJumps;
            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, extraJumps)}",
                border: !PlayerManager.GetTickPlayers().Any(p => p.Team == player.Team && p != player) ? "tb" : "t");
        }

        private static void GiveAdditionalJump(CCSPlayerController player, CCSPlayerController playerEvent)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;

            var playerEventPawn = playerEvent.PlayerPawn.Value;
            if (playerEventPawn == null || !playerEventPawn.IsValid) return;

            var flags = (PlayerFlags)playerEventPawn.Flags;
            var buttons = player.Buttons;

            bool isJumpDown = (playerPawn.MovementServices?.Buttons?.ButtonStates[0] & (ulong)PlayerButtons.Jump) != 0 || (buttons & PlayerButtons.Jump) != 0;
            bool wasJumpDown = (LB[player.Slot] & PlayerButtons.Jump) != 0;

            bool jumpPressed = (isJumpDown && !wasJumpDown)
                || (playerPawn.MovementServices?.QueuedButtonChangeMask & (ulong)PlayerButtons.Jump) != 0;

            var playerInfo = PlayerManager.GetPlayerByIndex(playerEvent!.Index);
            if (playerInfo == null) return;

            bool isOnGround = (flags & PlayerFlags.FL_ONGROUND) != 0;

            if (isOnGround)
                J[player.Slot] = 0;
            else if (jumpPressed && J[player.Slot] < playerInfo.SkillChance + 1)
            {
                J[player.Slot]++;
                playerEventPawn.AbsVelocity.Z = 300;
            }

            LB[player.Slot] = buttons;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#FFA500", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, int extraJumpsMin = 1, int extraJumpsMax = 4) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public int ExtraJumpsMin { get; set; } = extraJumpsMin;
            public int ExtraJumpsMax { get; set; } = extraJumpsMax;
        }
    }
}