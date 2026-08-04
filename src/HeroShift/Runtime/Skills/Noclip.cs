using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Noclip - Short burst of noclip - you can fly through walls.
     *
     * LOGIC
     *   UseSkill: enables noclip for 'duration' seconds.
     *   OnTick: disables it on expiry; if you end up stuck inside geometry,
     *     cooldownWhenStuck is used instead of the normal cooldown.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   cooldown          = 30f
     *                         -> seconds before the skill can be used again
     *   duration          = 2f
     *                         -> how long (seconds) noclip stays on
     *   cooldownWhenStuck = 5f
     *                         -> shorter cooldown applied when you got stuck in a
     *                            wall
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
    public class Noclip : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Noclip;
        private static NoclipOptions Options => SkillConfigurationResolver.Get<NoclipOptions>(BuiltInSkillIds.Noclip);
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
                IsFlying = false,
                Cooldown = DateTime.MinValue,
                LastPosition = null,
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
            float flying = 0;
            if (skillInfo != null)
            {
                float time = (int)Math.Ceiling((skillInfo.Cooldown.AddSeconds(Options.Cooldown) - DateTime.Now).TotalSeconds);
                cooldown = Math.Max(time, 0);

                float flyingTime = (int)(skillInfo.Cooldown.AddSeconds(Options.Duration) - DateTime.Now).TotalMilliseconds;
                flying = Math.Max(flyingTime, 0);

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            if (cooldown == 0)
            {
                playerInfo.PrintHTML = null;
                return;
            }

            playerInfo.PrintHTML =
                skillInfo?.IsFlying == true
                    ? $"{player.GetTranslation("active_hud_info", $"<font color='#00FF00'>{Math.Round(flying / 100, 2)}</font>")}"
                    : $"{player.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}";
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            uint playerIndex = player.Index;

            if (SkillPlayerInfo.TryGetValue(playerIndex, out var skillInfo))
            {
                if (skillInfo.IsFlying)
                {
                    StopFlying(player, skillInfo);
                    return;
                }

                if (skillInfo.CanUse)
                {
                    var duration = Options.Duration;

                    skillInfo.CanUse = false;
                    skillInfo.IsFlying = true;
                    skillInfo.Cooldown = DateTime.Now;
                    skillInfo.LastPosition = playerPawn.AbsOrigin == null ? null : new Vector(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z);

                    SetNoclip(player, true);
                    skillInfo.Timer?.Kill();

                    skillInfo.Timer = Instance.AddTimer(duration, () =>
                    {
                        var player = Utilities.GetPlayerFromIndex((int)playerIndex);
                        if (player == null || !player.IsValid) return;

                        var playerInfo = PlayerManager.GetPlayerByIndex(playerIndex);
                        if (playerInfo == null) return;

                        StopFlying(player, skillInfo);
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        }

        private static void SetNoclip(CCSPlayerController player, bool noclip = true)
        {
            if (player == null || !player.IsValid) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            playerPawn.MoveType = noclip ? MoveType_t.MOVETYPE_NOCLIP : MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(playerPawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)playerPawn.MoveType);
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_MoveType");
        }

        private static void StopFlying(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            skillInfo.Timer?.Kill();
            skillInfo.Timer = null;

            if (!skillInfo.IsFlying) return;
            skillInfo.IsFlying = false;

            if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            SetNoclip(player, false);

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || skillInfo.IsFlying) return;

            Vector? safePoint = GetCorrectPosition(player, skillInfo);
            playerPawn.Teleport(safePoint ?? skillInfo.LastPosition, null, new Vector(0, 0, 0));
        }

        private static Vector? GetCorrectPosition(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null) return null;

            Vector currentPos = playerPawn.AbsOrigin;
            float offset = 50;

            Vector[] checkOffsets =
            {
                currentPos,
                currentPos + new Vector(offset, 0, 10),
                currentPos + new Vector(-offset, 0, 10),
                currentPos + new Vector(0, offset, 10),
                currentPos + new Vector(0, -offset, 10),

                currentPos + new Vector(offset, 0, 60),
                currentPos + new Vector(-offset, 0, 60),
                currentPos + new Vector(0, offset, 60),
                currentPos + new Vector(0, -offset, 60),
            };

            ulong mask = playerPawn.Collision.CollisionAttribute.InteractsWith;
            ulong contents = playerPawn.Collision.CollisionGroup;

            bool hasGround = false;
            Vector stuckVector = new(currentPos.X, currentPos.Y, currentPos.Z);

            foreach (Vector targetPos in checkOffsets)
            {
                Vector start = new(targetPos.X, targetPos.Y, targetPos.Z + 70);
                Vector end = new(targetPos.X, targetPos.Y, targetPos.Z - 1000);

                var groundResult = HeroShift.Instance.TraceService.TraceShape(player, start, end, mask, contents);
                if (!groundResult.HasValue || !groundResult.Value.DidHit) continue;

                Vector newPos =
                    groundResult.Value.EndPos.Z > targetPos.Z
                    ? new(groundResult.Value.EndPos.X, groundResult.Value.EndPos.Y, groundResult.Value.EndPos.Z)
                    : targetPos;

                hasGround = true;
                stuckVector = newPos;

                var result = HeroShift.Instance.TraceService.TraceHullShape(
                    targetPos,
                    targetPos,
                    player
                );

                if (result.HasValue && !result.Value.DidHit)
                    return newPos;
            }

            if (hasGround)
                skillInfo.Cooldown = DateTime.Now.AddSeconds(-Options.Cooldown + Options.CooldownWhenStuck);
            return hasGround ? stuckVector : null;
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public bool IsFlying { get; set; }
            public DateTime Cooldown { get; set; }
            public Vector? LastPosition { get; set; }
            public Timer? Timer { get; set; }
        }
    }
}