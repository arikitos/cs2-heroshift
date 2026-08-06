using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using src.SkillsCore;
using src.SkillsCore.BuiltIn;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

namespace src.player.skills
{
    public class Chameleon : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Chameleon;
        private static readonly ConcurrentDictionary<uint, byte> holders = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            holders.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            holders[player.Index] = 0;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null) return;
            holders.TryRemove(player.Index, out _);
        }

        public static void PlayerDisconnect(uint playerIndex)
        {
            holders.TryRemove(playerIndex, out _);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            try
            {
                if (holders.IsEmpty) return;

                var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
                if (attacker == null || !attacker.IsValid) return;

                var victim = PlayerManager.GetPlayerEvent(@event.Userid);
                if (victim == null || !victim.IsValid) return;

                if (attacker.Index == victim.Index || attacker.Team == victim.Team) return;
                if (!holders.ContainsKey(attacker.Index)) return;

                var victimInfo = PlayerManager.GetPlayerByIndex(victim.Index);
                if (victimInfo == null || victimInfo.IsDrawing) return;

                CopySkill(attacker, victim.PlayerName, victimInfo.Skill);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[HeroShift] Chameleon.PlayerDeath failed: {ex.Message}");
            }
        }

        private static void CopySkill(CCSPlayerController player, string victimName, SkillId victimSkill)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player.Index);
            if (playerInfo == null) return;

            var playerEvent = PlayerManager.GetPlayerFromEvent(player);
            if (!CanCopy(player, victimSkill, out string? blockReason))
            {
                if (blockReason != null && playerEvent != null && playerEvent.IsValid)
                    playerEvent.PrintToChat($" {ChatColors.Red}" + playerEvent.GetTranslation(blockReason, victimName));

                return;
            }

            uint playerIndex = player.Index;
            SkillId previousSkill = playerInfo.Skill;
            if (!holders.TryRemove(playerIndex, out _)) return;

            Instance.AddTimer(.1f, () =>
            {
                var target = Utilities.GetPlayerFromIndex((int)playerIndex);
                if (target == null || !target.IsValid) return;

                var targetInfo = PlayerManager.GetPlayerByIndex(playerIndex);
                if (targetInfo == null || targetInfo.Skill != previousSkill) return;

                Instance.InvokeDisableSkill(previousSkill, target);

                targetInfo.Skill = victimSkill;
                targetInfo.SpecialSkill = skillName;
                targetInfo.SkillChance = null;

                if (target.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

                if (SkillRuntime.GetMetadata(victimSkill).DisableOnFreezeTime && SkillUtils.IsFreezeTime())
                    Instance.AddTimer(Math.Max((float)(Event.GetFreezeTimeEnd() - DateTime.Now).TotalSeconds, 0), () =>
                    {
                        var copiedPlayer = Utilities.GetPlayerFromIndex((int)playerIndex);
                        if (copiedPlayer == null || !copiedPlayer.IsValid) return;
                        if (PlayerManager.GetPlayerByIndex(playerIndex)?.Skill != victimSkill) return;

                        Instance.InvokeEnableSkill(victimSkill, copiedPlayer);
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                else
                    Instance.InvokeEnableSkill(victimSkill, target);
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

            if (playerEvent != null && playerEvent.IsValid)
                playerEvent.PrintToChat($" {ChatColors.Green}" + playerEvent.GetTranslation("chameleon_copied_info", player.GetSkillName(victimSkill)));
        }

        private static bool CanCopy(CCSPlayerController player, SkillId victimSkill, out string? blockReason)
        {
            blockReason = null;

            if (victimSkill == BuiltInSkillIds.None || victimSkill == skillName) return false;
            if (SkillData.GetInfo(victimSkill) == null) return false;

            bool ctOnly = Event.counterterroristSkills.Any(skill => skill.Id == victimSkill);
            bool ttOnly = Event.terroristSkills.Any(skill => skill.Id == victimSkill);

            if ((player.Team == CsTeam.Terrorist && ctOnly) || (player.Team == CsTeam.CounterTerrorist && ttOnly))
            {
                blockReason = "chameleon_wrong_team_info";
                return false;
            }

            var metadata = SkillRuntime.GetMetadata(victimSkill);
            if (!player.IsBot && !string.IsNullOrEmpty(metadata.RequiredPermission)
                && !AdminManager.PlayerHasPermissions(player, metadata.RequiredPermission))
                return false;

            if (!metadata.NeedsTeammates) return true;

            return PlayerManager.GetTickPlayers().Any(candidate =>
                candidate != null && candidate.IsValid && candidate.Index != player.Index && candidate.Team == player.Team
                && candidate.PlayerPawn?.Value != null && candidate.PlayerPawn.Value.IsValid && candidate.PlayerPawn.Value.Health > 0);
        }
    }
}
