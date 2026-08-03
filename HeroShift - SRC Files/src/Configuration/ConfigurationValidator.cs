using src.Configuration.Models;
using src.SkillsCore.Abstractions;

namespace src.Configuration;

/*
 * ConfigurationValidator - validates a fully-merged HeroShiftConfiguration
 * before it becomes the active snapshot (REFACTOR.md section 14-15).
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
        ValidateCommands(config.Commands, errors);
        ValidateVoting(config.Voting, errors);
        ValidateSkillOverrides(config.Skills, knownSkillIds, errors);

        return errors;
    }

    private static void ValidateGeneral(GeneralOptions general, List<string> errors)
    {
        if (general.SkillTimeBeforeStart < 0)
            errors.Add("general.skillTimeBeforeStart: value must be greater than or equal to 0");

        if (general.SkillHudDuration < -1)
            errors.Add("general.skillHudDuration: value must be -1 (infinite) or greater than or equal to 0");

        if (general.SkillDescriptionDuration < -1)
            errors.Add("general.skillDescriptionDuration: value must be -1 (infinite) or greater than or equal to 0");

        if (general.CurseSkillPerPlayer is < 0)
            errors.Add("general.curseSkillPerPlayer: value must be null (unlimited) or greater than or equal to 0");
    }

    private static void ValidateCommands(CommandOptions commands, List<string> errors)
    {
        var seenAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void ValidateCommand(string path, CommandDefinition command) =>
            ValidateAliasList(path, command.Aliases, seenAliases, errors);

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

    private static void ValidateVoting(VotingOptions voting, List<string> errors)
    {
        var seenAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        ValidateAliasList("voting.startGameCommand", voting.StartGameCommand.Aliases, seenAliases, errors);
        ValidateVotingTimings("voting.startGameCommand", voting.StartGameCommand.TimeToVote, voting.StartGameCommand.PercentagesToSuccess, voting.StartGameCommand.MinimumPlayersToStartVoting, errors);

        ValidateVotingCommand("voting.changeMapCommand", voting.ChangeMapCommand, seenAliases, errors);
        ValidateVotingCommand("voting.swapCommand", voting.SwapCommand, seenAliases, errors);
        ValidateVotingCommand("voting.shuffleCommand", voting.ShuffleCommand, seenAliases, errors);
        ValidateVotingCommand("voting.pauseCommand", voting.PauseCommand, seenAliases, errors);
        ValidateVotingCommand("voting.setScoreCommand", voting.SetScoreCommand, seenAliases, errors);
    }

    private static void ValidateVotingCommand(string path, VotingCommandDefinition command, Dictionary<string, string> seenAliases, List<string> errors)
    {
        ValidateAliasList(path, command.Aliases, seenAliases, errors);
        ValidateVotingTimings(path, command.TimeToVote, command.PercentagesToSuccess, command.MinimumPlayersToStartVoting, errors);
    }

    private static void ValidateVotingTimings(string path, float timeToVote, float percentagesToSuccess, int minimumPlayers, List<string> errors)
    {
        if (timeToVote < 0)
            errors.Add($"{path}.timeToVote: value must be greater than or equal to 0");

        if (percentagesToSuccess is < 0 or > 100)
            errors.Add($"{path}.percentagesToSuccess: value must be between 0 and 100");

        if (minimumPlayers < 0)
            errors.Add($"{path}.minimumPlayersToStartVoting: value must be greater than or equal to 0");
    }

    private static void ValidateAliasList(string path, IReadOnlyList<string> aliases, Dictionary<string, string> seenAliases, List<string> errors)
    {
        if (aliases.Count == 0)
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
