using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Regeneration - Your health regenerates over time.
     *
     * LOGIC
     *   OnTick: periodically adds health back up to the normal maximum.
     */
    public class Regeneration : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Regeneration;

        private static RegenerationOptions Options => SkillConfigurationResolver.Get<RegenerationOptions>(BuiltInSkillIds.Regeneration);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void OnTick()
        {
            int cooldown = Math.Max(1, (int)(64 * Options.Cooldown));
            if (Server.TickCount % cooldown != 0) return;
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill != skillName) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;
                SkillUtils.AddHealth(pawn, Options.HealthToAdd);
            }
        }
    }
}