using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace src.utils
{
    /*
     * BotManager - makes BOTS use their hero, and provides a bot-churn stress test.
     *
     * Note the file lives in src/player/ but declares namespace src.utils.
     *
     * Bots receive a hero like anyone else (Event.OnPlayerConnectedBot registers them
     * whenever EnableBotSkills is on), but nothing ever presses their ability key -
     * Event.CheckUseSkill only reacts to real button input. This class supplies that
     * missing trigger from a timer instead.
     *
     * TWO TIMERS, both opt-in and both STOP_ON_MAPCHANGE:
     *   _skillTimer    (every 2s, requires EnableBotSkills)
     *       Picks ONE random living bot and fires its ability. Deliberately one bot per
     *       interval rather than all of them, so a server full of bots does not trigger
     *       a dozen abilities on the same frame.
     *   _rotationTimer (every 45s, requires EnableBotKickDebug)
     *       DEBUG ONLY. Kicks a random bot and adds a fresh one, which forces controller
     *       indices to be recycled. That is exactly the situation where stale per-index
     *       hero state shows up as a bug, so this exercises connect/disconnect cleanup.
     *
     * LIFECYCLE
     *   Initialize() runs from PlayerOnTick's OnMapStart, Stop() from OnMapEnd.
     *   Initialize() calls Stop() first so a re-init cannot leave an orphan timer
     *   running alongside the new one.
     *
     * Stop() always kills both owned timers, including after a hot reload turns a
     * feature off.
     */
    public static class BotManager
    {
        private static Timer? _skillTimer;
        private static Timer? _rotationTimer;
        private static readonly Random _random = new();

        // Seconds between bot ability attempts / between bot kick-and-readd cycles.
        private const float SkillInterval = 2f;
        private const float RotationInterval = 45f;

        // Starts the repeating timers. Called on every map start.
        public static void Initialize()
        {
            if (!ConfigurationStore.Settings.General.EnableBotSkills) return;

            // Stop() first: a second Initialize() (map change, plugin reload) would
            // otherwise leave the previous timer running and double the bot activity.
            Stop();
            _skillTimer = Instance.AddTimer(SkillInterval, OnBotUseSkillTimer, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

            if (!ConfigurationStore.Settings.General.EnableBotKickDebug) return;

            _rotationTimer = Instance.AddTimer(RotationInterval, OnBotRotationTimer, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Kills both timers. Called on map end and at the top of Initialize().
        public static void Stop()
        {
            _skillTimer?.Kill();
            _skillTimer = null;
            _rotationTimer?.Kill();
            _rotationTimer = null;
        }

        // Fires one random bot's ability, mirroring what Event.CheckUseSkill does for a
        // human key press: same UseSkill entry point, same skill-state lookup.
        // Uses Utilities.GetPlayers() rather than PlayerManager.GetTickPlayers() because
        // this runs on a 2s timer, not in a tick path, so the per-tick cache would be
        // stale and offers nothing here.
        private static void OnBotUseSkillTimer()
        {
            var activeBots = Utilities.GetPlayers()
                .Where(p => p != null && p.IsValid && p.IsBot && !p.IsHLTV && p.LifeState == (byte)LifeState_t.LIFE_ALIVE && p.Team != CsTeam.Spectator)
                .ToList();

            if (activeBots.Count == 0) return;

            var randomBot = activeBots[_random.Next(activeBots.Count)];
            if (randomBot == null || !randomBot.IsValid) return;
            // BOT-TAKEOVER GUARD. When a human has taken over this bot, the pawn's
            // OriginalControllerOfCurrentPawn no longer points back at the bot itself.
            // Skipping that case leaves the ability to the human's own key press, so the
            // timer cannot fire it behind their back.
            if (randomBot.Index != randomBot.OriginalControllerOfCurrentPawn.Value?.Index) return;
            // Do not interrupt a defuse, matching the +use check in Event.CheckUseSkill.
            if (randomBot.PlayerPawn?.Value == null || !randomBot.PlayerPawn.Value.IsValid || randomBot.PlayerPawn.Value.IsDefusing) return;

            var bot_info = PlayerManager.GetPlayerByIndex(randomBot.Index);
            if (bot_info == null) return;

            // Invoke the active typed skill definition. Passive skills simply have no UseSkill hook.
            Instance.InvokeUseSkill(bot_info.Skill, randomBot);
        }

        // DEBUG churn: kick one bot, add one back. The point is to keep recycling
        // controller indices so any hero holding stale per-index state is exposed.
        private static void OnBotRotationTimer()
        {
            var allBots = Utilities.GetPlayers()
                .Where(p => p != null && p.IsValid && p.IsBot && !p.IsHLTV)
                .ToList();

            if (allBots.Count > 0)
            {
                var botToKick = allBots[_random.Next(allBots.Count)];
                Server.ExecuteCommand($"kickid {botToKick.UserId}");
            }

            // Deferred to the next frame: kickid is processed asynchronously, so adding a
            // bot in the same frame can race the kick and leave the bot count drifting.
            Server.NextFrame(() => Server.ExecuteCommand("bot_add"));
        }
    }
}
