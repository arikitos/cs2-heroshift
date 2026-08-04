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
     * Astronaut - Low gravity - you jump much higher and fall slowly.
     *
     * LOGIC
     *   EnableSkill: rolls a gravity scale between chanceFrom and chanceTo and
     *     applies it to the pawn.
     *   DisableSkill/NewRound: restores normal gravity (1.0).
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   chanceFrom = .1f
     *                  -> lowest gravity scale that can be rolled (0.1 = almost
     *                     no gravity)
     *   chanceTo   = .7f
     *                  -> highest gravity scale that can be rolled
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
    public class Astronaut : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Astronaut;

        private static AstronautOptions Options => SkillConfigurationResolver.Get<AstronautOptions>(BuiltInSkillIds.Astronaut);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            ApplyGravityModifier(player);
        }

        public static void NewRound()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player != null && player.IsValid)
                    DisableSkill(player);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            pawn.ActualGravityScale = 1.0f;
        }

        private static void ApplyGravityModifier(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            var astronautOptions = Options;
            float gravityModifier = (float)Math.Round(Instance.Random.NextDouble() * (astronautOptions.ChanceTo - astronautOptions.ChanceFrom) + astronautOptions.ChanceFrom, 1);
            playerInfo.SkillChance = gravityModifier;

            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, gravityModifier)}",
                border: !PlayerManager.GetTickPlayers().Any(p => p.IsValid && p.Team == player.Team && p != player) ? "tb" : "t");

            pawn.ActualGravityScale = gravityModifier;
        }
    }
}