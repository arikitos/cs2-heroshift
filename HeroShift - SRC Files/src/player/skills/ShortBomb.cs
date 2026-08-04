using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * ShortBomb - The bomb you plant explodes much sooner.
     *
     * LOGIC
     *   BombPlanted: overrides the C4 countdown with detonationTime.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   detonationTime = 20
     *                      -> seconds on the C4 timer instead of the default 40
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.Terrorist
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = 1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class ShortBomb : ISkill
    {
        private const Skills skillName = Skills.ShortBomb;
        private static ShortBombOptions Options => SkillConfigurationResolver.Get<ShortBombOptions>(BuiltInSkillIds.ShortBomb);
        // mp_c4timer is an Int32 cvar; captured at load so restore never picks up another skill's override.
        private static int defaultC4Timer = 40;
        private static bool c4TimerOverridden;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
            defaultC4Timer = ConVar.Find("mp_c4timer")?.GetPrimitiveValue<int>() ?? 40;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            // At round start (not at plant) so the client HUD/alert countdown is right before the plant completes.
            c4TimerOverridden = true;
            Server.ExecuteCommand($"mp_c4timer {Options.DetonationTime}");
        }

        public static void NewRound()
        {
            if (!c4TimerOverridden) return;

            c4TimerOverridden = false;
            Server.ExecuteCommand($"mp_c4timer {defaultC4Timer}");
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (plantedBomb != null)
                Server.NextFrame(() =>
                {
                    if (plantedBomb != null && plantedBomb.IsValid)
                        plantedBomb.C4Blow = Server.CurrentTime + Options.DetonationTime;
                });

            foreach (var p in PlayerManager.GetTickPlayers().Where(p => p.IsValid))
                p.PrintToCenterAlert(p.GetTranslation("bombplanted", Options.DetonationTime));
        }
    }
}