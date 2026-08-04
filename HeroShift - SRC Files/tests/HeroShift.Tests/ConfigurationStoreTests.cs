using src.Configuration;
using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;

namespace HeroShift.Tests;

public class ConfigurationStoreTests
{
    [Fact]
    public void Initialize_BindsMetadataAndTypedOptionsFromOverrides()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
            {
              "schemaVersion": 1,
              "skills": {
                "dash": {
                  "enabled": false,
                  "color": "#123456",
                  "onlyTeam": "Terrorist",
                  "rarity": "Legendary",
                  "options": {
                    "cooldown": 5.5,
                    "jumpVelocity": 175
                  }
                }
              }
            }
            """);

            var registry = BuiltInSkillCatalog.BuildRegistry();
            var snapshot = ConfigurationStore.Initialize(path, registry);
            Assert.Same(snapshot.Configuration, ConfigurationStore.Settings);
            var dash = snapshot.Skills.Get(BuiltInSkillIds.Dash);

            Assert.False(dash.Metadata.Active);
            Assert.Equal("#123456", dash.Metadata.Color);
            Assert.Equal(CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist, dash.Metadata.OnlyTeam);
            Assert.Equal(global::src.utils.Rarity.Legendary, dash.Metadata.Rarity);

            var options = Assert.IsType<DashOptions>(dash.Options);
            Assert.Equal(5.5f, options.Cooldown);
            Assert.Equal(175f, options.JumpVelocity);
            Assert.Equal(600f, options.PushVelocity);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Initialize_RejectsUnknownTypedOption()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
            {
              "skills": {
                "dash": {
                  "options": { "notAnOption": 1 }
                }
              }
            }
            """);

            var ex = Assert.Throws<ConfigurationValidationException>(() =>
                ConfigurationStore.Initialize(path, BuiltInSkillCatalog.BuildRegistry()));

            Assert.Contains(ex.Errors, error => error.Contains("skills.dash.options.notAnOption"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReloadFailure_RetainsPreviousSnapshot()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ \"skills\": { \"dash\": { \"options\": { \"cooldown\": 4 } } } }");
            var registry = BuiltInSkillCatalog.BuildRegistry();
            var original = ConfigurationStore.Initialize(path, registry);

            File.WriteAllText(path, "{ invalid json");

            Assert.Throws<ConfigurationValidationException>(() => ConfigurationStore.Reload());
            Assert.Same(original, ConfigurationStore.Current);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
