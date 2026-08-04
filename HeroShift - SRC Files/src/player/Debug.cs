using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using static CounterStrikeSharp.API.Core.Listeners;
using static src.HeroShift;

namespace src.player
{
    /*
     * Debug - the plugin's debug log, gated on ConfigurationStore.Settings.General.DebugMode.
     *
     * Writes to <plugin>/logs/debug_<session timestamp>.txt. Load() picks a fresh
     * sessionId per load, so every map load / !reload starts a new file, and the
     * StreamWriter is opened lazily on the first line with AutoFlush so a crash
     * still leaves the log on disk.
     *
     * DebugMode == false means Load() registers nothing at all and
     * WriteToDebug() returns immediately, so the cost when disabled is one bool
     * check per call. Because of that early return, a hero can call
     * Debug.WriteToDebug("...") freely from any hook - that is the intended way to
     * trace skill behaviour instead of Console.WriteLine.
     *
     * Beyond the log helper, Load() subscribes to the interesting game events
     * (connect/disconnect, round start/freeze end/end, deaths, bomb plant/defuse,
     * map change, shots) and hooks CBaseEntity::TakeDamage in Pre mode so that
     * every damage event is logged with the attacker's and victim's current hero.
     *
     * Two things here exist specifically to debug known CS2 pitfalls:
     *   - The hitgroup cross-check: the native CTakeDamageInfo hitgroup is
     *     recorded in OnTakeDamage and compared against the hitgroup reported by
     *     the later EventPlayerHurt. A mismatch is logged as [HITGROUP], which is
     *     how head/leg-based heroes are verified against what the engine reports.
     *   - DescribeIdentitySplit: when a human is controlling a bot, the pawn taking
     *     the damage and the controller owning the skill are different entity
     *     indexes. Any such split is appended as SPLIT(...) with both indexes and
     *     both skills, since that mismatch is a common source of "my skill did
     *     nothing" reports.
     *
     * Unload() removes the TakeDamage hook and disposes the writer; the unhook is
     * wrapped in an empty catch so unloading still succeeds if the hook was never
     * installed (DebugMode off) or was already removed.
     */
    public static class Debug
    {
        private static string sessionId = "00000";
        private static readonly string debugFolder = Path.Combine(Instance.ModuleDirectory, "logs");
        private static StreamWriter? _writer;
        private static readonly object _writeLock = new();

        // Starts a new log session and, only when DebugMode is on, registers all the
        // event handlers and the TakeDamage hook below.
        public static void Load()
        {
            sessionId = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            // Drop any writer from a previous session so the next line opens the new file.
            lock (_writeLock) { _writer?.Dispose(); _writer = null; }

            if (ConfigurationStore.Settings.General.DebugMode != true)
                return;

            Instance.RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                var player = PlayerManager.GetPlayerEvent(@event.Userid);
                if (player == null || !player.IsValid) return HookResult.Continue;
                WriteToDebug($"{(player.IsBot ? "Bot" : "Player")} {player.PlayerName} joined the game.");
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                var player = PlayerManager.GetPlayerEvent(@event.Userid);
                if (player == null || !player.IsValid) return HookResult.Continue;
                WriteToDebug($"{(player.IsBot ? "Bot" : "Player")} {player.PlayerName} disconnected.");
                return HookResult.Continue;
            });

            // Round number is derived from the two team scores rather than read from a
            // counter: the cs_team_manager entities hold the per-team score, and the
            // round about to start is their sum + 1.
            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").Where(t => t != null).ToList();
                var tTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.Terrorist);
                var ctTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.CounterTerrorist);
                WriteToDebug($"Round #{tTeam?.Score + ctTeam?.Score + 1} (CT {ctTeam?.Score} : {tTeam?.Score} TT) started.");
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                WriteToDebug($"Freeze time ended.");
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").Where(t => t != null).ToList();
                var tTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.Terrorist);
                var ctTeam = teams.FirstOrDefault(t => t.TeamNum == (int)CsTeam.CounterTerrorist);
                WriteToDebug($"Round #{tTeam?.Score + ctTeam?.Score} (CT {ctTeam?.Score} : {tTeam?.Score} TT) ended.");
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var victim = PlayerManager.GetPlayerEvent(@event.Userid);
                var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
                if (victim != null)
                {
                    if (attacker != null)
                        WriteToDebug($"{(victim.IsBot ? "Bot" : "Player")} {victim.PlayerName} died from {(attacker.IsBot ? "bot" : "player")} {attacker.PlayerName}.");
                    else
                        WriteToDebug($"{(victim.IsBot ? "Bot" : "Player")} {victim.PlayerName} died.");
                }
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventBombPlanted>((@event, info) =>
            {
                WriteToDebug($"Bomb planted.");
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventBombDefused>((@event, info) =>
            {
                WriteToDebug($"Bomb defused.");
                return HookResult.Continue;
            });

            Instance.RegisterListener<OnMapStart>((mapName) =>
            {
                WriteToDebug($"Map changed: {mapName}.");
            });

            Instance.RegisterEventHandler<EventPlayerShoot>((@event, info) =>
            {
                var player = PlayerManager.GetPlayerEvent(@event.Userid);
                if (player == null || !player.IsValid) return HookResult.Continue;
                WriteToDebug($"{(player.IsBot ? "Bot" : "Player")} {player.PlayerName} fired a shot.");
                return HookResult.Continue;
            });

            // Hitgroup cross-check. OnTakeDamage recorded the hitgroup the engine put in
            // CTakeDamageInfo; here we compare it with the hitgroup the player_hurt
            // event reports and log only when the two disagree. TryRemove consumes the
            // stored value so each damage instance is checked at most once.
            Instance.RegisterEventHandler<EventPlayerHurt>((@event, info) =>
            {
                var victim = PlayerManager.GetPlayerEvent(@event.Userid);
                if (victim == null || !victim.IsValid) return HookResult.Continue;

                if (!lastNativeHitGroup.TryRemove(victim.Index, out var native)) return HookResult.Continue;
                if (native.HitGroup == @event.Hitgroup) return HookResult.Continue;

                WriteToDebug($"[HITGROUP] mismatch on {victim.PlayerName}: native={(HitGroup_t)native.HitGroup} event={(HitGroup_t)@event.Hitgroup} " +
                    $"weapon={@event.Weapon} rawDmg={native.Damage:0.#} appliedDmg={@event.DmgHealth}");

                return HookResult.Continue;
            });

            // Pre mode: runs before the damage is applied, so the values logged are the
            // requested damage and the victim's health *before* the hit.
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        }

        // Victim entity index -> the hitgroup and raw damage seen natively, waiting to
        // be compared against the matching player_hurt event.
        private static readonly ConcurrentDictionary<uint, (int HitGroup, float Damage)> lastNativeHitGroup = [];

        // Removes the native hook and closes the log file.
        public static void Unload()
        {
            try { VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre); }
            catch { }

            lock (_writeLock) { _writer?.Dispose(); _writer = null; }
        }

        // Returns a SPLIT(...) fragment when the victim's own controller index differs
        // from the index PlayerManager routes to (human controlling a bot). Empty string
        // when there is no split, so it can be appended unconditionally.
        private static string DescribeIdentitySplit(CCSPlayerController victim)
        {
            var routed = PlayerManager.GetPlayerEvent(victim);
            uint routedIndex = routed?.Index ?? victim.Index;
            if (routedIndex == victim.Index) return string.Empty;

            return $" SPLIT(idx {victim.Index}->{routedIndex}, skill {PlayerManager.GetPlayerByIndex(victim.Index)?.Skill}" +
                $"->{PlayerManager.GetPlayerByIndex(routedIndex)?.Skill}, controllingBot={victim.ControllingBot})";
        }

        // Logs one line per player-vs-player damage instance. Always returns Continue -
        // this hook only observes, it never blocks or modifies damage (SkillUtils and
        // the individual heroes do that).
        private static HookResult OnTakeDamage(DynamicHook h)
        {
            // TakeDamage(CBaseEntity* victim, CTakeDamageInfo* info): param 0 is the
            // entity being hurt, param 1 carries attacker, damage and hitgroup.
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(param.Handle);

            // The hook fires for every entity in the world (props, chickens, breakables),
            // so anything that is not a player pawn on both sides is ignored here.
            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = PlayerManager.GetPlayerEvent(attackerPawn.Controller.Value.As<CCSPlayerController>())!;
            CCSPlayerController victim = PlayerManager.GetPlayerEvent(victimPawn.Controller.Value.As<CCSPlayerController>())!;

            var playerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
            if (playerInfo == null) return HookResult.Continue;

            // Stashed for the EventPlayerHurt comparison registered in Load().
            var nativeHitGroup = SkillUtils.GetHitGroup(param2);
            lastNativeHitGroup[victim.Index] = ((int)nativeHitGroup, param2.Damage);

            WriteToDebug($"{(victim.IsBot ? "Bot" : "Player")} {victim.PlayerName} took damage from {(attacker.IsBot ? "bot" : "player")} {attacker.PlayerName}. " +
                $"[dmg={param2.Damage:0.#} hp={victimPawn.Health}/{victimPawn.MaxHealth} armor={victimPawn.ArmorValue} hitgroup={nativeHitGroup} " +
                $"takes={victimPawn.TakesDamage} vskill={PlayerManager.GetPlayerByIndex(victim.Index)?.Skill} askill={playerInfo.Skill}" +
                $"{DescribeIdentitySplit(victim)}]");
            return HookResult.Continue;
        }

        // The logging entry point for the whole plugin. No-op unless DebugMode is set,
        // so heroes can call it unconditionally. Thread-safe via _writeLock, which
        // matters because tick hooks and event handlers can both reach it.
        public static void WriteToDebug(string message)
        {
            if (ConfigurationStore.Settings.General.DebugMode != true)
                return;

            lock (_writeLock)
            {
                _writer ??= CreateWriter();
                _writer?.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {message}");
            }
        }

        // Opens logs/debug_<sessionId>.txt with AutoFlush so nothing is lost on a hard
        // server crash. Returns null on any IO failure, which makes logging silently
        // inert rather than throwing inside a game hook.
        private static StreamWriter? CreateWriter()
        {
            try
            {
                Directory.CreateDirectory(debugFolder);
                string path = Path.Combine(debugFolder, $"debug_{sessionId}.txt");
                return new StreamWriter(path, append: true, System.Text.Encoding.UTF8) { AutoFlush = true };
            }
            catch
            {
                return null;
            }
        }

        // Dumps every valid entity's DesignerName and index to the console and appends
        // it to the debug file. Currently has no callers - it is kept as a manual probe
        // for tracking down entities the plugin leaked. Note it writes with
        // File.AppendAllText and no newline, bypassing the shared _writer.
        private static void GetAllEntityIndexes()
        {
            if (Instance.GameRules == null) return;

            var entities = Utilities.GetAllEntities();

            foreach (var entity in entities)
                if (entity != null && entity.IsValid && !string.IsNullOrEmpty(entity.DesignerName))
                {
                    string text = $"Entity: {entity.DesignerName}, ID: {entity.Index}";
                    Console.WriteLine(text);

                    string filename = $"debug_{sessionId}.txt";
                    string path = Path.Combine(debugFolder, filename);

                    Directory.CreateDirectory(debugFolder);
                    File.AppendAllText(path, text, System.Text.Encoding.UTF8);
                }
        }
    }
}