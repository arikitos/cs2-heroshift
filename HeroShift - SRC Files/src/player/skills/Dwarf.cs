using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;
using src.utils;
using static src.HeroShift;

namespace src.player.skills
{
    /*
     * Dwarf - You are shrunk down - smaller hitbox, lower viewpoint.
     *
     * LOGIC
     *   NewRound/EnableSkill: rolls a scale between minScale and maxScale and
     *     applies it.
     *   DisableSkill: restores scale 1.0.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   minScale = .6f
     *                -> smallest body scale that can be rolled (0.6 = 60% size)
     *   maxScale = .95f
     *                -> largest body scale that can be rolled
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
    public class Dwarf : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Dwarf;

        private static DwarfOptions Options => SkillConfigurationResolver.Get<DwarfOptions>(BuiltInSkillIds.Dwarf);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void NewRound()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;
                DisableSkill(player);
            }
        }

        public static unsafe void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null && player.IsValid)
            {
                var dwarfOptions = Options;
                float newSize = (float)Instance.Random.NextDouble() * (dwarfOptions.MaxScale - dwarfOptions.MinScale) + dwarfOptions.MinScale;
                newSize = (float)Math.Round(newSize, 2);
                playerInfo.SkillChance = newSize;

                SkillUtils.ChangePlayerScale(player, newSize);
                SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, newSize)}",
                    border: !PlayerManager.GetTickPlayers().Any(p => p.Team == player.Team && p != player) ? "tb" : "t");
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null && playerPawn?.CBodyComponent != null)
            {
                SkillUtils.ChangePlayerScale(player, 1);
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
            }
        }
    }
}