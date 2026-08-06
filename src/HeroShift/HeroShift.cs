using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using src.command;
using src.Infrastructure.Menu;
using src.Infrastructure.Tracing;
using src.player;
using src.utils;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using static CounterStrikeSharp.API.Core.Listeners;

using src.SkillsCore;
using src.SkillsCore.Abstractions;
namespace src
{
    /*
     * HeroShift - the plugin entry point.
     *
     * WHAT THIS PLUGIN DOES
     * Every round each player is given a random "skill" (a hero power). The
     * skills themselves live in src/player/skills/ - one file per hero - and the
     * rest of the plugin is the machinery that hands them out and routes game
     * events into them.
     *
     * HOW A SKILL GETS CALLED
     * Every built-in skill is registered in BuiltInSkillCatalog with typed hook
     * delegates. SkillDispatcher routes game events, while explicit lifecycle
     * coordinator methods preserve assignment history, curse ownership and PerfLog.
     *
     * LOAD ORDER (Load method): typed heroshift.json snapshot -> embedded English /
     * optional language override -> event/tick listeners -> commands -> WASD menu ->
     * enabled skills -> player sync.
     */
    public partial class HeroShift : BasePlugin
    {
#pragma warning disable CS8618
        public static HeroShift Instance { get; private set; }
#pragma warning restore CS8618
        public IEnumerable<PlayerRuntimeState> SkillPlayer => PlayerManager.GetAllPlayers();
        public Random Random { get; } = new Random();
        public CCSGameRules? GameRules { get; set; }
        private ConcurrentBag<string> ManifestResources { get; set; } = ["models/sprays/spray_plane.vmdl"];
        private readonly CancellationTokenSource startupDiagnostics = new();
        internal IGameMenuService MenuService { get; private set; } = null!;
        internal ITraceService TraceService { get; private set; } = null!;
        internal SkillRegistry SkillRegistry { get; private set; } = null!;
        internal SkillDispatcher SkillDispatcher { get; private set; } = null!;
        // Skills enabled at least once this round; used to reset only those on round change, not all 146 definitions.
        public static readonly ConcurrentDictionary<SkillId, byte> ActiveSkillsThisRound = new();
        public static readonly ConcurrentDictionary<SkillId, byte> SkillsUsedThisMap = new();

        public override string ModuleName => "[CS2] [ HeroShift ]";
        public override string ModuleAuthor => "D3X (Original), Juzlus (Modifier), ByDexterTR (Contributor)";
        public override string ModuleDescription => "Plugin adds random skills every round for CS2 by D3X. Modified by Juzlus.";
        public override string ModuleVersion =>
            typeof(HeroShift).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion.Split('+')[0] ?? "1.0.0";

        public override void Load(bool hotReload)
        {
            Instance = this;

            SkillRegistry = BuiltInSkillCatalog.BuildRegistry();
            MenuService = new WasdGameMenuService();
            TraceService = new RayTraceService();
            SkillDispatcher = new SkillDispatcher(SkillRegistry, Server.PrintToConsole);
            src.LocalizationCore.LocalizationService? localization = null;
            ConfigurationStore.Initialize(
                Path.Combine(ModuleDirectory, "configs", "heroshift.json"),
                SkillRegistry,
                Logger,
                snapshot => localization = Localization.CreateService(snapshot.Configuration));
            Localization.UseService(localization!);
            Debug.Load();
            PlayerOnTick.Load();
            Event.Load();
            Command.Load();
            MenuService.Load(this, hotReload);
            LoadAllSkills();
            PlayerManager.SyncWithPlugin(Instance);

            Instance.RegisterListener<OnServerPrecacheResources>(LoadManifest);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3500, startupDiagnostics.Token);
                    await PrintInfoToConsoleAsync(startupDiagnostics.Token);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception) when (startupDiagnostics.IsCancellationRequested)
                {
                }
                catch (Exception ex) when (!startupDiagnostics.IsCancellationRequested)
                {
                    Server.PrintToConsole($"[HeroShift] startup diagnostics failed: {ex.Message}");
                }
            });
        }

        public override void Unload(bool hotReload)
        {
            src.player.PerfLog.Info("===== PLUGIN UNLOAD (clean shutdown/reload) =====");
            Debug.WriteToDebug("===== PLUGIN UNLOAD (clean shutdown/reload) =====");
            startupDiagnostics.Cancel();

            try { BotManager.Stop(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] bot timer cleanup failed: {ex.Message}"); }
            try { Command.Unload(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] command cleanup failed: {ex.Message}"); }
            try { MenuService?.Unload(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] menu cleanup failed: {ex.Message}"); }
            try { PlayerOnTick.Unload(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] tick cleanup failed: {ex.Message}"); }
            try { Event.Shutdown(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] runtime cleanup failed: {ex.Message}"); }
            try { Event.Unload(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] event cleanup failed: {ex.Message}"); }
            try { Debug.Unload(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] debug cleanup failed: {ex.Message}"); }
            DisposeManagedRegistrations();

            SkillData.Skills.Clear();
            SkillData.Invalidate();
            ActiveSkillsThisRound.Clear();
            SkillsUsedThisMap.Clear();
            PlayerManager.Clear();
            Localization.Unload();
            ConfigurationStore.Reset();
            GameRules = null;

            base.Unload(hotReload);
        }

        private void DisposeManagedRegistrations()
        {
            static void TryCleanup(Action cleanup, string kind)
            {
                try { cleanup(); }
                catch (Exception ex) { Server.PrintToConsole($"[HeroShift] {kind} cleanup failed: {ex.Message}"); }
            }

            foreach (var subscriber in Handlers.Values.ToArray())
                TryCleanup(subscriber.Dispose, "event handler");
            Handlers.Clear();

            foreach (var subscriber in Listeners.Values.ToArray())
                TryCleanup(subscriber.Dispose, "listener");
            Listeners.Clear();

            foreach (var subscriber in CommandListeners.Values.ToArray())
                TryCleanup(subscriber.Dispose, "command listener");
            CommandListeners.Clear();

            foreach (var subscriber in EntityOutputHooks.Values.ToArray())
                TryCleanup(subscriber.Dispose, "entity output");
            EntityOutputHooks.Clear();

            foreach (var command in CommandDefinitions.ToArray())
                TryCleanup(() => CommandManager.RemoveCommand(command), "command");
            CommandDefinitions.Clear();

            foreach (var timer in Timers.ToArray())
                TryCleanup(timer.Kill, "timer");
            Timers.Clear();
        }

        internal void AddToManifest(string prop)
        {
            if (!ManifestResources.Contains(prop))
                ManifestResources.Add(prop);
        }

        internal void LoadManifest(ResourceManifest manifest)
        {
            foreach (var prop in ManifestResources)
                manifest.AddResource(prop);
        }

        internal void LoadAllSkills()
        {
            foreach (var definition in SkillRegistry.All)
                if (SkillRuntime.GetMetadata(definition.Id).Active)
                    InvokeLoadSkill(definition.Id);

            Debug.WriteToDebug($"HeroShift v{Instance.ModuleVersion} ({SkillData.Skills.Count - 1}/{SkillRuntime.All.Count - 1} Skills) loaded!");
            Debug.WriteToDebug($"GameModes: {ConfigurationStore.Settings.General.GameMode}");
            foreach (var skill in SkillData.Skills)
                Debug.WriteToDebug($"Loaded: {skill.Skill}");
        }

        private void InvokeLifecycle(SkillId skill, string hookName, Action<SkillDefinition> invoke)
        {
            if (!SkillRegistry.TryGet(skill, out var definition)) return;

            if (!PerfLog.Enabled)
            {
                invoke(definition);
                return;
            }

            long perfStart = PerfLog.Start();
            invoke(definition);
            PerfLog.End($"SkillHook {skill}.{hookName}", perfStart, 2.0);
        }

        internal void InvokeLoadSkill(SkillId skill) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.LoadSkill), d => d.Hooks.LoadSkill?.Invoke());

        internal void InvokeEnableSkill(SkillId skill, CCSPlayerController player)
        {
            ActiveSkillsThisRound.TryAdd(skill, 0);
            SkillsUsedThisMap.TryAdd(skill, 0);
            InvokeLifecycle(skill, nameof(SkillHookSet.EnableSkill), d => d.Hooks.EnableSkill?.Invoke(player));
        }

        internal void InvokeDisableSkill(SkillId skill, CCSPlayerController player)
        {
            if (SkillUtils.CurseLimitEnabled && SkillUtils.IsCurseSkill(skill) && player.IsValid)
                SkillUtils.ReleaseCurse(player.Index);

            InvokeLifecycle(skill, nameof(SkillHookSet.DisableSkill), d => d.Hooks.DisableSkill?.Invoke(player));
        }

        internal void InvokeUseSkill(SkillId skill, CCSPlayerController player) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.UseSkill), d => d.Hooks.UseSkill?.Invoke(player));

        internal bool InvokeTypeSkill(SkillId skill, CCSPlayerController player, string[] arguments)
        {
            if (SkillUtils.CurseLimitEnabled && SkillUtils.IsCurseSkill(skill) &&
                !TryClaimCurseTarget([player, arguments]))
                return false;

            InvokeLifecycle(skill, nameof(SkillHookSet.TypeSkill), d => d.Hooks.TypeSkill?.Invoke(player, arguments));
            return true;
        }

        internal void InvokeNewRoundSkill(SkillId skill) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.NewRound), d => d.Hooks.NewRound?.Invoke());

        internal void InvokeRoundEndSkill(SkillId skill) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.RoundEnd), d => d.Hooks.RoundEnd?.Invoke());

        private static bool TryClaimCurseTarget(object[]? param)
        {
            if (param == null || param.Length < 2) return true;
            if (param[0] is not CCSPlayerController curser || !curser.IsValid) return true;
            if (param[1] is not string[] commands || commands.Length < 1) return true;
            if (!uint.TryParse(commands[0], out uint victimIndex)) return true;

            var victim = Utilities.GetPlayerFromIndex((int)victimIndex);
            if (victim == null || !victim.IsValid) return true;

            if (SkillUtils.TryClaimCurse(curser.Index, victimIndex)) return true;

            if (!SkillUtils.AnyCurseCapacity(curser))
                return SkillUtils.TryClaimCurse(curser.Index, victimIndex, true);

            var curserEvent = PlayerManager.GetPlayerFromEvent(curser);
            curserEvent?.PrintToChat($" {ChatColors.Red}{curserEvent.GetTranslation("curse_limit_info", victim.PlayerName)}");
            return false;
        }

        internal new void AddCommand(string name, string description, CommandInfo.CommandCallback handler)
        {
            var definition = new CounterStrikeSharp.API.Core.Commands.CommandDefinition(name, description, handler);
            CommandDefinitions.Add(definition);
            CommandManager.RegisterCommand(definition);
        }

        internal bool IsPlayerValid(CCSPlayerController? player)
        {
            return player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid && player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE;
        }

        // Hashes of CS2 sound events, used by the sound-based skills.
        // A skill compares the soundevent_hash from the PlayerMakeSound user
        // message against these lists, then clears um.Recipients to mute it.
        //   footstepSoundEvents - walking/running/landing sounds (used by Flash)
        //   silentSoundEvents   - the wider set muted by Silent
        public uint[] footstepSoundEvents = [3109879199, 70939233, 1342713723, 2722081556, 1909915699, 3193435079, 2300993891, 3847761506, 4084367249, 1342713723, 3847761506, 2026488395, 2745524735, 2684452812, 2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732, 2240518199, 2829617974, 1194677450, 1803111098, 3749333696, 29217150, 1692050905, 2207486967, 2633527058, 3342414459, 988265811, 540697918, 1763490157, 3755338324, 3161194970, 3753692454, 3166948458, 3997353267, 3161194970, 3753692454, 3166948458, 3997353267, 809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576, 737696412, 1403457606, 1770765328, 892882552, 3023174225, 4163677892, 3952104171, 4082928848, 1019414932, 1485322532, 1161855519, 1557420499, 1163426340, 809738584, 3368720745, 2708661994, 2479376962, 3295206520, 1404198078, 1194093029, 1253503839, 2189706910, 1218015996, 96240187, 1116700262, 84876002, 1598540856, 2231399653];
        public uint[] silentSoundEvents = [2551626319, 765706800, 765706800, 2860219006, 2162652424, 2551626319, 2162652424, 117596568, 117596568, 740474905, 1661204257, 3009312615, 1506215040, 115843229, 3299941720, 1016523349, 2684452812, 2067683805, 2067683805, 1016523349, 4160462271, 1543118744, 585390608, 3802757032, 2302139631, 2546391140, 144629619, 4152012084, 4113422219, 1627020521, 2899365092, 819435812, 3218103073, 961838155, 1535891875, 1826799645, 3460445620, 1818046345, 3666896632, 3099536373, 1440734007, 1409986305, 1939055066, 782454593, 4074593561, 1540837791, 3257325156];

        private static async Task PrintInfoToConsoleAsync(CancellationToken cancellationToken)
        {
            string? versionFromGithub = await GetLatestVersion(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // Top border
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Magenta;
            Console.WriteLine($"\n************************************************************************************************************\n");

            // ASCII tag
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.WriteLine("      || '||''|.                         '||                      .|'''.|  '||       ||  '||  '||         \r\n     ...  ||   ||   ....   .. ...      .. ||    ...   .. .. ..    ||..  '   ||  ..  ...   ||   ||   ....  \r\n      ||  ||''|'   '' .||   ||  ||   .'  '||  .|  '|.  || || ||    ''|||.   || .'    ||   ||   ||  ||. '  \r\n      ||  ||   |.  .|' ||   ||  ||   |.   ||  ||   ||  || || ||  .     '||  ||'|.    ||   ||   ||  . '|.. \r\n      || .||.  '|' '|..'|' .||. ||.  '|..'||.  '|..|' .|| || ||. |'....|'  .||. ||. .||. .||. .||. |'..|' \r\n   .. |'                                                                                                  \r\n    ''  ");

            // Version info
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write($"\nHeroShift ");

            if (versionFromGithub == null)
            {
                Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Yellow;
                Console.Write($"v{Instance.ModuleVersion} (failed to get version from github)");
            }
            else if (versionFromGithub == Instance.ModuleVersion)
            {
                Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Green;
                Console.Write($"v{Instance.ModuleVersion} (latest version)");
            }
            else
            {
                Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Red;
                Console.Write($"v{Instance.ModuleVersion} (new version {versionFromGithub} detected)");
            }

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.WriteLine($" ({SkillData.Skills.Count - 1}/{SkillRuntime.All.Count - 1} Skills) loaded!");

            if (versionFromGithub != null && versionFromGithub != Instance.ModuleVersion)
            {
                Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Red;
                Console.WriteLine($"\n#########################################################");
                Console.WriteLine($"# Download the new version from:                        #");
                Console.WriteLine($"# https://github.com/Juzlus/HeroShift/releases      #");
                Console.WriteLine($"#########################################################");
            }

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("\nConfiguration: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.WriteLine($"schema {ConfigurationStore.Settings.SchemaVersion}, {ConfigurationStore.EffectivePath}");

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("Translations: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.WriteLine(Localization.SourceDescription);

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("WASDMenu: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.WriteLine(Instance.MenuService.Status);

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("RayTrace capability: ");
            bool rayTraceAvailable = Instance.TraceService.IsAvailable;
            Console.ForegroundColor = rayTraceAvailable
                ? (ConsoleColor)CS2ConsoleColors.Green3
                : (ConsoleColor)CS2ConsoleColors.LightRed;
            Console.WriteLine(rayTraceAvailable ? "available" : "unavailable");

            // Main config info
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("\nGameMode: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.Write($"{ConfigurationStore.Settings.General.GameMode} ({(int)ConfigurationStore.Settings.General.GameMode})");

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write(", DebugMode: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.WriteLine(ConfigurationStore.Settings.General.DebugMode);

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("SkillHudDuration: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.Write(ConfigurationStore.Settings.General.SkillHudDuration == -1 ? "infinity" : ConfigurationStore.Settings.General.SkillHudDuration);

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write(", SkillButton: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.Write(ConfigurationStore.Settings.General.AlternativeSkillButton ?? "(NULL)");

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write(", HtmlHudFix: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.WriteLine(ConfigurationStore.Settings.General.EnableFlashingHtmlHudFix);

            // Dependences
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.WriteLine($"\nDependences:");
            var files = new Dictionary<string, string> {
                { "Newtonsoft Json", "./Newtonsoft.Json.dll" },
                { "WASDMenuAPI", "./WASDMenuAPI.dll" },
                { "RayTraceApi", "./../../shared/RayTraceApi/RayTraceApi.dll" },
                { "RayTraceImpl", "./../../plugins/RayTraceImpl/RayTraceImpl.dll" },
                { "RayTrace MetaMod", "./../../../metamod/RayTrace.vdf" },
                { "HeroShift gamedata", "./../../gamedata/HeroShift.gamedata.json" }
            };

            foreach (var fileInfo in files)
            {
                string fullPath = Path.GetFullPath(Path.Combine(Instance.ModuleDirectory, fileInfo.Value));
                if (File.Exists(fullPath))
                {
                    Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Green3;
                    Console.WriteLine($"- {fileInfo.Key} [OK]");
                }
                else
                {
                    Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightRed;
                    Console.WriteLine($"- {fileInfo.Key} (Missing: {fileInfo.Value})");
                }
            }

            // Skills info
            List<SkillId> enabled = [];
            List<SkillId> disabled = [];

            foreach (SkillId skill in BuiltInSkillIds.All)
                if (skill == BuiltInSkillIds.None)
                    continue;
                else if (SkillData.Skills.Any(s => s.Skill == skill))
                    enabled.Add(skill);
                else
                    disabled.Add(skill);

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.WriteLine($"\nEnabled skills ({enabled.Count}):");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Green;
            Console.WriteLine(" " + string.Join("\n ", enabled.Chunk(10).Select(group => string.Join(", ", group))));

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.WriteLine($"\nDisabled skills ({disabled.Count}):");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Red;
            Console.WriteLine(" " + string.Join("\n ", disabled.Chunk(10).Select(group => string.Join(", ", group))));

            // Bottom border
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Magenta;
            Console.WriteLine($"\n************************************************************************************************************\n");
            Console.ResetColor();
        }

        private static async Task<string?> GetLatestVersion(CancellationToken cancellationToken)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("HeroShift", "1.0"));
            const string URL = "https://api.github.com/repos/Juzlus/HeroShift/releases/latest";

            try
            {
                string response = await client.GetStringAsync(URL, cancellationToken);
                using JsonDocument doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("tag_name", out JsonElement value))
                    return value.GetString()?.Replace("v", "");
                return null;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        internal PlayerRuntimeState? GetPlayerInfoByIndex(uint playerIndex)
        {
            return PlayerManager.GetPlayerByIndex(playerIndex);
        }
    }

    public static class SkillData
    {
        public static ConcurrentBag<SkillRuntimeInfo> Skills { get; } = [];

        private static SkillRuntimeInfo[]? _snapshot;

        public static SkillRuntimeInfo[] GetSnapshot() => _snapshot ??= [.. Skills];

        private static Dictionary<SkillId, SkillRuntimeInfo>? _bySkill;

        public static SkillRuntimeInfo? GetInfo(SkillId skill)
        {
            var map = _bySkill;
            if (map == null)
            {
                map = new Dictionary<SkillId, SkillRuntimeInfo>();
                foreach (var s in Skills)
                    map[s.Skill] = s;
                _bySkill = map;
            }
            return map.TryGetValue(skill, out var info) ? info : null;
        }

        public static void Invalidate()
        {
            _bySkill = null;
            _snapshot = null;
        }
    }

    public enum CS2ConsoleColors
    {
        Black = ConsoleColor.Black,
        White = ConsoleColor.DarkBlue,
        Orange = ConsoleColor.DarkGreen,
        Yellow = ConsoleColor.DarkCyan,
        LightGreen = ConsoleColor.DarkRed,
        Green = ConsoleColor.DarkMagenta,
        Green2 = ConsoleColor.DarkYellow,
        Green3 = ConsoleColor.Gray,
        Cyan = ConsoleColor.DarkGray,
        LightBlue = ConsoleColor.Blue,
        Blue = ConsoleColor.Green,
        DarkPurple = ConsoleColor.Cyan,
        Purple = ConsoleColor.Red,
        Magenta = ConsoleColor.Magenta,
        LightRed = ConsoleColor.Yellow,
        Red = ConsoleColor.White,
    }
}
