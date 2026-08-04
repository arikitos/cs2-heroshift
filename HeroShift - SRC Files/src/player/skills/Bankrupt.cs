using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * Bankrupt - Curse: the chosen enemy loses all their money.
     *
     * LOGIC
     *   TypeSkill: you pick the victim; OnTick drives the menu/targeting. The
     *     victim's account is zeroed when the curse is applied.
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
    public class Bankrupt : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Bankrupt;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player != null && player.IsValid)
                    SkillUtils.CloseMenu(player);
            }
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;

            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player == null || !player.IsValid) continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                if (!SkillUtils.HasMenu(player)) continue;

                var enemies = SkillUtils.GetSelectableEnemies(player, true);
                ConcurrentBag<(string, string)> menuItems = [];

                foreach (var e in enemies)
                {
                    int money = e.InGameMoneyServices?.Account ?? 0;
                    menuItems.Add(($"\u202A{e.PlayerName}\u202C : {money}$", e.Index.ToString()));
                }

                if (!menuItems.IsEmpty)
                    SkillUtils.UpdateMenu(player, menuItems);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            playerInfo.SkillUsed = false;

            var enemies = PlayerManager.GetTickPlayers().Where(p => p.IsValid && p.PlayerPawn?.Value?.Health > 0 && p.Team != player.Team && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
            if (enemies.Length > 0)
            {
                ConcurrentBag<(string, string)> menuItems = [.. enemies.Select(e => ($"\u202A{e.PlayerName}\u202C : {(e.InGameMoneyServices?.Account ?? 0)}$", e.Index.ToString()))];
                SkillUtils.CreateMenu(player, menuItems);
            }
            else
            {
                player.PrintToChat($" {ChatColors.Red}{player.GetTranslation("selectplayerskill_incorrect_enemy_index")}");
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || commands.Length < 1) return;

            string option = commands[0];
            if (string.IsNullOrEmpty(option)) return;

            var playerEvent = PlayerManager.GetPlayerFromEvent(player);
            if (playerEvent == null || !playerEvent.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (playerInfo.SkillUsed)
            {
                playerEvent.PrintToChat($" {ChatColors.Red}{playerEvent.GetTranslation("selectplayerskill_used_info")}");
                return;
            }

            if (uint.TryParse(option, out uint enemyIndex))
            {
                var enemy = Utilities.GetEntityFromIndex<CCSPlayerController>((int)enemyIndex);
                if (enemy != null && enemy.IsValid && enemy.PlayerPawn?.Value?.Health > 0 && enemy.Team != player.Team)
                {
                    ResetMoney(enemy);
                    playerInfo.SkillUsed = true;
                    SkillUtils.CloseMenu(player);
                    playerEvent.PrintToChat($" {ChatColors.Lime}{playerEvent.GetTranslation("bankrupt_player_info", enemy.PlayerName)}");

                    var enemyEvent = PlayerManager.GetPlayerFromEvent(enemy);
                    if (enemyEvent != null && enemyEvent.IsValid)
                        enemyEvent.PrintToChat($" {ChatColors.Red}{enemyEvent.GetTranslation("bankrupt_enemy_info")}");
                    return;
                }
            }
            playerEvent.PrintToChat($" {ChatColors.Red}{playerEvent.GetTranslation("selectplayerskill_incorrect_enemy_index")}");
        }

        private static void ResetMoney(CCSPlayerController enemy)
        {
            if (enemy == null || !enemy.IsValid) return;
            var enemyMoneyServices = enemy.InGameMoneyServices;
            if (enemyMoneyServices == null) return;

            enemyMoneyServices.Account = 0;
            Utilities.SetStateChanged(enemy, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player != null && player.IsValid)
                SkillUtils.CloseMenu(player);
        }
    }
}