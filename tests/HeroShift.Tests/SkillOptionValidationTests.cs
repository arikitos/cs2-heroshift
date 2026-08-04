using Newtonsoft.Json.Linq;
using src.Configuration;
using src.SkillsCore;
using src.SkillsCore.Abstractions;

namespace HeroShift.Tests;

public sealed class SkillOptionValidationTests
{
    [Fact]
    public void EveryCanonicalDefault_PassesGenericAndTypedValidation()
    {
        foreach (var definition in BuiltInSkillCatalog.BuildRegistry().All)
        {
            var json = JObject.FromObject(definition.DefaultOptionsBoxed);
            Assert.Empty(SkillOptionValidator.Validate(definition.Id, json));
            Assert.Empty(definition.ValidateOptionsBoxed(definition.DefaultOptionsBoxed));
        }
    }

    [Fact]
    public void Validate_RejectsNegativeBoundedAndColorValues()
    {
        var options = JObject.Parse("""
        {
          "cooldown": -1,
          "dmgReduction": 2,
          "colorR": 256
        }
        """);

        var errors = SkillOptionValidator.Validate(SkillId.Create("test"), options);
        Assert.Contains(errors, error => error.Contains("cooldown"));
        Assert.Contains(errors, error => error.Contains("dmgReduction"));
        Assert.Contains(errors, error => error.Contains("colorR"));
    }
}
