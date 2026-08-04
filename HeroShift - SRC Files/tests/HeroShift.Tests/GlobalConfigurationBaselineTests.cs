using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Linq;
using src.Configuration;

namespace HeroShift.Tests;

public sealed class GlobalConfigurationBaselineTests
{
    [Fact]
    public void CanonicalDefaults_MatchCompleteLegacyConfigurationBaseline()
    {
        var baseline = JObject.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "global-config-baseline.json")));
        var actual = new HeroShiftConfiguration();

        AssertObject((JObject)baseline["general"]!, actual.General, "general",
            new HashSet<string>(["configName", "gameModeEnumName"], StringComparer.OrdinalIgnoreCase));
        AssertObject((JObject)baseline["htmlHudCustomisation"]!, actual.Hud, "hud");
        AssertObject((JObject)baseline["chatMessage"]!, actual.Chat, "chat");
        AssertObject((JObject)baseline["normalCommands"]!, actual.Commands, "commands");
        AssertObject((JObject)baseline["votingCommands"]!, actual.Voting, "voting");
    }

    private static void AssertObject(JObject expected, object actual, string path, IReadOnlySet<string>? ignored = null)
    {
        var properties = actual.GetType().GetProperties().ToDictionary(
            property => property.Name,
            StringComparer.OrdinalIgnoreCase);

        foreach (var expectedProperty in expected.Properties())
        {
            if (ignored?.Contains(expectedProperty.Name) == true) continue;

            Assert.True(properties.TryGetValue(expectedProperty.Name, out var actualProperty),
                $"{path} is missing {expectedProperty.Name}");
            AssertValue(expectedProperty.Value, actualProperty!.GetValue(actual), $"{path}.{expectedProperty.Name}");
        }
    }

    private static void AssertValue(JToken expected, object? actual, string path)
    {
        if (expected.Type == JTokenType.Null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        if (expected is JObject expectedObject)
        {
            AssertObject(expectedObject, actual!, path);
            return;
        }

        if (expected is JArray expectedArray)
        {
            var values = ((IEnumerable)actual!).Cast<object?>().Select(value => value?.ToString()).ToArray();
            Assert.Equal(expectedArray.Values<string>().ToArray(), values);
            return;
        }

        if (actual is Enum enumeration)
        {
            Assert.Equal(expected.Value<int>(), Convert.ToInt32(enumeration, CultureInfo.InvariantCulture));
            return;
        }

        if (actual is char character)
        {
            Assert.Equal(expected.Value<string>(), character.ToString());
            return;
        }

        if (actual is string text)
        {
            Assert.Equal(expected.Value<string>(), text);
            return;
        }

        if (actual is bool boolean)
        {
            Assert.Equal(expected.Value<bool>(), boolean);
            return;
        }

        double expectedNumber = expected.Value<double>();
        double actualNumber = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(expectedNumber - actualNumber) < 0.0001d,
            $"{path} expected {expectedNumber} but was {actualNumber}");
    }
}
