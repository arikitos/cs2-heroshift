using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static src.HeroShift;

namespace src.utils
{
    /*
     * Config - the global plugin settings, stored in configs/config.json.
     *
     * This is the server-wide behaviour of the plugin (game mode, HUD, chat,
     * command names, voting). Per-hero balance values are NOT here - those live
     * in configs/skillsInfo.json, see utils/SkillsInfo.cs.
     *
     * The file is created with defaults on first load. Read values anywhere with
     * Config.LoadedConfig.<Property>. Missing keys fall back to the defaults set
     * in the SettingsModel constructor below.
     */
    public static class Config
    {
        private static readonly string configsFolder = Path.Combine(Instance.ModuleDirectory, "configs");
        private static readonly string configPath = Path.Combine(configsFolder, "config.json");
        private static readonly object fileLock = new();

        private static SettingsModel config = LoadConfig();
        public static SettingsModel LoadedConfig => config;

        public static SettingsModel LoadConfig()
        {
            lock (fileLock)
            {
                var newConfig = new SettingsModel();

                if (!File.Exists(configPath))
                {
                    Instance.Logger.LogInformation("Config file does not exist. Create a new config file...");
                    SaveConfig(newConfig);
                    return config = newConfig;
                }

                try
                {
                    string json;
                    using (var fs = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                        json = sr.ReadToEnd();
                    newConfig = JsonConvert.DeserializeObject<SettingsModel>(json) ?? new SettingsModel();
                }
                catch
                {
                    Instance.Logger.LogError("Error when loading the config file.");
                }

                if (newConfig.DisplayAlwaysDescription)
                    newConfig.SkillDescriptionDuration = 9999;
                return config = newConfig;
            }
        }

        public static void SaveConfig(SettingsModel config)
        {
            lock (fileLock)
            {
                try
                {
                    Directory.CreateDirectory(configsFolder);
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);

                    string tempPath = $"{configPath}.temp";
                    File.WriteAllText(tempPath, json);

                    File.Copy(tempPath, configPath, overwrite: true);
                    File.Delete(tempPath);
                }
                catch
                {
                    Instance.Logger.LogError("Error when saving the config file.");
                }
            }
        }

        public class SettingsModel
        {
            // Label only - shown in console as the active preset name.
            public string ConfigName { get; set; }

            // How skills are handed out each round. See the GameModes enum below.
            public int GameMode { get; set; }

            // Which chat announcements are printed.
            public bool YourSkillChatInfo { get; set; }      // your own hero
            public bool KillerSkillChatInfo { get; set; }    // the hero that killed you
            public bool TeamMateSkillChatInfo { get; set; }  // your teammates' heroes
            public bool SummaryAfterTheRound { get; set; }   // recap at round end

            // Bots take part in the skill draw like real players.
            public bool EnableBotSkills { get; set; }
            public bool EnableBotKickDebug { get; set; }
            public bool EnableFullForceUpdate { get; set; }

            // DebugMode writes the debug log; PerfMode logs slow hooks (PerfLog).
            public bool DebugMode { get; set; }
            public bool PerfMode { get; set; }

            // Extra key that triggers UseSkill in addition to the default one
            // (a console button name, e.g. "e"). null = only the default.
            public string? AlternativeSkillButton { get; set; }

            // Seconds after round start before skills become usable.
            public float SkillTimeBeforeStart { get; set; }

            // HUD lifetimes in seconds. -1 = show for the whole round.
            public float SkillHudDuration { get; set; }
            public float SkillDescriptionDuration { get; set; }
            // When true, SkillDescriptionDuration is forced to 9999 on load.
            public bool DisplayAlwaysDescription { get; set; }

            public bool DisableSpectateHUD { get; set; }
            public bool HideHudForOtherPlugins { get; set; }
            // Workaround for the HTML HUD flickering when other plugins draw too.
            public bool EnableFlashingHtmlHudFix { get; set; }
            // Draw a visible beam along ray traces (debugging aid).
            public bool TraceRayBeam { get; set; }

            public string DisableHUDOnDeathPermission { get; set; }
            // Strip everyone's skill the moment the round ends.
            public bool DisableSkillsOnRoundEnd { get; set; }
            // Cap on how many curse-type skills may target one player
            // (null = no limit). Enforced by SkillUtils.TryClaimCurse.
            public int? CurseSkillPerPlayer { get; set; }
            public HtmlHudCustomisation HtmlHudCustomisation { get; set; }
            public ChatMessage ChatMessage { get; set; }
            public NormalCommands NormalCommands { get; set; }
            public VotingCommands VotingCommands { get; set; }

            public SettingsModel()
            {
                ConfigName = "Default";
                GameMode = (int)GameModes.NoRepeat;
                YourSkillChatInfo = true;
                KillerSkillChatInfo = true;
                TeamMateSkillChatInfo = true;
                SummaryAfterTheRound = true;
                EnableBotSkills = true;
                EnableBotKickDebug = false;
                EnableFullForceUpdate = false;
                DebugMode = false;
                PerfMode = false;
                AlternativeSkillButton = null;
                SkillTimeBeforeStart = 7;
                SkillHudDuration = -1;
                SkillDescriptionDuration = 7;
                DisplayAlwaysDescription = false;
                EnableFlashingHtmlHudFix = false;
                TraceRayBeam = false;
                DisableSpectateHUD = false;
                HideHudForOtherPlugins = true;
                DisableHUDOnDeathPermission = "@HeroShift/death";
                DisableSkillsOnRoundEnd = false;
                CurseSkillPerPlayer = null;

                HtmlHudCustomisation = new HtmlHudCustomisation
                {
                    HeaderLineColor = "#FFFFFF",
                    HeaderLineSize = "",
                    SkillLineSize = "l",
                    InfoLineColor = "#FFFFFF",
                    InfoLineSize = "sm",
                    SkillDescriptionLineColor = "#999999",
                    SkillDescriptionLineSize = "sm",
                    WSADMenuSelectInfoLineColor = "#999999",
                    WSADMenuSelectInfoLineSize = "sm",
                    WSADMenuItemLineColor = "white",
                    WSADMenuItemHoverLineColor = "orange",
                    WSADMenuItemLineSize = "sm",
                    WSADMenuControllsLineSize = "sm",
                    WSADMenuControllsLineColor1 = "cyan",
                    WSADMenuControllsLineColor2 = "white",
                    WSADMenuControllsLineColor3 = "green",
                };

                ChatMessage = new ChatMessage
                {
                    MaxWidth = 1280,
                    LineSymbol = '―',
                    LineColor = "\x04",
                    LineShow = true,
                    InfoPlayerNameColor = "\x02",
                    InfoSkillColor = "\x06",
                    InfoMessageShow = true,
                    TagFormat = "\x02◢◆◤ {TAG} ◥◆◣",
                };

                NormalCommands = new NormalCommands
                {
                    SetSkillCommand = new NormalCommand("setskill, set_skill", "@HeroShift/admin"),
                    SkillsListCommand = new NormalCommand("skills", "@HeroShift/admin"),
                    UseSkillCommand = new NormalCommand("t, useSkill", "@HeroShift/admin"),
                    HealCommand = new NormalCommand("heal", "@HeroShift/admin"),
                    HealthCommand = new NormalCommand("sethealth, set_health, health", "@HeroShift/admin"),
                    PlantedBomb = new NormalCommand("plantedbomb, planted_bomb, bomb", "@HeroShift/admin"),
                    BotPlace = new NormalCommand("botplace, bot_place", "@HeroShift/admin"),
                    ConsoleCommand = new NormalCommand("console, sv", "@HeroShift/owner"),
                    HudCommand = new NormalCommand("hud, hood", ""),
                    SetStaticSkillCommand = new NormalCommand("setstaticskill, set_static_skill", "@HeroShift/admin"),
                    ReloadCommand = new NormalCommand("reload, refresh", "@HeroShift/admin"),
                    NextCommand = new NormalCommand("next_skill", "@HeroShift/admin"),
                    CheckEntityCommand = new NormalCommand("ent, entity, checkentity, check_entity, checkent, check_ent", "@HeroShift/owner"),
                };

                VotingCommands = new VotingCommands
                {
                    StartGameCommand = new StartGameCommand(true, "start, go", "@HeroShift/admin", "mp_freezetime 15; mp_forcecamera 0; mp_overtime_enable 1; sv_cheats 0", "mp_freezetime 0; mp_forcecamera 0; mp_overtime_enable 1; sv_cheats 1", 15, 60, 15, 500, 2),
                    ChangeMapCommand = new VotingCommand(true, "map, changemap", "@HeroShift/admin", 25, 90, 15, 500, 2),
                    SwapCommand = new VotingCommand(true, "swap", "@HeroShift/admin", 15, 90, 15, 20, 2),
                    ShuffleCommand = new VotingCommand(true, "shuffle", "@HeroShift/admin", 15, 90, 15, 20, 2),
                    PauseCommand = new VotingCommand(true, "pause, unpause", "@HeroShift/admin", 15, 60, 15, 2, 2),
                    SetScoreCommand = new VotingCommand(true, "setscore", "@HeroShift/owner", 15, 90, 15, 90, 2),
                };
            }
        }

        public class ChatMessage
        {
            public required float MaxWidth { get; set; }
            public required char LineSymbol { get; set; }
            public required string LineColor { get; set; }
            public required bool LineShow { get; set; }
            public required string InfoPlayerNameColor { get; set; }
            public required string InfoSkillColor { get; set; }
            public required bool InfoMessageShow { get; set; }
            public required string TagFormat { get; set; }
        }

        public class HtmlHudCustomisation
        {
            public required string HeaderLineColor { get; set; }
            public required string HeaderLineSize { get; set; }
            public required string SkillLineSize { get; set; }
            public required string InfoLineColor { get; set; }
            public required string InfoLineSize { get; set; }
            public required string SkillDescriptionLineColor { get; set; }
            public required string SkillDescriptionLineSize { get; set; }
            public required string WSADMenuSelectInfoLineColor { get; set; }
            public required string WSADMenuSelectInfoLineSize { get; set; }
            public required string WSADMenuItemLineColor { get; set; }
            public required string WSADMenuItemHoverLineColor { get; set; }
            public required string WSADMenuItemLineSize { get; set; }
            public required string WSADMenuControllsLineSize { get; set; }
            public required string WSADMenuControllsLineColor1 { get; set; }
            public required string WSADMenuControllsLineColor2 { get; set; }
            public required string WSADMenuControllsLineColor3 { get; set; }
        }

        public class NormalCommand(string alias, string permissions)
        {
            public string Alias { get; set; } = alias;
            public string Permissions { get; set; } = permissions;
        }

        public class NormalCommands
        {
            public required NormalCommand SetSkillCommand { get; set; }
            public required NormalCommand SkillsListCommand { get; set; }
            public required NormalCommand UseSkillCommand { get; set; }
            public required NormalCommand HealCommand { get; set; }
            public required NormalCommand HealthCommand { get; set; }
            public required NormalCommand PlantedBomb { get; set; }
            public required NormalCommand BotPlace { get; set; }
            public required NormalCommand ConsoleCommand { get; set; }
            public required NormalCommand HudCommand { get; set; }
            public required NormalCommand SetStaticSkillCommand { get; set; }
            public required NormalCommand ReloadCommand { get; set; }
            public required NormalCommand NextCommand { get; set; }
            public required NormalCommand CheckEntityCommand { get; set; }
        }

        public class VotingCommand(bool enableVoting, string alias, string permissions, float timeToVote, float percentagesToSuccess, float timeToNextVoting, float timeToNextSameVoting, int minimumPlayersToStartVoting) : NormalCommand(alias, permissions)
        {
            public bool EnableVoting { get; set; } = enableVoting;
            public float TimeToVote { get; set; } = timeToVote;
            public float PercentagesToSuccess { get; set; } = percentagesToSuccess;
            public float TimeToNextVoting { get; set; } = timeToNextVoting;
            public float TimeToNextSameVoting { get; set; } = timeToNextSameVoting;
            public int MinimumPlayersToStartVoting { get; set; } = minimumPlayersToStartVoting;
        }

        public class StartGameCommand(bool enableVoting, string alias, string permissions, string startParams, string svStartParams, float timeToVote, float percentagesToSuccess, float timeToNextVoting, float timeToNextSameVoting, int minimumPlayersToStartVoting)
        {
            public bool EnableVoting { get; set; } = enableVoting;
            public string Alias { get; set; } = alias;
            public string Permissions { get; set; } = permissions;
            public string StartParams { get; set; } = startParams;
            public string SVStartParams { get; set; } = svStartParams;
            public float TimeToVote { get; set; } = timeToVote;
            public float PercentagesToSuccess { get; set; } = percentagesToSuccess;
            public float TimeToNextVoting { get; set; } = timeToNextVoting;
            public float TimeToNextSameVoting { get; set; } = timeToNextSameVoting;
            public int MinimumPlayersToStartVoting { get; set; } = minimumPlayersToStartVoting;
        }

        public class VotingCommands
        {
            public required StartGameCommand StartGameCommand { get; set; }
            public required VotingCommand ChangeMapCommand { get; set; }
            public required VotingCommand SwapCommand { get; set; }
            public required VotingCommand ShuffleCommand { get; set; }
            public required VotingCommand PauseCommand { get; set; }
            public required VotingCommand SetScoreCommand { get; set; }
        }

        /*
         * How the per-round skill draw works. Set via GameMode above.
         *   Normal      - everyone gets an independent random skill
         *   TeamSkills  - one skill per team; the whole team shares it
         *   SameSkills  - one skill for the entire server that round
         *   NoRepeat    - random, but avoids skills already used this map
         *                 (tracked in HeroShift.SkillsUsedThisMap) - the default
         *   FullRandom  - random with no history or restrictions
         *   Debug       - for testing; see src/player/Debug.cs
         */
        public enum GameModes
        {
            Normal = 0,
            TeamSkills = 1,
            SameSkills = 2,
            NoRepeat = 3,
            FullRandom = 4,
            Debug = 5
        }
    }
}