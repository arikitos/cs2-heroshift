using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * NoRecoil - Your weapons have no recoil or spray pattern.
     *
     * LOGIC
     *   OnTick: resets the aim punch/recoil values every tick.
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
    public class NoRecoil : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.NoRecoil;

        private static readonly ConcurrentDictionary<uint, byte> holders = [];
        private static bool noSpreadActive;

        private static bool defaultNoSpread;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
            defaultNoSpread = ConVar.Find("weapon_accuracy_nospread")?.GetPrimitiveValue<bool>() ?? false;
        }

        public static void NewRound()
        {
            holders.Clear();
            ApplyNoSpread(false);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            holders.TryAdd(player.Index, 0);
            ApplyNoSpread(true);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            holders.TryRemove(player.Index, out _);
            if (holders.IsEmpty)
                ApplyNoSpread(false);
        }

        private static void ApplyNoSpread(bool enabled)
        {
            if (noSpreadActive == enabled) return;

            noSpreadActive = enabled;
            bool value = enabled || defaultNoSpread;
            Server.ExecuteCommand($"weapon_accuracy_nospread {(value ? 1 : 0)}");
        }

        public static void OnTick()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;
                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);

                if (playerInfo?.Skill == skillName)
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid) continue;

                    if (pawn.AimPunchServices != null)
                    {
                        pawn.AimPunchServices.PredictableBaseTick = 0;
                        pawn.AimPunchServices.PredictableBaseTickInterpAmount = 0;
                        pawn.AimPunchServices.UnpredictableBaseTick = 0;
                    }

                    if (pawn.CameraServices != null)
                    {
                        pawn.CameraServices.CsViewPunchAngleTick = 0;
                        pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;
                    }
                }
            }
        }
    }
}