using src.SkillsCore.Abstractions;

namespace HeroShift.Tests;

public class SkillIdTests
{
    [Theory]
    [InlineData("Dash", "dash")]
    [InlineData("DASH", "dash")]
    [InlineData("  dash  ", "dash")]
    [InlineData("AimLock", "aimlock")]
    public void Create_NormalizesToLowercaseInvariantTrimmed(string input, string expected)
    {
        var id = SkillId.Create(input);
        Assert.Equal(expected, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_RejectsEmptyOrWhitespace(string? input)
    {
        Assert.Throws<ArgumentException>(() => SkillId.Create(input!));
    }

    [Fact]
    public void TryParse_IsCaseInsensitiveAndNeverThrows()
    {
        Assert.True(SkillId.TryParse("KillerFlash", out var id));
        Assert.Equal("killerflash", id.Value);

        Assert.False(SkillId.TryParse(null, out _));
        Assert.False(SkillId.TryParse("   ", out _));
    }

    [Fact]
    public void Equality_IsCaseInsensitiveByConstruction()
    {
        Assert.Equal(SkillId.Create("Dash"), SkillId.Create("DASH"));
    }

    [Fact]
    public void BuiltInSkillIds_All_ContainsExactly146EntriesWithNoDuplicates()
    {
        Assert.Equal(146, BuiltInSkillIds.All.Count);
        Assert.Equal(BuiltInSkillIds.All.Count, BuiltInSkillIds.All.Distinct().Count());
    }

    [Fact]
    public void BuiltInSkillIds_ContainsNoneAndDash()
    {
        Assert.Contains(BuiltInSkillIds.None, BuiltInSkillIds.All);
        Assert.Contains(BuiltInSkillIds.Dash, BuiltInSkillIds.All);
        Assert.Equal("none", BuiltInSkillIds.None.Value);
        Assert.Equal("dash", BuiltInSkillIds.Dash.Value);
    }
}
