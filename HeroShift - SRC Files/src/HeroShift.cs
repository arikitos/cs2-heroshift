using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using src.command;
using src.player;
using src.utils;
using System.Collections.Concurrent;
using System.Text.Json;
using WASDSharedAPI;
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
     * delegates. The compatibility SkillAction entry point below resolves those
     * delegates through SkillRegistry while call sites are migrated incrementally.
     *
     * LOAD ORDER (Load method): config -> skill tunables -> translations ->
     * event/tick listeners -> commands -> WASD menu -> all skills -> player sync.
     *
     * CONFIG FILES this reads (both in the plugin's configs/ folder):
     *   settings.json    - global plugin behaviour (see utils/Config.cs)
     *   skillsInfo.json  - per-hero tunables (see utils/SkillsInfo.cs)
     */
    public partial class HeroShift : BasePlugin
    {
#pragma warning disable CS8618
        public static HeroShift Instance { get; private set; }
#pragma warning restore CS8618
        public IEnumerable<jSkill_PlayerInfo> SkillPlayer => PlayerManager.GetAllPlayers();
        public Random Random { get; } = new Random();
        public CCSGameRules? GameRules { get; set; }
        private ConcurrentBag<string> ManifestResources { get; set; } = ["models/sprays/spray_plane.vmdl"];
        public IWasdMenuManager? MenuManager;
        internal SkillRegistry SkillRegistry { get; private set; } = null!;
        internal SkillDispatcher SkillDispatcher { get; private set; } = null!;
        // Skills that were enabled at least once this round; used to reset only those on round change (not all 124).
        public static readonly ConcurrentDictionary<string, byte> ActiveSkillsThisRound = new();
        public static readonly ConcurrentDictionary<string, byte> SkillsUsedThisMap = new();

        public override string ModuleName => "[CS2] [ HeroShift ]";
        public override string ModuleAuthor => "D3X (Original), Juzlus (Modifier), ByDexterTR (Contributor)";
        public override string ModuleDescription => "Plugin adds random skills every round for CS2 by D3X. Modified by Juzlus.";
        public override string ModuleVersion => "1.0.0";

        public override void Load(bool hotReload)
        {
            Instance = this;

            SkillRegistry = BuiltInSkillCatalog.BuildRegistry();
            SkillDispatcher = new SkillDispatcher(SkillRegistry, Server.PrintToConsole);
            Config.Initialize(SkillRegistry);
            Localization.Load();
            Debug.Load();
            PlayerOnTick.Load();
            Event.Load();
            Command.Load();
            WASDMenuAPI.WASDMenuAPI.LoadPlugin(Instance, hotReload);
            LoadAllSkills();
            PlayerManager.SyncWithPlugin(Instance);

            Instance.RegisterListener<OnServerPrecacheResources>(LoadManifest);

            Task.Run(async () =>
            {
                await Task.Delay(3500);
                PrintInfoToConsole();
            });
        }

        public override void Unload(bool hotReload)
        {
            src.player.PerfLog.Info("===== PLUGIN UNLOAD (clean shutdown/reload) =====");
            Debug.WriteToDebug("===== PLUGIN UNLOAD (clean shutdown/reload) =====");

            Event.Unload();
            Debug.Unload();

            base.Unload(hotReload);
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
            foreach (var skill in Enum.GetValues<Skills>())
                if (SkillRuntime.GetMetadata(skill).Active)
                    SkillAction(skill.ToString()!, "LoadSkill");

            Debug.WriteToDebug($"HeroShift v{Instance.ModuleVersion} ({SkillData.Skills.Count - 1}/{SkillRuntime.All.Count - 1} Skills) loaded!");
            Debug.WriteToDebug($"GameModes: {(Config.GameModes)Config.LoadedConfig.GameMode}");
            foreach (var skill in SkillData.Skills)
                Debug.WriteToDebug($"Loaded: {skill.Skill}");
        }

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

        /*
         * Temporary compatibility entry point while event call sites move to the
         * typed dispatcher. Hook resolution is now entirely registry-based: no
         * class-name construction, assembly scanning or MethodInfo invocation.
         */
        internal object? SkillAction(string skill, string methodName, object[]? param = null)
        {
            if (string.IsNullOrWhiteSpace(skill) ||
                !Enum.TryParse<Skills>(skill, ignoreCase: true, out var legacySkill))
                return null;

            if (!SkillRegistry.TryGet(SkillRuntime.GetId(legacySkill), out var definition))
                return null;

            if (methodName == nameof(SkillHookSet.EnableSkill))
            {
                ActiveSkillsThisRound.TryAdd(skill, 0);
                SkillsUsedThisMap.TryAdd(skill, 0);
            }

            if (SkillUtils.CurseLimitEnabled && SkillUtils.IsCurseSkill(skill))
            {
                if (methodName == nameof(SkillHookSet.DisableSkill) && param?.Length > 0 && param[0] is CCSPlayerController curser && curser.IsValid)
                    SkillUtils.ReleaseCurse(curser.Index);

                if (methodName == nameof(SkillHookSet.TypeSkill) && !TryClaimCurseTarget(param))
                    return null;
            }

            object? InvokeTyped() => methodName switch
            {
                nameof(SkillHookSet.LoadSkill) => Invoke(definition.Hooks.LoadSkill),
                nameof(SkillHookSet.EnableSkill) => Invoke(definition.Hooks.EnableSkill, Arg<CCSPlayerController>(param, 0)),
                nameof(SkillHookSet.DisableSkill) => Invoke(definition.Hooks.DisableSkill, Arg<CCSPlayerController>(param, 0)),
                nameof(SkillHookSet.UseSkill) => Invoke(definition.Hooks.UseSkill, Arg<CCSPlayerController>(param, 0)),
                nameof(SkillHookSet.TypeSkill) => Invoke(definition.Hooks.TypeSkill, Arg<CCSPlayerController>(param, 0), Arg<string[]>(param, 1)),
                nameof(SkillHookSet.OnTakeDamage) => Invoke(definition.Hooks.OnTakeDamage, Arg<CounterStrikeSharp.API.Modules.Memory.DynamicFunctions.DynamicHook>(param, 0)),
                nameof(SkillHookSet.OnTakeDamagePost) => Invoke(definition.Hooks.OnTakeDamagePost, Arg<CounterStrikeSharp.API.Modules.Memory.DynamicFunctions.DynamicHook>(param, 0)),
                nameof(SkillHookSet.OnEntitySpawned) => Invoke(definition.Hooks.OnEntitySpawned, Arg<CEntityInstance>(param, 0)),
                nameof(SkillHookSet.OnTick) => Invoke(definition.Hooks.OnTick),
                nameof(SkillHookSet.CheckTransmit) => Invoke(definition.Hooks.CheckTransmit, Arg<CCheckTransmitInfoList>(param, 0)),
                nameof(SkillHookSet.NewRound) => Invoke(definition.Hooks.NewRound),
                nameof(SkillHookSet.RoundEnd) => Invoke(definition.Hooks.RoundEnd),
                nameof(SkillHookSet.PlayerMakeSound) => Invoke(definition.Hooks.PlayerMakeSound, Arg<CounterStrikeSharp.API.Modules.UserMessages.UserMessage>(param, 0)),
                nameof(SkillHookSet.PlayerBlind) => Invoke(definition.Hooks.PlayerBlind, Arg<EventPlayerBlind>(param, 0)),
                nameof(SkillHookSet.PlayerHurt) => Invoke(definition.Hooks.PlayerHurt, Arg<EventPlayerHurt>(param, 0)),
                nameof(SkillHookSet.PlayerHurtPre) => Invoke(definition.Hooks.PlayerHurtPre, Arg<EventPlayerHurt>(param, 0)),
                nameof(SkillHookSet.PlayerDeath) => Invoke(definition.Hooks.PlayerDeath, Arg<EventPlayerDeath>(param, 0)),
                nameof(SkillHookSet.PlayerJump) => Invoke(definition.Hooks.PlayerJump, Arg<EventPlayerJump>(param, 0)),
                nameof(SkillHookSet.SwitchTeam) => Invoke(definition.Hooks.SwitchTeam, Arg<EventSwitchTeam>(param, 0), Arg<GameEventInfo>(param, 1)),
                nameof(SkillHookSet.BotTakeover) => Invoke(definition.Hooks.BotTakeover, Arg<EventBotTakeover>(param, 0)),
                nameof(SkillHookSet.WeaponFire) => Invoke(definition.Hooks.WeaponFire, Arg<EventWeaponFire>(param, 0)),
                nameof(SkillHookSet.WeaponEquip) => Invoke(definition.Hooks.WeaponEquip, Arg<EventItemEquip>(param, 0)),
                nameof(SkillHookSet.WeaponPickup) => Invoke(definition.Hooks.WeaponPickup, Arg<EventItemPickup>(param, 0)),
                nameof(SkillHookSet.WeaponReload) => Invoke(definition.Hooks.WeaponReload, Arg<EventWeaponReload>(param, 0)),
                nameof(SkillHookSet.WeaponDrop) => Invoke(definition.Hooks.WeaponDrop, Arg<CounterStrikeSharp.API.Modules.Memory.DynamicFunctions.DynamicHook>(param, 0), Arg<CCSPlayerController>(param, 1)),
                nameof(SkillHookSet.GrenadeThrown) => Invoke(definition.Hooks.GrenadeThrown, Arg<EventGrenadeThrown>(param, 0)),
                nameof(SkillHookSet.BulletImpact) => Invoke(definition.Hooks.BulletImpact, Arg<EventBulletImpact>(param, 0)),
                nameof(SkillHookSet.BombBeginplant) => Invoke(definition.Hooks.BombBeginplant, Arg<EventBombBeginplant>(param, 0)),
                nameof(SkillHookSet.BombAbortplant) => Invoke(definition.Hooks.BombAbortplant, Arg<EventBombAbortplant>(param, 0)),
                nameof(SkillHookSet.BombPlanted) => Invoke(definition.Hooks.BombPlanted, Arg<EventBombPlanted>(param, 0)),
                nameof(SkillHookSet.BombBegindefuse) => Invoke(definition.Hooks.BombBegindefuse, Arg<EventBombBegindefuse>(param, 0)),
                nameof(SkillHookSet.DecoyStarted) => Invoke(definition.Hooks.DecoyStarted, Arg<EventDecoyStarted>(param, 0)),
                nameof(SkillHookSet.DecoyDetonate) => Invoke(definition.Hooks.DecoyDetonate, Arg<EventDecoyDetonate>(param, 0)),
                nameof(SkillHookSet.SmokegrenadeDetonate) => Invoke(definition.Hooks.SmokegrenadeDetonate, Arg<EventSmokegrenadeDetonate>(param, 0)),
                nameof(SkillHookSet.SmokegrenadeExpired) => Invoke(definition.Hooks.SmokegrenadeExpired, Arg<EventSmokegrenadeExpired>(param, 0)),
                nameof(SkillHookSet.OnTriggerEnter) => Invoke(definition.Hooks.OnTriggerEnter, Arg<CBaseTrigger>(param, 0), Arg<CBaseEntity>(param, 1)),
                nameof(SkillHookSet.OnTriggerExit) => Invoke(definition.Hooks.OnTriggerExit, Arg<CBaseTrigger>(param, 0), Arg<CBaseEntity>(param, 1)),
                nameof(SkillHookSet.OnWeaponCanAcquire) => Invoke(definition.Hooks.OnWeaponCanAcquire, Arg<CounterStrikeSharp.API.Modules.Memory.DynamicFunctions.DynamicHook>(param, 0), Arg<CCSPlayerController>(param, 1), Arg<CEconItemView>(param, 2), Arg<CCSWeaponBaseVData>(param, 3)),
                _ => null,
            };

            if (!PerfLog.Enabled)
                return InvokeTyped();

            long perfStart = PerfLog.Start();
            var result = InvokeTyped();
            PerfLog.End($"SkillAction {skill}.{methodName}", perfStart, 2.0);
            return result;
        }

        private static T Arg<T>(object[]? args, int index) =>
            args != null && args.Length > index && args[index] is T value
                ? value
                : throw new ArgumentException($"Missing or invalid skill-hook argument {index} ({typeof(T).Name}).");

        private static object? Invoke(Action? action)
        {
            action?.Invoke();
            return null;
        }

        private static object? Invoke<T>(Action<T>? action, T arg)
        {
            action?.Invoke(arg);
            return null;
        }

        private static object? Invoke<T1, T2>(Action<T1, T2>? action, T1 arg1, T2 arg2)
        {
            action?.Invoke(arg1, arg2);
            return null;
        }

        private static object? Invoke<T, TResult>(Func<T, TResult>? function, T arg) =>
            function == null ? null : function(arg);

        private static object? Invoke<T1, T2, TResult>(Func<T1, T2, TResult>? function, T1 arg1, T2 arg2) =>
            function == null ? null : function(arg1, arg2);

        private static object? Invoke<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult>? function, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            function == null ? null : function(arg1, arg2, arg3, arg4);

        internal new void AddCommand(string name, string description, CommandInfo.CommandCallback handler)
        {
            var definition = new CommandDefinition(name, description, handler);
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

        private static async void PrintInfoToConsole()
        {
            string? versionFromGithub = await GetLatestVersion();

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
            Console.WriteLine(Config.LoadedConfig.ConfigName);

            // Main config info
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("\nGameMode: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.Write($"{(Config.GameModes)Config.LoadedConfig.GameMode} ({Config.LoadedConfig.GameMode})");

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write(", DebugMode: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.WriteLine(Config.LoadedConfig.DebugMode);

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write("SkillHudDuration: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.Write(Config.LoadedConfig.SkillHudDuration == -1 ? "infinity" : Config.LoadedConfig.SkillHudDuration);

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write(", SkillButton: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.Write(Config.LoadedConfig.AlternativeSkillButton ?? "(NULL)");

            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.Cyan;
            Console.Write(", HtmlHudFix: ");
            Console.ForegroundColor = (ConsoleColor)CS2ConsoleColors.LightBlue;
            Console.WriteLine(Config.LoadedConfig.EnableFlashingHtmlHudFix);

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
            List<Skills> enabled = [];
            List<Skills> disabled = [];

            foreach (Skills skill in Enum.GetValues(typeof(Skills)))
                if (skill.ToString() == "None")
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

        private static async Task<string?> GetLatestVersion()
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("HeroShift", "1.0"));
            const string URL = "https://api.github.com/repos/Juzlus/HeroShift/releases/latest";

            try
            {
                string response = await client.GetStringAsync(URL);
                using JsonDocument doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("tag_name", out JsonElement value))
                    return value.GetString()?.Replace("v", "");
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal jSkill_PlayerInfo? GetPlayerInfoByIndex(uint playerIndex)
        {
            return PlayerManager.GetPlayerByIndex(playerIndex);
        }
    }

    /*
     * Per-player plugin state. One instance per connected player, kept in
     * PlayerManager and looked up with PlayerManager.GetPlayerByIndex(index).
     * This is the object a skill reads/writes to remember anything about its
     * holder for the round.
     */
    public class jSkill_PlayerInfo
    {
        public required bool IsBot { get; set; }
        public required string PlayerName { get; set; }
        public required uint PlayerIndex { get; set; }

        // The hero this player currently has. Skills compare against this to
        // decide "is this event mine?" - see any skill's `playerInfo?.Skill != skillName`.
        public Skills Skill { get; set; }
        public Skills SpecialSkill { get; set; }

        // The value rolled for this round by skills that randomise their
        // strength (speed multiplier, gravity, damage reduction, chance...).
        // Set in EnableSkill, read back in OnTick / damage hooks.
        public float? SkillChance { get; set; }

        public bool IsDrawing { get; set; }

        // HUD timing: when the hero name / description should stop being drawn,
        // and a window during which the HUD is suppressed entirely.
        public DateTime SkillHudExpired { get; set; }
        public DateTime SkillDescriptionHudExpired { get; set; }
        public DateTime HudSuppressedUntil { get; set; }

        // Free-form HTML a skill wants shown in the centre HUD this tick
        // (e.g. Distancer writes the nearest-enemy distance here).
        public string? PrintHTML { get; set; }
        public int HideHUD { get; set; }

        // Set true by one-shot-per-round skills so they cannot be used twice.
        public bool SkillUsed = false;
        public bool? HudOnDeathBlocked { get; set; }
    }

    public class jSkill_SkillInfo(Skills skill, string color, bool display)
    {
        public Skills Skill { get; } = skill;
        public string Color { get; set; } = color;
        public bool Display { get; } = display;

        public static implicit operator Skills(jSkill_SkillInfo v) => v?.Skill ?? Skills.None;
    }

    public static class SkillData
    {
        public static ConcurrentBag<jSkill_SkillInfo> Skills { get; } = [];

        private static Dictionary<Skills, jSkill_SkillInfo>? _bySkill;

        public static jSkill_SkillInfo? GetInfo(Skills skill)
        {
            var map = _bySkill;
            if (map == null)
            {
                map = new Dictionary<Skills, jSkill_SkillInfo>();
                foreach (var s in Skills)
                    map[s.Skill] = s;
                _bySkill = map;
            }
            return map.TryGetValue(skill, out var info) ? info : null;
        }

        public static void Invalidate() => _bySkill = null;
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