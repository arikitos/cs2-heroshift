using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using System.Collections.Concurrent;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Medic - You carry healthshots and can heal yourself or teammates.
     *
     * LOGIC
     *   UseSkill: consumes a healthshot and restores healthToAdd.
     *   OnTick: enforces the cooldown between uses.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   healthToAdd     = 50
     *                       -> health restored per use
     *   healthShotLimit = 3
     *                       -> how many heals you get per round
     *   cooldown        = 1f
     *                       -> seconds between two heals
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
    public class Medic : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Medic;
        private static MedicOptions Options => SkillConfigurationResolver.Get<MedicOptions>(BuiltInSkillIds.Medic);
        private static readonly ConcurrentDictionary<uint, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            lock (setLock)
                SkillPlayerInfo.Clear();
        }

        public static void OnTick()
        {
            if (!SkillUtils.IsHudFrame()) return;
            if (SkillUtils.IsFreezeTime()) return;
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill == skillName)
                    if (SkillPlayerInfo.TryGetValue(player.Index, out var skillInfo))
                        UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryAdd(player.Index, new PlayerSkillInfo
            {
                SteamID = player.Index,
                CanUse = true,
                Cooldown = DateTime.MinValue,
                Count = Options.HealthShotLimit,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryRemove(player.Index, out _);
            SkillUtils.ResetPrintHTML(player);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)Math.Ceiling((skillInfo.Cooldown.AddSeconds(Options.Cooldown) - DateTime.Now).TotalSeconds);
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            string remainingLine = cooldown != 0
                ? $"{player.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}"
                : $"<font color='#{(skillInfo == null || skillInfo.Count == 0 ? "FF0000" : "00FF00")}'>{(skillInfo == null ? 0 : skillInfo.Count)}/{Options.HealthShotLimit}</font>";

            playerInfo.PrintHTML = remainingLine;
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.Index, out var skillInfo))
            {
                if (!player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
                if (skillInfo.CanUse && skillInfo.Count != 0)
                {
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;
                    skillInfo.Count -= 1;
                    SkillUtils.AddHealth(playerPawn, Options.HealthToAdd);
                    player.EmitSound("Healthshot.Success");
                }
            }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public int Count { get; set; }
            public DateTime Cooldown { get; set; }
        }
    }
}