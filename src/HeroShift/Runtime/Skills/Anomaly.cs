using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Anomaly - Rewinds you back to where you stood a few seconds ago.
     *
     * LOGIC
     *   OnTick: continuously records your position history (a trail of past
     *     origins).
     *   UseSkill: teleports you back to the recorded position from N seconds ago.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   secondsInBack = 5
     *                     -> how many seconds into the past you are teleported
     *   cooldown      = 15
     *                     -> seconds before the skill can be used again
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
    public class Anomaly : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Anomaly;
        private static AnomalyOptions Options => SkillConfigurationResolver.Get<AnomalyOptions>(BuiltInSkillIds.Anomaly);
        private static readonly float tickRate = 64;

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
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player == null || !player.IsValid) continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill == skillName)
                {
                    if (SkillPlayerInfo.TryGetValue(player.Index, out var skillInfo))
                    {
                        UpdateHUD(player, skillInfo);
                        if (Server.TickCount % tickRate != 0) continue;

                        var pawn = player.PlayerPawn.Value;
                        if (pawn != null && pawn.IsValid && pawn.AbsOrigin != null)
                        {
                            if (skillInfo.LastRotations == null || skillInfo.LastPositions == null) continue;

                            skillInfo.LastPositions.Enqueue(new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z));
                            skillInfo.LastRotations.Enqueue(new QAngle(pawn.V_angle.X, pawn.V_angle.Y, 0));

                            if (skillInfo.LastRotations.Count > Options.SecondsInBack)
                            {
                                skillInfo.LastPositions.TryDequeue(out _);
                                skillInfo.LastRotations.TryDequeue(out _);
                            }
                        }
                    }
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            SkillPlayerInfo.TryAdd(player.Index, new PlayerSkillInfo
            {
                PlayerIndex = player.Index,
                CanUse = true,
                Cooldown = DateTime.MinValue,
                LastPositions = new ConcurrentQueue<Vector>(),
                LastRotations = new ConcurrentQueue<QAngle>()
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            SkillPlayerInfo.TryRemove(player.Index, out _);
            SkillUtils.ResetPrintHTML(player);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            if (player == null || !player.IsValid || skillInfo == null) return;

            float cooldown = 0;
            float time = (int)Math.Ceiling((skillInfo.Cooldown.AddSeconds(Options.Cooldown) - DateTime.Now).TotalSeconds);
            cooldown = Math.Max(time, 0);

            if (cooldown == 0 && !skillInfo.CanUse)
                skillInfo.CanUse = true;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            if (cooldown == 0)
                playerInfo.PrintHTML = null;
            else
                playerInfo.PrintHTML = $"{player.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}";
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (SkillPlayerInfo.TryGetValue(player.Index, out var skillInfo))
            {
                if (!skillInfo.CanUse) return;

                skillInfo.Cooldown = DateTime.Now;
                skillInfo.CanUse = false;

                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn != null && playerPawn.IsValid)
                {
                    if (skillInfo.LastPositions == null || skillInfo.LastPositions.IsEmpty) return;
                    if (skillInfo.LastRotations == null || skillInfo.LastRotations.IsEmpty) return;

                    Vector? lastPosition = skillInfo.LastPositions.FirstOrDefault();
                    QAngle? lastRotation = skillInfo.LastRotations.FirstOrDefault();

                    if (lastPosition != null && lastRotation != null)
                    {
                        playerPawn.Teleport(lastPosition, null, null);
                        playerPawn.Look(lastRotation);
                    }
                }
            }
        }

        public class PlayerSkillInfo
        {
            public uint PlayerIndex { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
            public ConcurrentQueue<Vector>? LastPositions { get; set; }
            public ConcurrentQueue<QAngle>? LastRotations { get; set; }
        }
    }
}