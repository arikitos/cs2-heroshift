using src.Configuration;
using src.Configuration.Models;
using src.SkillsCore.Abstractions;

namespace HeroShift.Tests;

public class ConfigurationLoaderTests
{
    [Fact]
    public void LoadFromJson_EmptyObject_ProducesAllDefaults()
    {
        var snapshot = ConfigurationLoader.LoadFromJson("{}", null);

        Assert.Equal(1, snapshot.Configuration.SchemaVersion);
        Assert.Equal(GameMode.NoRepeat, snapshot.Configuration.General.GameMode);
        Assert.Equal(7f, snapshot.Configuration.General.SkillTimeBeforeStart);
        Assert.Empty(snapshot.Configuration.Skills);
    }

    [Fact]
    public void LoadFromJson_PartialGeneralOverride_KeepsUntouchedFieldsAtDefault()
    {
        const string json = """
        {
          "general": { "gameMode": 0, "debugMode": true }
        }
        """;

        var snapshot = ConfigurationLoader.LoadFromJson(json, null);

        Assert.Equal(GameMode.Normal, snapshot.Configuration.General.GameMode);
        Assert.True(snapshot.Configuration.General.DebugMode);
        // Untouched field must still carry its code default.
        Assert.True(snapshot.Configuration.General.EnableBotSkills);
        Assert.Equal(7f, snapshot.Configuration.General.SkillTimeBeforeStart);
    }

    [Fact]
    public void LoadFromJson_UnknownRootSection_Throws()
    {
        const string json = """{ "totallyUnknownSection": {} }""";

        var ex = Assert.Throws<ConfigurationValidationException>(() => ConfigurationLoader.LoadFromJson(json, null));
        Assert.Contains(ex.Errors, e => e.Contains("totallyUnknownSection") && e.Contains("unknown configuration section"));
    }

    [Fact]
    public void LoadFromJson_UnknownFieldInSection_Throws()
    {
        const string json = """
        {
          "general": { "thisFieldDoesNotExist": true }
        }
        """;

        var ex = Assert.Throws<ConfigurationValidationException>(() => ConfigurationLoader.LoadFromJson(json, null));
        Assert.Contains(ex.Errors, e => e.Contains("general.thisFieldDoesNotExist") && e.Contains("unknown field"));
    }

    [Fact]
    public void LoadFromJson_UnsupportedSchemaVersion_Throws()
    {
        const string json = """{ "schemaVersion": 99 }""";

        var ex = Assert.Throws<ConfigurationValidationException>(() => ConfigurationLoader.LoadFromJson(json, null));
        Assert.Contains(ex.Errors, e => e.Contains("schemaVersion"));
    }

    [Fact]
    public void LoadFromJson_SkillOverride_ParsesIntoTypedDictionaryKeyedBySkillId()
    {
        const string json = """
        {
          "skills": {
            "dash": { "enabled": true, "options": { "cooldownSeconds": 2.5 } }
          }
        }
        """;

        var snapshot = ConfigurationLoader.LoadFromJson(json, [BuiltInSkillIds.Dash]);

        Assert.True(snapshot.Configuration.Skills.ContainsKey(BuiltInSkillIds.Dash));
        Assert.True(snapshot.Configuration.Skills[BuiltInSkillIds.Dash].Enabled);
    }

    [Fact]
    public void LoadFromJson_UnknownSkillId_ProducesValidationError()
    {
        const string json = """
        {
          "skills": {
            "totallynotaskill": { "enabled": true }
          }
        }
        """;

        var ex = Assert.Throws<ConfigurationValidationException>(() =>
            ConfigurationLoader.LoadFromJson(json, [BuiltInSkillIds.Dash]));
        Assert.Contains(ex.Errors, e => e.Contains("totallynotaskill") && e.Contains("unknown skill ID"));
    }

    [Fact]
    public void LoadFromJson_DuplicateCommandAlias_ProducesValidationError()
    {
        const string json = """
        {
          "commands": {
            "healCommand": { "aliases": ["heal", "skills"] }
          }
        }
        """;

        var ex = Assert.Throws<ConfigurationValidationException>(() => ConfigurationLoader.LoadFromJson(json, null));
        Assert.Contains(ex.Errors, e => e.Contains("already registered"));
    }

    [Fact]
    public void LoadFromJson_MalformedJson_Throws()
    {
        Assert.Throws<ConfigurationValidationException>(() => ConfigurationLoader.LoadFromJson("{ not valid json", null));
    }
}
