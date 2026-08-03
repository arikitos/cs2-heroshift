using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using src.player;
using CounterStrikeSharp.API.Core;

namespace src.utils
{
    /*
     * PlayerManager - the player lookup layer every skill uses.
     *
     * Two things live here:
     *
     * 1. PER-TICK CACHES. GetTickPlayers() calls Utilities.GetPlayers() at most
     *    once per server tick and reuses the list for the rest of that tick
     *    (same for GetTickBomb). Skills run in OnTick 64 times a second, so
     *    ALWAYS prefer GetTickPlayers() over Utilities.GetPlayers() inside a
     *    hook - it is the difference between one engine call per tick and one
     *    per skill per tick.
     *
     * 2. SKILL STATE LOOKUP. GetPlayerByIndex(index) returns the
     *    jSkill_PlayerInfo holding that player's current hero and per-round
     *    state. This is the standard first line of nearly every hook:
     *        var playerInfo = PlayerManager.GetPlayerByIndex(player.Index);
     *        if (playerInfo?.Skill != skillName) return;
     *
     * BOTS AND BOT-TAKEOVER (important, and easy to get wrong):
     *   GetPlayerEvent(player)     - given the controller from a game event,
     *       returns the BOT controller actually holding the pawn when a human
     *       has taken over a bot. Use this to act on the pawn in the world.
     *   GetPlayerFromEvent(player) - the inverse: returns the HUMAN controller
     *       behind a bot. Use this when printing chat/HUD to a real person.
     *   Mixing these two up is why a message goes to nobody, or an effect is
     *   applied to the wrong body.
     */
    public static class PlayerManager
    {
        private static readonly ConcurrentDictionary<uint, jSkill_PlayerInfo> playersByIndex = [];

        private static int cachedTick = int.MinValue;
        private static List<CCSPlayerController> cachedControllers = [];

        private static int cachedBombTick = int.MinValue;
        private static CC4? cachedBomb;

        private static void EnsureTickCache()
        {
            int tick = Server.TickCount;
            if (tick == cachedTick) return;
            cachedTick = tick;
            cachedControllers = Utilities.GetPlayers();
        }

        public static List<CCSPlayerController> GetTickPlayers()
        {
            EnsureTickCache();
            return cachedControllers;
        }

        public static CC4? GetTickBomb()
        {
            int tick = Server.TickCount;
            if (tick != cachedBombTick)
            {
                cachedBombTick = tick;
                cachedBomb = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").FirstOrDefault();
            }

            return cachedBomb != null && cachedBomb.IsValid ? cachedBomb : null;
        }

        public static void Register(jSkill_PlayerInfo playerInfo)
        {
            if (playerInfo == null) return;
            playersByIndex[playerInfo.PlayerIndex] = playerInfo;
        }

        public static void UnregisterPlayer(uint playerIndex)
        {
            playersByIndex.TryRemove(playerIndex, out _);
        }

        public static jSkill_PlayerInfo? GetPlayerByIndex(uint? playerIndex)
        {
            if (playerIndex == null) return null;

            playersByIndex.TryGetValue((uint)playerIndex, out var playerInfo);
            return playerInfo;
        }

        public static CCSPlayerController? GetPlayerEvent(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid)
                return null;

            if (!player.ControllingBot)
                return player;

            return GetTickPlayers().FirstOrDefault(p =>
                p != null &&
                p.IsValid &&
                p.IsBot &&
                p.OriginalControllerOfCurrentPawn.Value != null && p.OriginalControllerOfCurrentPawn.Value == player)
                ?? player;
        }

        public static CCSPlayerController? GetPlayerFromEvent(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid)
                return null;

            if (!player.IsBot)
                return player;

            return GetTickPlayers().FirstOrDefault(p =>
                p != null &&
                p.IsValid &&
                !p.IsBot &&
                player.OriginalControllerOfCurrentPawn.Value != null && player.OriginalControllerOfCurrentPawn.Value == p)
                ?? player;
        }

        public static IEnumerable<jSkill_PlayerInfo> GetAllPlayers()
        {
            return playersByIndex.Values;
        }

        public static int GetPlayerCountBySkill(Skills skills)
        {
            return playersByIndex.Values.Count(p => p.Skill == skills);
        }

        public static void Clear()
        {
            playersByIndex.Clear();
        }

        public static void SyncWithPlugin(HeroShift instance)
        {
            if (instance?.SkillPlayer == null) return;

            foreach (var player in instance.SkillPlayer)
                Register(player);
        }
    }
}
