using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;
using System.Collections.Concurrent;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Poison - Curse: the victim is poisoned and loses health over time.
     *
     * LOGIC
     *   TypeSkill: pick the victim.
     *   OnTick: every 'cooldown' seconds removes 'damage' health, but never below
     *     minHealth.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   cooldown  = .85f
     *                 -> seconds between each poison tick
     *   damage    = 1
     *                 -> health lost per poison tick
     *   minHealth = 30
     *                 -> poison stops at this health value (so it cannot kill)
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.None
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = 2
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class Poison : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Poison;
        private static PoisonOptions Options => SkillConfigurationResolver.Get<PoisonOptions>(BuiltInSkillIds.Poison);
        private static readonly ConcurrentDictionary<uint, byte> poisonedPlayers = [];
        private static readonly ConcurrentDictionary<uint, uint> playersToTarget = [];
        private static readonly ConcurrentDictionary<uint, uint> targetToPlayer = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void PlayerDisconnect(uint playerIndex)
        {
            lock (setLock)
            {
                poisonedPlayers.TryRemove(playerIndex, out _);
                targetToPlayer.TryRemove(playerIndex, out _);

                if (playersToTarget.TryRemove(playerIndex, out uint ownTarget))
                    targetToPlayer.TryRemove(ownTarget, out _);

                foreach (var kvp in playersToTarget)
                    if (kvp.Value == playerIndex)
                        playersToTarget.TryRemove(kvp.Key, out _);
            }
        }

        public static void NewRound()
        {
            lock (setLock)
            {
                poisonedPlayers.Clear();
                playersToTarget.Clear();
                targetToPlayer.Clear();
            }
        }

        public static void OnTick()
        {
            int cooldown = Math.Max(1, (int)(64 * Options.Cooldown));

            if (Server.TickCount % cooldown == 0)
            {
                int cooldown2 = cooldown * 2;

                foreach (var playerIndex in poisonedPlayers.Keys)
                {
                    var player = Utilities.GetPlayerFromIndex((int)playerIndex);
                    if (player == null || !player.IsValid || player.PlayerPawn == null) continue;

                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid) continue;

                    if (Jester.IsActiveJester(playerIndex)) continue;

                    if (pawn.Health <= Options.MinHealth) continue;

                    SkillUtils.TakeHealth(pawn, Options.Damage, GetSkillOwner(playerIndex), KillfeedIcons.Spray);

                    if (Server.TickCount % cooldown2 == 0)
                        PlayerManager.GetPlayerFromEvent(player)?.ExecuteClientCommand($"play player/player_damagebody_0{HeroShift.Instance.Random.Next(4, 8)}");
                }
            }

            if (Server.TickCount % 32 != 0) return;
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);

                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                if (!SkillUtils.HasMenu(player)) continue;

                var enemies = SkillUtils.GetSelectableEnemies(player, true);

                ConcurrentBag<(string, string)> menuItems = new(enemies.Select(e => (e.PlayerName, e.Index.ToString())));
                SkillUtils.UpdateMenu(player, menuItems);
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            var playerEvent = PlayerManager.GetPlayerFromEvent(player);
            if (playerEvent == null || !playerEvent.IsValid) return;

            if (playerInfo.SkillUsed)
            {
                playerEvent.PrintToChat($" {ChatColors.Red}{playerEvent.GetTranslation("areareaper_used_info")}");
                return;
            }

            string enemyId = commands[0];

            if (!uint.TryParse(enemyId, out uint enemyIndex))
            {
                playerEvent.PrintToChat($" {ChatColors.Red}" + playerEvent.GetTranslation("selectplayerskill_incorrect_enemy_index"));
                return;
            }

            var enemy = Utilities.GetPlayerFromIndex((int)enemyIndex);

            if (enemy == null)
            {
                playerEvent.PrintToChat($" {ChatColors.Red}" + playerEvent.GetTranslation("selectplayerskill_incorrect_enemy_index"));
                return;
            }

            poisonedPlayers.TryAdd(enemy.Index, 0);
            playersToTarget[player.Index] = enemy.Index;
            targetToPlayer[enemy.Index] = player.Index;
            playerInfo.SkillUsed = true;

            var enemyEvent = PlayerManager.GetPlayerFromEvent(enemy);
            if (enemyEvent == null || !enemyEvent.IsValid) return;

            playerEvent.PrintToChat($" {ChatColors.Green}" + playerEvent.GetTranslation("poison_player_info", enemy.PlayerName));
            enemyEvent.PrintToChat($" {ChatColors.Red}" + enemyEvent.GetTranslation("poison_enemy_info"));
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;
            playerInfo.SkillUsed = false;

            var playerEvent = PlayerManager.GetPlayerFromEvent(player);
            if (playerEvent == null || !playerEvent.IsValid) return;

            var enemies = SkillUtils.GetSelectableEnemies(player, true);
            if (enemies.Length > 0)
            {
                ConcurrentBag<(string, string)> menuItems = new(enemies.Select(e => (e.PlayerName, e.Index.ToString())));
                SkillUtils.CreateMenu(player, menuItems);
            }
            else
                playerEvent.PrintToChat($" {ChatColors.Red}{player.GetTranslation("selectplayerskill_incorrect_enemy_index")}");
        }

        private static CCSPlayerController? GetSkillOwner(uint targetIndex)
        {
            if (!targetToPlayer.TryGetValue(targetIndex, out uint ownerIndex)) return null;

            var owner = Utilities.GetPlayerFromIndex((int)ownerIndex);
            return owner != null && owner.IsValid ? owner : null;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            targetToPlayer.TryRemove(player.Index, out _);

            if (playersToTarget.TryRemove(player.Index, out uint targetIndex))
            {
                poisonedPlayers.TryRemove(targetIndex, out _);
                targetToPlayer.TryRemove(targetIndex, out _);

                var target = PlayerManager.GetPlayerFromEvent(Utilities.GetPlayerFromIndex((int)targetIndex));
                if (target != null && target.IsValid && target.PawnIsAlive && !SkillUtils.IsFreezeTime())
                    target.PrintToChat($" {ChatColors.Green}" + target.GetTranslation("poison_disable_info"));
            }

            SkillUtils.CloseMenu(player);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;

            poisonedPlayers.TryRemove(player.Index, out _);
            targetToPlayer.TryRemove(player.Index, out _);

            SkillUtils.CloseMenu(player);
        }
    }
}
