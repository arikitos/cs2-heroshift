using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using src.SkillsCore;
using src.SkillsCore.Abstractions;

namespace HeroShift.Tests;

public class BaselineEquivalenceTests
{
    [Fact]
    public void BuiltInCatalog_MatchesCompleteLegacyBaseline()
    {
        var baseline = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "baseline.json")));
        var baselineSkills = baseline["skills"]!.Values<JObject>().ToArray();
        var registry = BuiltInSkillCatalog.BuildRegistry();

        Assert.Equal(baselineSkills.Length, registry.All.Count);

        foreach (var baselineSkill in baselineSkills)
        {
            string name = baselineSkill.Value<string>("name")!;
            var id = SkillId.Create(name);
            var definition = registry.Get(id);

            AssertMetadata((JObject)baselineSkill["metadata"]!, definition.Metadata, name);
            AssertOptions((JObject)baselineSkill["options"]!, definition.DefaultOptionsBoxed, name);
            AssertHooks(baselineSkill["hooks"]!.Values<string>().ToHashSet(StringComparer.Ordinal), definition.Hooks, name);
        }
    }

    [Fact]
    public void EmbeddedEnglish_MatchesCompleteLegacyLocalizationBaseline()
    {
        var baseline = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "baseline.json")));
        var expectedEntries = baseline["localization"]!["keys"]!.Values<JObject>().ToArray();

        using var stream = typeof(BuiltInSkillCatalog).Assembly
            .GetManifestResourceStream("src.Localization.Resources.en.json");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var embedded = JObject.Parse(reader.ReadToEnd());

        Assert.Equal(expectedEntries.Length, embedded.Properties().Count());
        foreach (var expected in expectedEntries)
        {
            string key = expected.Value<string>("key")!;
            Assert.True(embedded.TryGetValue(key, out var value), $"Embedded English is missing {key}");

            var expectedPlaceholders = expected["placeholders"]!.Values<int>().ToHashSet();
            var actualPlaceholders = Regex.Matches(value!.Value<string>()!, @"\{(\d+)\}").Cast<Match>()
                .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
                .ToHashSet();
            Assert.True(expectedPlaceholders.SetEquals(actualPlaceholders),
                $"Embedded English placeholder mismatch for {key}");
        }
    }

    private static void AssertMetadata(JObject baseline, SkillMetadata actual, string skill)
    {
        AssertLiteral(baseline.Value<string>("active")!, actual.Active, skill, "active");
        AssertLiteral(baseline.Value<string>("color")!, actual.Color, skill, "color");
        AssertLiteral(baseline.Value<string>("onlyTeam")!, actual.OnlyTeam, skill, "onlyTeam");
        AssertLiteral(baseline.Value<string>("disableOnFreezeTime")!, actual.DisableOnFreezeTime, skill, "disableOnFreezeTime");
        AssertLiteral(baseline.Value<string>("needsTeammates")!, actual.NeedsTeammates, skill, "needsTeammates");
        AssertLiteral(baseline.Value<string>("requiredPermission")!, actual.RequiredPermission, skill, "requiredPermission");
        AssertLiteral(baseline.Value<string>("hudDuration")!, actual.HudDuration, skill, "hudDuration");
        AssertLiteral(baseline.Value<string>("descriptionHudDuration")!, actual.DescriptionHudDuration, skill, "descriptionHudDuration");
        AssertLiteral(baseline.Value<string>("maxPerServer")!, actual.MaxPerServer, skill, "maxPerServer");
        AssertLiteral(baseline.Value<string>("rarity")!, actual.Rarity, skill, "rarity");
    }

    private static void AssertOptions(JObject baseline, ISkillOptions actual, string skill)
    {
        var properties = actual.GetType().GetProperties().ToDictionary(
            property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..],
            StringComparer.OrdinalIgnoreCase);

        Assert.Equal(baseline.Properties().Count(), properties.Count);
        foreach (var expected in baseline.Properties())
        {
            Assert.True(properties.TryGetValue(expected.Name, out var property), $"{skill} is missing option {expected.Name}");
            AssertLiteral(expected.Value.Value<string>()!, property!.GetValue(actual), skill, expected.Name);
        }
    }

    private static void AssertHooks(HashSet<string> expected, SkillHookSet actual, string skill)
    {
        var registered = actual.GetType().GetProperties()
            .Where(property => property.GetValue(actual) != null)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(expected.SetEquals(registered),
            $"{skill} hook mismatch. Expected {string.Join(",", expected.Order())}. Actual {string.Join(",", registered.Order())}");
    }

    private static void AssertLiteral(string expectedLiteral, object? actual, string skill, string field)
    {
        if (expectedLiteral == "null")
        {
            Assert.Null(actual);
            return;
        }

        if (actual is string text)
        {
            Assert.Equal(expectedLiteral.Trim('"'), text);
            return;
        }

        if (actual is bool boolean)
        {
            Assert.Equal(bool.Parse(expectedLiteral), boolean);
            return;
        }

        if (actual is Enum enumeration)
        {
            Assert.Equal(expectedLiteral[(expectedLiteral.LastIndexOf('.') + 1)..], enumeration.ToString());
            return;
        }

        Assert.NotNull(actual);
        string normalized = expectedLiteral.TrimEnd('f', 'F', 'd', 'D', 'm', 'M');
        double expected = double.Parse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture);
        double number = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(expected - number) < 0.0001d, $"{skill}.{field} expected {expected} but was {number}");
    }
}
