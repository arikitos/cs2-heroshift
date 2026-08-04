using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using src.Configuration.Models;
using src.SkillsCore.Abstractions;

namespace src.Configuration;

/*
 * ConfigurationLoader - reads heroshift.json overrides and merges them onto
 * the canonical code defaults to produce a validated, immutable
 * ConfigurationSnapshot.
 *
 * Resolution order (section 14):
 *   canonical code defaults -> read heroshift.json overrides -> validate
 *   schemaVersion -> reject unknown root sections -> resolve global options ->
 *   resolve per-skill overrides -> validate full effective configuration ->
 *   create immutable snapshot.
 *
 * This is the sole runtime parser. Plugin startup and reload publish its validated result atomically through ConfigurationStore.
 */
public static class ConfigurationLoader
{
    private static readonly string[] KnownRootSections = ["schemaVersion", "general", "hud", "chat", "commands", "voting", "skills"];

    // Loads from `path`, merging over defaults and validating. Throws
    // ConfigurationValidationException on any structural or semantic error -
    // callers decide whether that means "keep the previous snapshot" (reload)
    // or "fail startup" (initial load), per the documented architecture.
    public static ConfigurationSnapshot Load(string path, IReadOnlyCollection<SkillId>? knownSkillIds, ILogger? logger = null)
    {
        if (!File.Exists(path))
        {
            logger?.LogInformation("HeroShift configuration file '{Path}' does not exist; using canonical defaults.", path);
            return CreateValidated(new HeroShiftConfiguration(), knownSkillIds);
        }

        string json;
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs))
            json = sr.ReadToEnd();

        return LoadFromJson(json, knownSkillIds);
    }

    public static ConfigurationSnapshot LoadFromJson(string json, IReadOnlyCollection<SkillId>? knownSkillIds)
    {
        var errors = new List<string>();

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ConfigurationValidationException([$"<root>: malformed JSON - {ex.Message}"]);
        }

        foreach (var property in root.Properties())
            if (!KnownRootSections.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                errors.Add($"{property.Name}: unknown configuration section");

        var dto = root.ToObject<ConfigurationOverrideDto>() ?? new ConfigurationOverrideDto();

        var configuration = new HeroShiftConfiguration
        {
            SchemaVersion = dto.SchemaVersion ?? 1,
            General = NormalizeGeneral(MergeSection<GeneralOptions>(dto.General, "general", errors)),
            Hud = MergeSection<HudOptions>(dto.Hud, "hud", errors),
            Chat = MergeSection<ChatOptions>(dto.Chat, "chat", errors),
            Commands = NormalizeCommands(MergeSection<CommandOptions>(dto.Commands, "commands", errors)),
            Voting = NormalizeVoting(MergeSection<VotingOptions>(dto.Voting, "voting", errors)),
            Skills = MergeSkillOverrides(dto.Skills, errors),
        };

        if (errors.Count > 0)
            throw new ConfigurationValidationException(errors);

        return CreateValidated(configuration, knownSkillIds);
    }

    private static GeneralOptions NormalizeGeneral(GeneralOptions general) =>
        general.DisplayAlwaysDescription
            ? general with { SkillDescriptionDuration = 9999f }
            : general;

    private static IReadOnlyList<string> NormalizeAliases(IReadOnlyList<string>? aliases) =>
        aliases?.Select(alias => alias?.Trim().ToLowerInvariant() ?? string.Empty).ToArray() ?? [];

    private static CommandDefinition NormalizeCommand(CommandDefinition command) =>
        command with { Aliases = NormalizeAliases(command.Aliases) };

    private static CommandOptions NormalizeCommands(CommandOptions commands) => commands with
    {
        SetSkillCommand = NormalizeCommand(commands.SetSkillCommand),
        SkillsListCommand = NormalizeCommand(commands.SkillsListCommand),
        UseSkillCommand = NormalizeCommand(commands.UseSkillCommand),
        HealCommand = NormalizeCommand(commands.HealCommand),
        HealthCommand = NormalizeCommand(commands.HealthCommand),
        PlantedBomb = NormalizeCommand(commands.PlantedBomb),
        BotPlace = NormalizeCommand(commands.BotPlace),
        ConsoleCommand = NormalizeCommand(commands.ConsoleCommand),
        HudCommand = NormalizeCommand(commands.HudCommand),
        SetStaticSkillCommand = NormalizeCommand(commands.SetStaticSkillCommand),
        ReloadCommand = NormalizeCommand(commands.ReloadCommand),
        NextCommand = NormalizeCommand(commands.NextCommand),
        CheckEntityCommand = NormalizeCommand(commands.CheckEntityCommand),
    };

    private static VotingCommandDefinition NormalizeVotingCommand(VotingCommandDefinition command) =>
        command with { Aliases = NormalizeAliases(command.Aliases) };

    private static VotingOptions NormalizeVoting(VotingOptions voting) => voting with
    {
        StartGameCommand = voting.StartGameCommand with { Aliases = NormalizeAliases(voting.StartGameCommand.Aliases) },
        ChangeMapCommand = NormalizeVotingCommand(voting.ChangeMapCommand),
        SwapCommand = NormalizeVotingCommand(voting.SwapCommand),
        ShuffleCommand = NormalizeVotingCommand(voting.ShuffleCommand),
        PauseCommand = NormalizeVotingCommand(voting.PauseCommand),
        SetScoreCommand = NormalizeVotingCommand(voting.SetScoreCommand),
    };

    private static T MergeSection<T>(JObject? section, string sectionName, List<string> errors) where T : class, new()
    {
        foreach (var unknown in JsonMerge.FindUnknownProperties<T>(section))
            errors.Add($"{sectionName}.{unknown}: unknown field");

        try
        {
            return JsonMerge.MergeOnto(section, new T());
        }
        catch (JsonException ex)
        {
            errors.Add($"{sectionName}: {ex.Message}");
            return new T();
        }
    }

    private static IReadOnlyDictionary<SkillId, SkillOverride> MergeSkillOverrides(JObject? skillsSection, List<string> errors)
    {
        var result = new Dictionary<SkillId, SkillOverride>();
        if (skillsSection == null) return result;

        foreach (var property in skillsSection.Properties())
        {
            if (!SkillId.TryParse(property.Name, out var id))
            {
                errors.Add($"skills.{property.Name}: invalid skill ID");
                continue;
            }

            if (property.Value is not JObject overrideObject)
            {
                errors.Add($"skills.{property.Name}: expected an object");
                continue;
            }

            foreach (var unknown in JsonMerge.FindUnknownProperties<SkillOverride>(overrideObject))
                errors.Add($"skills.{property.Name}.{unknown}: unknown field");

            try
            {
                result[id] = overrideObject.ToObject<SkillOverride>() ?? new SkillOverride();
            }
            catch (JsonException ex)
            {
                errors.Add($"skills.{property.Name}: {ex.Message}");
            }
        }

        return result;
    }

    private static ConfigurationSnapshot CreateValidated(HeroShiftConfiguration configuration, IReadOnlyCollection<SkillId>? knownSkillIds)
    {
        ConfigurationValidator.ValidateAndThrow(configuration, knownSkillIds);
        return new ConfigurationSnapshot(configuration);
    }
}
