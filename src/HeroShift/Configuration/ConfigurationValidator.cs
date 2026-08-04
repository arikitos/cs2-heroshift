using src.Configuration.Models;
using src.SkillsCore.Abstractions;

namespace src.Configuration;

/*
 * ConfigurationValidator - validates a fully-merged HeroShiftConfiguration
 * before it becomes the active snapshot.
 * Collects every error instead of throwing on the first one, so an operator
 * sees the full list of problems in one pass. Deliberately conservative:
 * only rejects values that are structurally invalid (empty required alias
 * lists, out-of-range percentages/durations, unknown skill IDs) - it must
 * not reject any value the legacy system currently accepts.
 */
public static class ConfigurationValidator
{
    private const int SupportedSchemaVersion = 1;

    public static void ValidateAndThrow(HeroShiftConfiguration config, IReadOnlyCollection<SkillId>? knownSkillIds = null)
    {
        var errors = Validate(config, knownSkillIds);
        if (errors.Count > 0)
            throw new ConfigurationValidationException(errors);
    }

    public static IReadOnlyList<string> Validate(HeroShiftConfiguration config, IReadOnlyCollection<SkillId>? knownSkillIds = null)
    {
        var errors = new List<string>();

        if (config.SchemaVersion != SupportedSchemaVersion)
            errors.Add($"schemaVersion: unsupported value {config.SchemaVersion} (expected {SupportedSchemaVersion})");

        ValidateGeneral(config.General, errors);
        ValidateHud(config.Hud, errors);
        ValidateChat(config.Chat, errors);
        var seenAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ValidateCommands(config.Commands, seenAliases, errors);
        ValidateVoting(config.Voting, seenAliases, errors);
        ValidateSkillOverrides(config.Skills, knownSkillIds, errors);

        return errors;
    }

    private static void ValidateGeneral(GeneralOptions general, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(general.Language) ||
            general.Language.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
            errors.Add("general.language: use only ASCII letters, digits, hyphens, or underscores");

        if (general.SkillTimeBeforeStart < 0)
            errors.Add("general.skillTimeBeforeStart: value must be greater than or equal to 0");

        if (general.SkillHudDuration < -1)
            errors.Add("general.skillHudDuration: value must be -1 (infinite) or greater than or equal to 0");

        if (general.SkillDescriptionDuration < -1)
            errors.Add("general.skillDescriptionDuration: value must be -1 (infinite) or greater than or equal to 0");

        if (general.CurseSkillPerPlayer is < 0)
            errors.Add("general.curseSkillPerPlayer: value must be null (unlimited) or greater than or equal to 0");
    }

    private static void ValidateHud(HudOptions hud, List<string> errors)
    {
        var required = new Dictionary<string, string?>
        {
            ["hud.headerLineColor"] = hud.HeaderLineColor,
            ["hud.headerLineSize"] = hud.HeaderLineSize,
            ["hud.skillLineSize"] = hud.SkillLineSize,
            ["hud.infoLineColor"] = hud.InfoLineColor,
            ["hud.infoLineSize"] = hud.InfoLineSize,
            ["hud.skillDescriptionLineColor"] = hud.SkillDescriptionLineColor,
            ["hud.skillDescriptionLineSize"] = hud.SkillDescriptionLineSize,
            ["hud.wsadMenuSelectInfoLineColor"] = hud.WsadMenuSelectInfoLineColor,
            ["hud.wsadMenuSelectInfoLineSize"] = hud.WsadMenuSelectInfoLineSize,
            ["hud.wsadMenuItemLineColor"] = hud.WsadMenuItemLineColor,
            ["hud.wsadMenuItemHoverLineColor"] = hud.WsadMenuItemHoverLineColor,
            ["hud.wsadMenuItemLineSize"] = hud.WsadMenuItemLineSize,
            ["hud.wsadMenuControllsLineSize"] = hud.WsadMenuControllsLineSize,
            ["hud.wsadMenuControllsLineColor1"] = hud.WsadMenuControllsLineColor1,
            ["hud.wsadMenuControllsLineColor2"] = hud.WsadMenuControllsLineColor2,
            ["hud.wsadMenuControllsLineColor3"] = hud.WsadMenuControllsLineColor3,
        };

        foreach (var (path, value) in required)
            if (value == null)
                errors.Add($"{path}: value cannot be null");
    }

    private static void ValidateChat(ChatOptions chat, List<string> errors)
    {
        if (chat.MaxWidth <= 0)
            errors.Add("chat.maxWidth: value must be greater than 0");
        if (chat.LineColor == null)
            errors.Add("chat.lineColor: value cannot be null");
        if (chat.InfoPlayerNameColor == null)
            errors.Add("chat.infoPlayerNameColor: value cannot be null");
        if (chat.InfoSkillColor == null)
            errors.Add("chat.infoSkillColor: value cannot be null");
        if (chat.TagFormat == null)
            errors.Add("chat.tagFormat: value cannot be null");
    }

    private static void ValidateCommands(CommandOptions commands, Dictionary<string, string> seenAliases, List<string> errors)
    {
        void ValidateCommand(string path, CommandDefinition command)
        {
            ValidateAliasList(path, command.Aliases, seenAliases, errors);
            if (command.Permission == null)
                errors.Add($"{path}.permission: value cannot be null");
        }

        ValidateCommand("commands.setSkillCommand", commands.SetSkillCommand);
        ValidateCommand("commands.skillsListCommand", commands.SkillsListCommand);
        ValidateCommand("commands.useSkillCommand", commands.UseSkillCommand);
        ValidateCommand("commands.healCommand", commands.HealCommand);
        ValidateCommand("commands.healthCommand", commands.HealthCommand);
        ValidateCommand("commands.plantedBomb", commands.PlantedBomb);
        ValidateCommand("commands.botPlace", commands.BotPlace);
        ValidateCommand("commands.consoleCommand", commands.ConsoleCommand);
        ValidateCommand("commands.hudCommand", commands.HudCommand);
        ValidateCommand("commands.setStaticSkillCommand", commands.SetStaticSkillCommand);
        ValidateCommand("commands.reloadCommand", commands.ReloadCommand);
        ValidateCommand("commands.nextCommand", commands.NextCommand);
        ValidateCommand("commands.checkEntityCommand", commands.CheckEntityCommand);
    }

    private static void ValidateVoting(VotingOptions voting, Dictionary<string, string> seenAliases, List<string> errors)
    {
        ValidateAliasList("voting.startGameCommand", voting.StartGameCommand.Aliases, seenAliases, errors);
        if (voting.StartGameCommand.Permission == null)
            errors.Add("voting.startGameCommand.permission: value cannot be null");
        if (voting.StartGameCommand.StartParams == null)
            errors.Add("voting.startGameCommand.startParams: value cannot be null");
        if (voting.StartGameCommand.SvStartParams == null)
            errors.Add("voting.startGameCommand.svStartParams: value cannot be null");
        ValidateVotingTimings("voting.startGameCommand", voting.StartGameCommand.TimeToVote, voting.StartGameCommand.PercentagesToSuccess, voting.StartGameCommand.TimeToNextVoting, voting.StartGameCommand.TimeToNextSameVoting, voting.StartGameCommand.MinimumPlayersToStartVoting, errors);

        ValidateVotingCommand("voting.changeMapCommand", voting.ChangeMapCommand, seenAliases, errors);
        ValidateVotingCommand("voting.swapCommand", voting.SwapCommand, seenAliases, errors);
        ValidateVotingCommand("voting.shuffleCommand", voting.ShuffleCommand, seenAliases, errors);
        ValidateVotingCommand("voting.pauseCommand", voting.PauseCommand, seenAliases, errors);
        ValidateVotingCommand("voting.setScoreCommand", voting.SetScoreCommand, seenAliases, errors);
    }

    private static void ValidateVotingCommand(string path, VotingCommandDefinition command, Dictionary<string, string> seenAliases, List<string> errors)
    {
        ValidateAliasList(path, command.Aliases, seenAliases, errors);
        if (command.Permission == null)
            errors.Add($"{path}.permission: value cannot be null");
        ValidateVotingTimings(path, command.TimeToVote, command.PercentagesToSuccess, command.TimeToNextVoting, command.TimeToNextSameVoting, command.MinimumPlayersToStartVoting, errors);
    }

    private static void ValidateVotingTimings(string path, float timeToVote, float percentagesToSuccess, float timeToNextVoting, float timeToNextSameVoting, int minimumPlayers, List<string> errors)
    {
        if (timeToVote < 0)
            errors.Add($"{path}.timeToVote: value must be greater than or equal to 0");

        if (percentagesToSuccess is < 0 or > 100)
            errors.Add($"{path}.percentagesToSuccess: value must be between 0 and 100");

        if (timeToNextVoting < 0)
            errors.Add($"{path}.timeToNextVoting: value must be greater than or equal to 0");

        if (timeToNextSameVoting < 0)
            errors.Add($"{path}.timeToNextSameVoting: value must be greater than or equal to 0");

        if (minimumPlayers < 0)
            errors.Add($"{path}.minimumPlayersToStartVoting: value must be greater than or equal to 0");
    }

    private static void ValidateAliasList(string path, IReadOnlyList<string>? aliases, Dictionary<string, string> seenAliases, List<string> errors)
    {
        if (aliases == null || aliases.Count == 0)
        {
            errors.Add($"{path}.aliases: must contain at least one alias");
            return;
        }

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                errors.Add($"{path}.aliases: contains an empty alias");
                continue;
            }

            var trimmed = alias.Trim();
            if (seenAliases.TryGetValue(trimmed, out var owner) && owner != path)
                errors.Add($"{path}.aliases: alias '{trimmed}' is already registered by {owner}");
            else
                seenAliases[trimmed] = path;
        }
    }

    private static void ValidateSkillOverrides(IReadOnlyDictionary<SkillId, SkillOverride> overrides, IReadOnlyCollection<SkillId>? knownSkillIds, List<string> errors)
    {
        foreach (var (id, @override) in overrides)
        {
            if (knownSkillIds != null && !knownSkillIds.Contains(id))
                errors.Add($"skills.{id}: unknown skill ID");

            if (@override.MaxPerServer is < -1)
                errors.Add($"skills.{id}.maxPerServer: value must be -1 (unlimited) or greater than or equal to 0");

            if (@override.HudDuration is < -1)
                errors.Add($"skills.{id}.hudDuration: value must be null, -1 (infinite) or greater than or equal to 0");

            if (@override.DescriptionHudDuration is < -1)
                errors.Add($"skills.{id}.descriptionHudDuration: value must be null, -1 (infinite) or greater than or equal to 0");
        }
    }
}
