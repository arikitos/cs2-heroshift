using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using src.Configuration.Models;
using src.player;
using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.utils;

namespace src.Configuration;

public static class ConfigurationStore
{
    private static readonly object Sync = new();
    private static RuntimeConfigurationSnapshot? _current;
    private static string? _path;
    private static SkillRegistry? _registry;
    private static ILogger? _logger;

    public static RuntimeConfigurationSnapshot Current =>
        Volatile.Read(ref _current)
        ?? throw new InvalidOperationException("HeroShift configuration has not been initialized.");

    public static HeroShiftConfiguration Settings => Current.Configuration;
    public static string EffectivePath => _path
        ?? throw new InvalidOperationException("HeroShift configuration has not been initialized.");

    public static RuntimeConfigurationSnapshot Initialize(
        string path,
        SkillRegistry registry,
        ILogger? logger = null,
        Action<RuntimeConfigurationSnapshot>? validateBeforePublish = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(registry);

        lock (Sync)
        {
            string? previousPath = _path;
            SkillRegistry? previousRegistry = _registry;
            ILogger? previousLogger = _logger;
            _path = path;
            _registry = registry;
            _logger = logger;

            try
            {
                var snapshot = Load(path, registry, logger);
                validateBeforePublish?.Invoke(snapshot);
                Publish(snapshot);
                return snapshot;
            }
            catch
            {
                _path = previousPath;
                _registry = previousRegistry;
                _logger = previousLogger;
                throw;
            }
        }
    }

    public static RuntimeConfigurationSnapshot Reload(Action<RuntimeConfigurationSnapshot>? validateBeforePublish = null)
    {
        lock (Sync)
        {
            if (_path == null || _registry == null)
                throw new InvalidOperationException("HeroShift configuration has not been initialized.");

            try
            {
                var snapshot = Load(_path, _registry, _logger);
                validateBeforePublish?.Invoke(snapshot);
                Publish(snapshot);
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "HeroShift configuration reload failed; retaining the previous valid snapshot.");
                throw;
            }
        }
    }

    public static void Reset()
    {
        lock (Sync)
        {
            Volatile.Write(ref _current, null);
            _path = null;
            _registry = null;
            _logger = null;
            SkillRuntime.Reset();
        }
    }

    internal static RuntimeConfigurationSnapshot Build(HeroShiftConfiguration configuration, SkillRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(registry);

        var errors = new List<string>();
        var effective = new List<EffectiveSkillConfiguration>(registry.All.Count);

        foreach (var definition in registry.All)
        {
            configuration.Skills.TryGetValue(definition.Id, out var @override);
            var metadata = MergeMetadata(definition, @override, errors);
            var options = MergeOptions(definition, @override?.Options, errors);
            effective.Add(new EffectiveSkillConfiguration(definition.Id, metadata, options));
        }

        if (errors.Count > 0)
            throw new ConfigurationValidationException(errors);

        return new RuntimeConfigurationSnapshot(
            configuration,
            new EffectiveSkillConfigurationCollection(effective));
    }

    private static RuntimeConfigurationSnapshot Load(string path, SkillRegistry registry, ILogger? logger)
    {
        var configuration = ConfigurationLoader.Load(path, registry.All.Select(definition => definition.Id).ToArray(), logger).Configuration;
        return Build(configuration, registry);
    }

    private static void Publish(RuntimeConfigurationSnapshot snapshot)
    {
        Volatile.Write(ref _current, snapshot);
        SkillRuntime.SetSnapshot(snapshot);
    }

    private static SkillMetadata MergeMetadata(
        SkillDefinition definition,
        SkillOverride? @override,
        List<string> errors)
    {
        if (@override == null)
            return definition.Metadata;

        var onlyTeam = definition.Metadata.OnlyTeam;
        if (@override.OnlyTeam != null && !Enum.TryParse(@override.OnlyTeam, true, out onlyTeam))
            errors.Add($"skills.{definition.Id}.onlyTeam: unknown team '{@override.OnlyTeam}'");

        var rarity = definition.Metadata.Rarity;
        if (@override.Rarity != null && !Enum.TryParse(@override.Rarity, true, out rarity))
            errors.Add($"skills.{definition.Id}.rarity: unknown rarity '{@override.Rarity}'");

        return definition.Metadata with
        {
            Active = @override.Enabled ?? definition.Metadata.Active,
            Color = @override.Color ?? definition.Metadata.Color,
            OnlyTeam = onlyTeam,
            DisableOnFreezeTime = @override.DisableOnFreezeTime ?? definition.Metadata.DisableOnFreezeTime,
            NeedsTeammates = @override.NeedsTeammates ?? definition.Metadata.NeedsTeammates,
            RequiredPermission = @override.RequiredPermission ?? definition.Metadata.RequiredPermission,
            HudDuration = @override.HudDuration ?? definition.Metadata.HudDuration,
            DescriptionHudDuration = @override.DescriptionHudDuration ?? definition.Metadata.DescriptionHudDuration,
            MaxPerServer = @override.MaxPerServer ?? definition.Metadata.MaxPerServer,
            Rarity = rarity,
        };
    }

    private static ISkillOptions MergeOptions(
        SkillDefinition definition,
        Newtonsoft.Json.Linq.JObject? optionOverrides,
        List<string> errors)
    {
        errors.AddRange(SkillOptionValidator.Validate(definition.Id, optionOverrides));

        foreach (var unknown in JsonMerge.FindUnknownProperties(optionOverrides, definition.DefaultOptionsBoxed.GetType()))
            errors.Add($"skills.{definition.Id}.options.{unknown}: unknown field");

        if (optionOverrides == null)
        {
            errors.AddRange(definition.ValidateOptionsBoxed(definition.DefaultOptionsBoxed)
                .Select(error => $"skills.{definition.Id}.options: {error}"));
            return definition.DefaultOptionsBoxed;
        }

        try
        {
            var merged = (ISkillOptions)JsonMerge.MergeOnto(
                optionOverrides,
                definition.DefaultOptionsBoxed,
                definition.DefaultOptionsBoxed.GetType());
            errors.AddRange(definition.ValidateOptionsBoxed(merged)
                .Select(error => $"skills.{definition.Id}.options: {error}"));
            return merged;
        }
        catch (JsonException ex)
        {
            errors.Add($"skills.{definition.Id}.options: {ex.Message}");
            return definition.DefaultOptionsBoxed;
        }
    }

}
