using src.SkillsCore;
using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;

namespace HeroShift.Tests;

public class SkillConfigurationResolverTests
{
    [Fact]
    public void Get_ReturnsRegisteredSnapshotOptions()
    {
        var options = new DashOptions { Cooldown = 5f };
        SkillConfigurationResolver.SetSnapshot(new Dictionary<SkillId, ISkillOptions> { [BuiltInSkillIds.Dash] = options });

        var resolved = SkillConfigurationResolver.Get<DashOptions>(BuiltInSkillIds.Dash);

        Assert.Equal(5f, resolved.Cooldown);
    }

    [Fact]
    public void Get_UnregisteredSkill_Throws()
    {
        SkillConfigurationResolver.SetSnapshot(new Dictionary<SkillId, ISkillOptions>());

        Assert.Throws<InvalidOperationException>(() => SkillConfigurationResolver.Get<DashOptions>(BuiltInSkillIds.Dash));
    }

    [Fact]
    public void Get_WrongOptionsType_Throws()
    {
        SkillConfigurationResolver.SetSnapshot(new Dictionary<SkillId, ISkillOptions> { [BuiltInSkillIds.Dash] = NoSkillOptions.Instance });

        Assert.Throws<InvalidOperationException>(() => SkillConfigurationResolver.Get<DashOptions>(BuiltInSkillIds.Dash));
    }
}
