using src.Configuration;
using src.Configuration.Models;
using src.SkillsCore;
using static src.HeroShift;

namespace src.utils;

/*
 * Compatibility facade for call sites that still use Config.LoadedConfig.
 * Runtime data comes exclusively from the immutable typed heroshift.json
 * snapshot; this type contains no defaults and performs no legacy file I/O.
 */
public static class Config
{
    private static SettingsModel _loadedConfig = new(new HeroShiftConfiguration());

    public static SettingsModel LoadedConfig => Volatile.Read(ref _loadedConfig);

    public static SettingsModel Initialize(SkillRegistry registry)
    {
        string path = Path.Combine(Instance.ModuleDirectory, "configs", "heroshift.json");
        var snapshot = ConfigurationStore.Initialize(path, registry, Instance.Logger);
        var adapted = new SettingsModel(snapshot.Configuration);
        Volatile.Write(ref _loadedConfig, adapted);
        return adapted;
    }

    public static SettingsModel LoadConfig()
    {
        var snapshot = ConfigurationStore.Reload();
        var adapted = new SettingsModel(snapshot.Configuration);
        Volatile.Write(ref _loadedConfig, adapted);
        return adapted;
    }

    public sealed class SettingsModel
    {
        public SettingsModel() : this(new HeroShiftConfiguration()) { }
        public SettingsModel(HeroShiftConfiguration config)
        {
            ConfigName = "heroshift.json";
            GameMode = (int)config.General.GameMode;
            YourSkillChatInfo = config.General.YourSkillChatInfo;
            KillerSkillChatInfo = config.General.KillerSkillChatInfo;
            TeamMateSkillChatInfo = config.General.TeamMateSkillChatInfo;
            SummaryAfterTheRound = config.General.SummaryAfterTheRound;
            EnableBotSkills = config.General.EnableBotSkills;
            EnableBotKickDebug = config.General.EnableBotKickDebug;
            EnableFullForceUpdate = config.General.EnableFullForceUpdate;
            DebugMode = config.General.DebugMode;
            PerfMode = config.General.PerfMode;
            AlternativeSkillButton = config.General.AlternativeSkillButton;
            SkillTimeBeforeStart = config.General.SkillTimeBeforeStart;
            SkillHudDuration = config.General.SkillHudDuration;
            SkillDescriptionDuration = config.General.SkillDescriptionDuration;
            DisplayAlwaysDescription = config.General.DisplayAlwaysDescription;
            DisableSpectateHUD = config.General.DisableSpectateHUD;
            HideHudForOtherPlugins = config.General.HideHudForOtherPlugins;
            EnableFlashingHtmlHudFix = config.General.EnableFlashingHtmlHudFix;
            TraceRayBeam = config.General.TraceRayBeam;
            DisableHUDOnDeathPermission = config.General.DisableHUDOnDeathPermission;
            DisableSkillsOnRoundEnd = config.General.DisableSkillsOnRoundEnd;
            CurseSkillPerPlayer = config.General.CurseSkillPerPlayer;
            HtmlHudCustomisation = new HtmlHudCustomisation(config.Hud);
            ChatMessage = new ChatMessage(config.Chat);
            NormalCommands = new NormalCommands(config.Commands);
            VotingCommands = new VotingCommands(config.Voting);
        }

        public string ConfigName { get; }
        public int GameMode { get; }
        public bool YourSkillChatInfo { get; }
        public bool KillerSkillChatInfo { get; }
        public bool TeamMateSkillChatInfo { get; }
        public bool SummaryAfterTheRound { get; }
        public bool EnableBotSkills { get; }
        public bool EnableBotKickDebug { get; }
        public bool EnableFullForceUpdate { get; }
        public bool DebugMode { get; }
        public bool PerfMode { get; }
        public string? AlternativeSkillButton { get; }
        public float SkillTimeBeforeStart { get; }
        public float SkillHudDuration { get; }
        public float SkillDescriptionDuration { get; }
        public bool DisplayAlwaysDescription { get; }
        public bool DisableSpectateHUD { get; }
        public bool HideHudForOtherPlugins { get; }
        public bool EnableFlashingHtmlHudFix { get; }
        public bool TraceRayBeam { get; }
        public string DisableHUDOnDeathPermission { get; }
        public bool DisableSkillsOnRoundEnd { get; }
        public int? CurseSkillPerPlayer { get; }
        public HtmlHudCustomisation HtmlHudCustomisation { get; }
        public ChatMessage ChatMessage { get; }
        public NormalCommands NormalCommands { get; }
        public VotingCommands VotingCommands { get; }
    }

    public sealed class ChatMessage(ChatOptions options)
    {
        public float MaxWidth { get; } = options.MaxWidth;
        public char LineSymbol { get; } = options.LineSymbol;
        public string LineColor { get; } = options.LineColor;
        public bool LineShow { get; } = options.LineShow;
        public string InfoPlayerNameColor { get; } = options.InfoPlayerNameColor;
        public string InfoSkillColor { get; } = options.InfoSkillColor;
        public bool InfoMessageShow { get; } = options.InfoMessageShow;
        public string TagFormat { get; } = options.TagFormat;
    }

    public sealed class HtmlHudCustomisation(HudOptions options)
    {
        public string HeaderLineColor { get; } = options.HeaderLineColor;
        public string HeaderLineSize { get; } = options.HeaderLineSize;
        public string SkillLineSize { get; } = options.SkillLineSize;
        public string InfoLineColor { get; } = options.InfoLineColor;
        public string InfoLineSize { get; } = options.InfoLineSize;
        public string SkillDescriptionLineColor { get; } = options.SkillDescriptionLineColor;
        public string SkillDescriptionLineSize { get; } = options.SkillDescriptionLineSize;
        public string WSADMenuSelectInfoLineColor { get; } = options.WsadMenuSelectInfoLineColor;
        public string WSADMenuSelectInfoLineSize { get; } = options.WsadMenuSelectInfoLineSize;
        public string WSADMenuItemLineColor { get; } = options.WsadMenuItemLineColor;
        public string WSADMenuItemHoverLineColor { get; } = options.WsadMenuItemHoverLineColor;
        public string WSADMenuItemLineSize { get; } = options.WsadMenuItemLineSize;
        public string WSADMenuControllsLineSize { get; } = options.WsadMenuControllsLineSize;
        public string WSADMenuControllsLineColor1 { get; } = options.WsadMenuControllsLineColor1;
        public string WSADMenuControllsLineColor2 { get; } = options.WsadMenuControllsLineColor2;
        public string WSADMenuControllsLineColor3 { get; } = options.WsadMenuControllsLineColor3;
    }

    public class NormalCommand(CommandDefinition definition)
    {
        public string Alias { get; } = string.Join(", ", definition.Aliases);
        public string Permissions { get; } = definition.Permission;
    }

    public sealed class NormalCommands
    {
        public NormalCommands(CommandOptions options)
        {
            SetSkillCommand = new(options.SetSkillCommand);
            SkillsListCommand = new(options.SkillsListCommand);
            UseSkillCommand = new(options.UseSkillCommand);
            HealCommand = new(options.HealCommand);
            HealthCommand = new(options.HealthCommand);
            PlantedBomb = new(options.PlantedBomb);
            BotPlace = new(options.BotPlace);
            ConsoleCommand = new(options.ConsoleCommand);
            HudCommand = new(options.HudCommand);
            SetStaticSkillCommand = new(options.SetStaticSkillCommand);
            ReloadCommand = new(options.ReloadCommand);
            NextCommand = new(options.NextCommand);
            CheckEntityCommand = new(options.CheckEntityCommand);
        }
        public NormalCommand SetSkillCommand { get; }
        public NormalCommand SkillsListCommand { get; }
        public NormalCommand UseSkillCommand { get; }
        public NormalCommand HealCommand { get; }
        public NormalCommand HealthCommand { get; }
        public NormalCommand PlantedBomb { get; }
        public NormalCommand BotPlace { get; }
        public NormalCommand ConsoleCommand { get; }
        public NormalCommand HudCommand { get; }
        public NormalCommand SetStaticSkillCommand { get; }
        public NormalCommand ReloadCommand { get; }
        public NormalCommand NextCommand { get; }
        public NormalCommand CheckEntityCommand { get; }
    }

    public class VotingCommand(VotingCommandDefinition definition) : NormalCommand(
        new CommandDefinition { Aliases = definition.Aliases, Permission = definition.Permission })
    {
        public bool EnableVoting { get; } = definition.EnableVoting;
        public float TimeToVote { get; } = definition.TimeToVote;
        public float PercentagesToSuccess { get; } = definition.PercentagesToSuccess;
        public float TimeToNextVoting { get; } = definition.TimeToNextVoting;
        public float TimeToNextSameVoting { get; } = definition.TimeToNextSameVoting;
        public int MinimumPlayersToStartVoting { get; } = definition.MinimumPlayersToStartVoting;
    }

    public sealed class StartGameCommand(StartGameCommandDefinition definition)
    {
        public bool EnableVoting { get; } = definition.EnableVoting;
        public string Alias { get; } = string.Join(", ", definition.Aliases);
        public string Permissions { get; } = definition.Permission;
        public string StartParams { get; } = definition.StartParams;
        public string SVStartParams { get; } = definition.SvStartParams;
        public float TimeToVote { get; } = definition.TimeToVote;
        public float PercentagesToSuccess { get; } = definition.PercentagesToSuccess;
        public float TimeToNextVoting { get; } = definition.TimeToNextVoting;
        public float TimeToNextSameVoting { get; } = definition.TimeToNextSameVoting;
        public int MinimumPlayersToStartVoting { get; } = definition.MinimumPlayersToStartVoting;
    }

    public sealed class VotingCommands
    {
        public VotingCommands(VotingOptions options)
        {
            StartGameCommand = new(options.StartGameCommand);
            ChangeMapCommand = new(options.ChangeMapCommand);
            SwapCommand = new(options.SwapCommand);
            ShuffleCommand = new(options.ShuffleCommand);
            PauseCommand = new(options.PauseCommand);
            SetScoreCommand = new(options.SetScoreCommand);
        }
        public StartGameCommand StartGameCommand { get; }
        public VotingCommand ChangeMapCommand { get; }
        public VotingCommand SwapCommand { get; }
        public VotingCommand ShuffleCommand { get; }
        public VotingCommand PauseCommand { get; }
        public VotingCommand SetScoreCommand { get; }
    }

    public enum GameModes
    {
        Normal = 0,
        TeamSkills = 1,
        SameSkills = 2,
        NoRepeat = 3,
        FullRandom = 4,
        Debug = 5,
    }
}
