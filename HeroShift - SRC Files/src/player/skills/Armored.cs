using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Armored - Incoming damage is reduced by a random multiplier rolled each
     * round.
     *
     * LOGIC
     *   EnableSkill: rolls the reduction between chanceFrom and chanceTo, stores
     *     it as SkillChance.
     *   OnTakeDamage: multiplies incoming damage by that rolled value.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   chanceFrom = .65f
     *                  -> lowest damage multiplier that can be rolled (0.65 = you
     *                     take 65% damage)
     *   chanceTo   = .85f
     *                  -> highest damage multiplier that can be rolled
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
    public class Armored : ISkill
    {
        private const Skills skillName = Skills.Armored;

        private static ArmoredOptions Options => SkillConfigurationResolver.Get<ArmoredOptions>(BuiltInSkillIds.Armored);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            float newScale = (float)Instance.Random.NextDouble() * (Options.ChanceTo - Options.ChanceFrom) + Options.ChanceFrom;
            playerInfo.SkillChance = newScale;
            newScale = (float)Math.Round(newScale, 2);

            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, newScale)}",
                border: !PlayerManager.GetTickPlayers().Any(p => p.IsValid && p.Team == player.Team && p != player) ? "tb" : "t");
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || !param.IsValid || param2 == null) return;

            var victimPawn = param.As<CCSPlayerPawn>();
            if (victimPawn == null || !victimPawn.IsValid || victimPawn.DesignerName != "player") return;

            var victimController = victimPawn.Controller.Value;
            if (victimController == null || !victimController.IsValid) return;

            var victim = victimController.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex((PlayerManager.GetPlayerEvent(victim)?.Index ?? victim.Index));
            if (playerInfo == null) return;

            if (playerInfo.Skill == skillName && victim.PawnIsAlive)
            {
                float? skillChance = playerInfo.SkillChance;
                param2.Damage *= skillChance ?? 1f;
            }
        }
    }
}