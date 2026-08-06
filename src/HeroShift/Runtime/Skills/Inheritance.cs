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
    public class Inheritance : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Inheritance;
        private static readonly ConcurrentDictionary<uint, byte> holders = [];
        private static readonly ConcurrentDictionary<uint, FallenInfo> fallen = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            holders.Clear();
            fallen.Clear();

            foreach (var player in PlayerManager.GetTickPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            holders[player.Index] = 0;
            RefreshMenu(player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null) return;

            holders.TryRemove(player.Index, out _);
            SkillUtils.CloseMenu(player);
        }

        public static void PlayerDisconnect(uint playerIndex)
        {
            holders.TryRemove(playerIndex, out _);
            fallen.TryRemove(playerIndex, out _);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);
            if (victim == null || !victim.IsValid) return;

            holders.TryRemove(victim.Index, out _);

            var victimInfo = PlayerManager.GetPlayerByIndex(victim.Index);
            if (victimInfo == null || victimInfo.IsDrawing) return;
            if (victimInfo.Skill == BuiltInSkillIds.None || victimInfo.Skill == skillName) return;
            if (SkillData.GetInfo(victimInfo.Skill) == null) return;

            fallen[victim.Index] = new FallenInfo
            {
                PlayerName = victim.PlayerName,
                Skill = victimInfo.Skill,
                Team = victim.Team,
            };

            foreach (uint holderIndex in holders.Keys)
            {
                var holder = Utilities.GetPlayerFromIndex((int)holderIndex);
                if (holder == null || !holder.IsValid || holder.Team != victim.Team) continue;

                RefreshMenu(holder);
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (PlayerManager.GetPlayerByIndex(player.Index)?.Skill != skillName) return;

            var playerEvent = PlayerManager.GetPlayerFromEvent(player);
            if (playerEvent == null || !playerEvent.IsValid) return;

            if (commands == null || commands.Length == 0 || !uint.TryParse(commands[0], out uint fallenIndex))
            {
                playerEvent.PrintToChat($" {ChatColors.Red}" + playerEvent.GetTranslation("selectplayerskill_incorrect_enemy_index"));
                return;
            }

            if (!fallen.TryGetValue(fallenIndex, out var fallenInfo) || !CanInherit(player, fallenInfo))
            {
                playerEvent.PrintToChat($" {ChatColors.Red}" + playerEvent.GetTranslation("selectplayerskill_incorrect_enemy_index"));
                return;
            }

            InheritSkill(player, fallenInfo);
        }

        private static void InheritSkill(CCSPlayerController player, FallenInfo fallenInfo)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player.Index);
            if (playerInfo == null) return;

            var playerEvent = PlayerManager.GetPlayerFromEvent(player);
            if (playerEvent == null || !playerEvent.IsValid) return;

            uint playerIndex = player.Index;
            SkillId inheritedSkill = fallenInfo.Skill;

            holders.TryRemove(playerIndex, out _);
            SkillUtils.CloseMenu(player);

            Instance.AddTimer(.1f, () =>
            {
                var target = Utilities.GetPlayerFromIndex((int)playerIndex);
                if (target == null || !target.IsValid) return;

                var targetInfo = PlayerManager.GetPlayerByIndex(playerIndex);
                if (targetInfo == null || targetInfo.Skill != skillName) return;

                targetInfo.Skill = inheritedSkill;
                targetInfo.SpecialSkill = skillName;
                SkillUtils.CloseMenu(target);

                if (SkillRuntime.GetMetadata(inheritedSkill).DisableOnFreezeTime && SkillUtils.IsFreezeTime())
                    Instance.AddTimer(Math.Max((float)(Event.GetFreezeTimeEnd() - DateTime.Now).TotalSeconds, 0), () =>
                    {
                        var heir = Utilities.GetPlayerFromIndex((int)playerIndex);
                        if (heir == null || !heir.IsValid) return;
                        if (PlayerManager.GetPlayerByIndex(playerIndex)?.Skill != inheritedSkill) return;

                        Instance.InvokeEnableSkill(inheritedSkill, heir);
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                else
                    Instance.InvokeEnableSkill(inheritedSkill, target);
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

            playerEvent.PrintToChat($" {ChatColors.Green}" + playerEvent.GetTranslation("inheritance_player_info", fallenInfo.PlayerName));
        }

        private static void RefreshMenu(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (PlayerManager.GetPlayerByIndex(player.Index)?.Skill != skillName) return;

            ConcurrentBag<(string, string)> menuItems = [];
            foreach (var (fallenIndex, fallenInfo) in fallen)
            {
                if (fallenIndex == player.Index || !CanInherit(player, fallenInfo)) continue;

                menuItems.Add(($"\u202A{fallenInfo.PlayerName}\u202C : {player.GetSkillName(fallenInfo.Skill)}", fallenIndex.ToString()));
            }

            if (menuItems.IsEmpty) return;

            if (SkillUtils.HasMenu(player))
                SkillUtils.UpdateMenu(player, menuItems);
            else
                SkillUtils.CreateMenu(player, menuItems);
        }

        private static bool CanInherit(CCSPlayerController player, FallenInfo fallenInfo)
        {
            if (fallenInfo.Team != player.Team) return false;

            var metadata = SkillRuntime.GetMetadata(fallenInfo.Skill);
            if (!player.IsBot && !string.IsNullOrEmpty(metadata.RequiredPermission)
                && !AdminManager.PlayerHasPermissions(player, metadata.RequiredPermission))
                return false;

            if (!metadata.NeedsTeammates) return true;

            return PlayerManager.GetTickPlayers().Any(candidate =>
                candidate != null && candidate.IsValid && candidate.Index != player.Index && candidate.Team == player.Team
                && candidate.PlayerPawn?.Value != null && candidate.PlayerPawn.Value.IsValid && candidate.PlayerPawn.Value.Health > 0);
        }

        private sealed class FallenInfo
        {
            public required string PlayerName { get; init; }
            public required SkillId Skill { get; init; }
            public required CsTeam Team { get; init; }
        }
    }
}
