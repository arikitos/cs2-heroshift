using Newtonsoft.Json.Linq;
using src.SkillsCore.Abstractions;

namespace src.Configuration;

public static class SkillOptionValidator
{
    private static readonly string[] NonNegativeTerms =
    [
        "cooldown", "duration", "radius", "distance", "range", "limit", "health",
        "damage", "time", "speed", "velocity", "fuel", "amount", "heal", "price",
        "money", "strength", "strenght", "force", "width", "height", "brightness",
        "angle", "alpha", "scale", "multiplier", "jumps", "seconds", "refuell", "chance",
    ];

    private static readonly (string Minimum, string Maximum)[] OrderedPairs =
    [
        ("chanceFrom", "chanceTo"),
        ("minScale", "maxScale"),
        ("minTime", "maxTime"),
        ("minExposure", "maxExposure"),
        ("minMoney", "maxMoney"),
        ("extraJumpsMin", "extraJumpsMax"),
        ("minExtraHealth", "maxExtraHealth"),
    ];

    public static IReadOnlyList<string> Validate(SkillId id, JObject? options)
    {
        var errors = new List<string>();
        if (options == null)
            return errors;

        foreach (var property in options.Properties())
        {
            if (property.Value.Type is not (JTokenType.Integer or JTokenType.Float))
                continue;

            double value = property.Value.Value<double>();
            string path = $"skills.{id}.options.{property.Name}";
            string normalized = property.Name.ToLowerInvariant();

            if (!double.IsFinite(value))
                errors.Add($"{path}: value must be finite");
            else if (NonNegativeTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)) && value < 0)
                errors.Add($"{path}: value must be greater than or equal to 0");

            if ((normalized.Contains("percent", StringComparison.Ordinal) ||
                 normalized.Contains("reflectionchance", StringComparison.Ordinal) ||
                 normalized.Contains("dmgreduction", StringComparison.Ordinal)) &&
                value is < 0 or > 1)
                errors.Add($"{path}: value must be between 0 and 1");

            if ((normalized is "r" or "g" or "b" or "a" or "colorr" or "colorg" or "colorb") &&
                value is < 0 or > 255)
                errors.Add($"{path}: value must be between 0 and 255");
        }

        foreach (var (minimumName, maximumName) in OrderedPairs)
        {
            if (!TryGetNumber(options, minimumName, out var minimum) ||
                !TryGetNumber(options, maximumName, out var maximum))
                continue;

            if (minimum > maximum)
                errors.Add($"skills.{id}.options.{minimumName}: value must be less than or equal to {maximumName}");
        }

        return errors;
    }

    private static bool TryGetNumber(JObject options, string name, out double value)
    {
        var property = options.Properties().FirstOrDefault(candidate =>
            candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (property?.Value.Type is JTokenType.Integer or JTokenType.Float)
        {
            value = property.Value.Value<double>();
            return true;
        }

        value = default;
        return false;
    }
}
