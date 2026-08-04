using src.Players;
using src.SkillsCore.Abstractions;

namespace HeroShift.Tests;

public class PlayerStateStoreTests
{
    [Fact]
    public void Store_TracksTypedSkillStateAndReplacementByPlayerIndex()
    {
        var store = new PlayerStateStore();
        var original = new PlayerRuntimeState { IsBot = false, PlayerName = "player", PlayerIndex = 7 };
        store.Register(original);

        Assert.Same(original, store.Get(7));
        Assert.Equal(BuiltInSkillIds.None, original.Skill);

        var replacement = new PlayerRuntimeState
        {
            IsBot = true,
            PlayerName = "bot",
            PlayerIndex = 7,
            Skill = BuiltInSkillIds.Dash,
        };
        store.Register(replacement);

        Assert.Same(replacement, store.Get(7));
        Assert.Equal(1, store.CountBySkill(BuiltInSkillIds.Dash));
        Assert.True(store.Remove(7));
        Assert.Null(store.Get(7));
    }
}
