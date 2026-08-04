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
     * Soldier - You deal increased damage with all weapons.
     *
     * LOGIC
     *   EnableSkill: rolls a damage multiplier between chanceFrom and chanceTo.
     *   OnTakeDamage: multiplies the damage you deal by that value.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   chanceFrom = 1.15f
     *                  -> lowest damage multiplier that can be rolled (1.15 =
     *                     +15% damage)
     *   chanceTo   = 1.35f
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
    public class Soldier : ISkill
    {
        private const Skills skillName = Skills.Soldier;

        private static SoldierOptions Options => SkillConfigurationResolver.Get<SoldierOptions>(BuiltInSkillIds.Soldier);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            float newScale = (float)Instance.Random.NextDouble() * (Options.ChanceTo - Options.ChanceFrom) + Options.ChanceFrom;
            playerInfo.SkillChance = newScale;

            newScale = (float)Math.Round(newScale, 2);
            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, newScale)}",
                border: !PlayerManager.GetTickPlayers().Any(p => p.Team == player.Team && p != player) ? "tb" : "t");
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
                return;

            CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(param.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return;

            CCSPlayerController attacker = PlayerManager.GetPlayerEvent(attackerPawn.Controller.Value.As<CCSPlayerController>())!;

            var playerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
            if (playerInfo == null) return;

            if (playerInfo.Skill == skillName)
            {
                float? skillChance = playerInfo.SkillChance;
                param2.Damage *= skillChance ?? 1f;
            }
        }
    }
}