using src.LocalizationCore;

namespace HeroShift.Tests;

public class TranslationValidatorTests
{
    [Fact]
    public void FindUnknownExternalKeys_ReportsKeysAbsentFromBaseline()
    {
        var baseline = TranslationCatalog.FromJson("""{ "known": "value" }""");
        var external = TranslationCatalog.FromJson("""{ "known": "value", "totallyunknown": "oops" }""");

        var unknown = TranslationValidator.FindUnknownExternalKeys(baseline, external);

        Assert.Single(unknown);
        Assert.Equal("totallyunknown", unknown[0]);
    }

    [Fact]
    public void FindUnknownExternalKeys_MissingFromExternal_IsNotReportedAsUnknown()
    {
        var baseline = TranslationCatalog.FromJson("""{ "known": "value", "onlyinbaseline": "x" }""");
        var external = TranslationCatalog.FromJson("""{ "known": "value" }""");

        Assert.Empty(TranslationValidator.FindUnknownExternalKeys(baseline, external));
    }

    [Fact]
    public void FindPlaceholderMismatches_DetectsDifferentPlaceholderSets()
    {
        var baseline = TranslationCatalog.FromJson("""{ "greeting": "Hello {0}, you have {1} skills" }""");
        var external = TranslationCatalog.FromJson("""{ "greeting": "Hola {0}" }""");

        var mismatches = TranslationValidator.FindPlaceholderMismatches(baseline, external);

        Assert.Single(mismatches);
        Assert.Equal("greeting", mismatches[0]);
    }

    [Fact]
    public void FindPlaceholderMismatches_SamePlaceholderSet_NoMismatch()
    {
        var baseline = TranslationCatalog.FromJson("""{ "greeting": "Hello {0}!" }""");
        var external = TranslationCatalog.FromJson("""{ "greeting": "Cześć {0}!" }""");

        Assert.Empty(TranslationValidator.FindPlaceholderMismatches(baseline, external));
    }

    [Fact]
    public void FindPlaceholderMismatches_IgnoresLiteralBracesThatAreNotNumberedPlaceholders()
    {
        var baseline = TranslationCatalog.FromJson("""{ "tag": "[css_useSkill]" }""");
        var external = TranslationCatalog.FromJson("""{ "tag": "[css_useSkill] override" }""");

        Assert.Empty(TranslationValidator.FindPlaceholderMismatches(baseline, external));
    }
}
