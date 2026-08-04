using src.command;
using src.Configuration.Models;

namespace HeroShift.Tests;

public class VotePolicyTests
{
    [Theory]
    [InlineData(3, 60f, 2)]
    [InlineData(5, 90f, 5)]
    [InlineData(10, 50f, 5)]
    [InlineData(0, 60f, 0)]
    public void CalculatePlayersNeeded_RoundsUpAndUsesHumanDenominator(int humans, float percentage, int expected)
    {
        Assert.Equal(expected, VotePolicy.CalculatePlayersNeeded(humans, percentage));
    }

    [Fact]
    public void GetSettings_UsesConfiguredCommandPolicy()
    {
        var voting = new VotingOptions
        {
            PauseCommand = new VotingCommandDefinition
            {
                Aliases = ["pause"],
                Permission = string.Empty,
                TimeToVote = 9,
                PercentagesToSuccess = 75,
                TimeToNextVoting = 4,
                TimeToNextSameVoting = 12,
                MinimumPlayersToStartVoting = 3,
            },
        };

        var settings = VotePolicy.GetSettings(VoteType.PauseGame, voting);
        Assert.Equal(new VoteSettings(9, 75, 4, 12, 3), settings);
    }
}
