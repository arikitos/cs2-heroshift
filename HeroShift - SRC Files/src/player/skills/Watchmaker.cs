using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Watchmaker - Lets you change the round time / bomb timer.
     *
     * LOGIC
     *   BombPlanted/OnEntitySpawned: adjusts the timer by changeRoundTime and
     *     plays a sound.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   changeRoundTime = 7
     *                       -> seconds added to / removed from the timer per use
     *   soundEvent      = "UIPanorama.sidemenu_select"
     *                       -> sound played when the timer is changed
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.None
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = 1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class Watchmaker : ISkill
    {
        private const Skills skillName = Skills.Watchmaker;
        private static WatchmakerOptions Options => SkillConfigurationResolver.Get<WatchmakerOptions>(BuiltInSkillIds.Watchmaker);
        private static bool bombPlanted = false;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            bombPlanted = false;
        }

        public static void BombPlanted(EventBombPlanted _)
        {
            bombPlanted = true;
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            if (bombPlanted) return;
            var name = entity.DesignerName;
            if (!name.EndsWith("_projectile")) return;

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid) return;

            var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;
            var player = pawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = PlayerManager.GetPlayerByIndex((PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index));
            if (playerInfo?.Skill != skillName || Instance.GameRules == null) return;

            var roundTime = Options.ChangeRoundTime;
            Instance.GameRules.RoundTime += player.Team == CsTeam.Terrorist ? roundTime : -roundTime;

            if (player.Team == CsTeam.Terrorist)
                Localization.PrintTranslationToChatAll($" {ChatColors.Orange}{{0}}", ["watchmaker_tt"], [roundTime]);
            else
                Localization.PrintTranslationToChatAll($" {ChatColors.LightBlue}{{0}}", ["watchmaker_ct"], [roundTime]);

            player.EmitSound(Options.SoundEvent);

            var proxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (proxy == null) return;
            Utilities.SetStateChanged(proxy, "CCSGameRulesProxy", "m_pGameRules");
        }
    }
}