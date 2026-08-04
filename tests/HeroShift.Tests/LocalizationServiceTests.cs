using src.LocalizationCore;

namespace HeroShift.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void GetTranslation_EmbeddedEnglishLoadsWithoutExternalFile()
    {
        var service = new LocalizationService(externalLanguagePath: null, alternativeSkillButton: null);

        Assert.Equal("None", service.GetTranslation("none"));
        Assert.Equal("You have no skill", service.GetTranslation("none_desc"));
    }

    [Fact]
    public void GetTranslation_UnknownKey_ReturnsKeyItself()
    {
        var service = new LocalizationService(externalLanguagePath: null, alternativeSkillButton: null);

        Assert.Equal("this_key_does_not_exist", service.GetTranslation("this_key_does_not_exist"));
    }

    [Fact]
    public void GetTranslation_FormatsPlaceholderArguments()
    {
        var service = new LocalizationService(externalLanguagePath: null, alternativeSkillButton: null);

        // areareaper_site_disabled = "Bomb site {0} has been deactivated - no bombs can be planted there!"
        var result = service.GetTranslation("areareaper_site_disabled", null, "A");
        Assert.Contains("Bomb site A has been deactivated", result);
    }

    [Fact]
    public void GetTranslation_WelcomeSentinel_ReturnsUnformattedText()
    {
        var service = new LocalizationService(externalLanguagePath: null, alternativeSkillButton: null);

        // Passing the "welcome" sentinel must return the raw translation with
        // its {PLAYER}-style placeholder intact, not run through string.Format
        // (which would throw on an unrecognized/extra placeholder).
        var result = service.GetTranslation("welcome_message", null, "welcome");
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void Reload_ExternalFileOverridesEmbeddedEnglishForSameKey()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"heroshift-lang-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempPath, """{ "none": "OverriddenNone" }""");

            var service = new LocalizationService(externalLanguagePath: tempPath, alternativeSkillButton: null);

            Assert.Equal("OverriddenNone", service.GetTranslation("none"));
            // Keys absent from the external file still fall back to embedded English.
            Assert.Equal("You have no skill", service.GetTranslation("none_desc"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Reload_MissingExternalFile_FallsBackToEmbeddedEnglishOnly()
    {
        var service = new LocalizationService(externalLanguagePath: "Z:\\definitely\\does\\not\\exist.json", alternativeSkillButton: null);

        Assert.Equal("None", service.GetTranslation("none"));
    }

    [Fact]
    public void AlternativeSkillButton_AppendsBindToCssUseSkillMentions()
    {
        var service = new LocalizationService(externalLanguagePath: null, alternativeSkillButton: "e");

        // aimlock_desc = "Click [css_useSkill] to lock your aim on the nearest enemy"
        var result = service.GetTranslation("aimlock_desc");
        Assert.Contains("css_useSkill/e", result);
    }

    [Fact]
    public void Reload_ExternalUnknownKey_FailsValidation()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"heroshift-lang-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempPath, """{ "not_in_english": "value" }""");
            Assert.Throws<InvalidDataException>(() =>
                new LocalizationService(externalLanguagePath: tempPath, alternativeSkillButton: null));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Reload_ExternalPlaceholderMismatch_FailsValidation()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"heroshift-lang-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempPath, """{ "areareaper_site_disabled": "Site disabled" }""");
            Assert.Throws<InvalidDataException>(() =>
                new LocalizationService(externalLanguagePath: tempPath, alternativeSkillButton: null));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
