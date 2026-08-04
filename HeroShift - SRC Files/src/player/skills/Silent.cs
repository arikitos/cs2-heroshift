using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * Silent - You make no sound at all.
     *
     * LOGIC
     *   PlayerMakeSound: clears um.Recipients for your sound events so nobody
     *     hears you.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
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
    public class Silent : ISkill
    {
        private const Skills skillName = Skills.Silent;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void PlayerMakeSound(UserMessage um)
        {
            var soundevent = um.ReadUInt("soundevent_hash");
            var userIndex = um.ReadUInt("source_entity_index");
            if (userIndex == 0) return;

            if (!Instance.footstepSoundEvents.Contains(soundevent) && !Instance.silentSoundEvents.Contains(soundevent))
                return;

            var player = PlayerManager.GetTickPlayers().FirstOrDefault(p => p.Pawn?.Value != null && p.Pawn.Value.IsValid && p.Pawn.Value.Index == userIndex);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex((PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index));
            if (playerInfo?.Skill != skillName) return;

            um.Recipients.Clear();
        }
    }
}