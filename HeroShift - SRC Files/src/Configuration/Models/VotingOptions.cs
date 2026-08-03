namespace src.Configuration.Models;

/*
 * VotingCommandDefinition - one voting command's aliases, permission and
 * timing/threshold tunables. Field-for-field equivalent of the legacy
 * src/utils/Config.VotingCommand nested class.
 */
public sealed record VotingCommandDefinition
{
    public required IReadOnlyList<string> Aliases { get; init; }
    public required string Permission { get; init; }
    public bool EnableVoting { get; init; } = true;
    public float TimeToVote { get; init; }
    public float PercentagesToSuccess { get; init; }
    public float TimeToNextVoting { get; init; }
    public float TimeToNextSameVoting { get; init; }
    public int MinimumPlayersToStartVoting { get; init; }
}

/*
 * StartGameCommandDefinition - the !start command additionally carries the
 * server console commands to run for its two variants (normal vs "sv").
 * Field-for-field equivalent of the legacy src/utils/Config.StartGameCommand.
 */
public sealed record StartGameCommandDefinition
{
    public required IReadOnlyList<string> Aliases { get; init; }
    public required string Permission { get; init; }
    public bool EnableVoting { get; init; } = true;
    public required string StartParams { get; init; }
    public required string SvStartParams { get; init; }
    public float TimeToVote { get; init; }
    public float PercentagesToSuccess { get; init; }
    public float TimeToNextVoting { get; init; }
    public float TimeToNextSameVoting { get; init; }
    public int MinimumPlayersToStartVoting { get; init; }
}

/*
 * VotingOptions - field-for-field equivalent of the legacy
 * src/utils/Config.VotingCommands nested class. Default aliases, timings and
 * percentages are transcribed verbatim from that type's constructor.
 */
public sealed record VotingOptions
{
    public StartGameCommandDefinition StartGameCommand { get; init; } = new()
    {
        Aliases = ["start", "go"],
        Permission = "@HeroShift/admin",
        StartParams = "mp_freezetime 15; mp_forcecamera 0; mp_overtime_enable 1; sv_cheats 0",
        SvStartParams = "mp_freezetime 0; mp_forcecamera 0; mp_overtime_enable 1; sv_cheats 1",
        TimeToVote = 15,
        PercentagesToSuccess = 60,
        TimeToNextVoting = 15,
        TimeToNextSameVoting = 500,
        MinimumPlayersToStartVoting = 2,
    };

    public VotingCommandDefinition ChangeMapCommand { get; init; } = new()
    {
        Aliases = ["map", "changemap"],
        Permission = "@HeroShift/admin",
        TimeToVote = 25,
        PercentagesToSuccess = 90,
        TimeToNextVoting = 15,
        TimeToNextSameVoting = 500,
        MinimumPlayersToStartVoting = 2,
    };

    public VotingCommandDefinition SwapCommand { get; init; } = new()
    {
        Aliases = ["swap"],
        Permission = "@HeroShift/admin",
        TimeToVote = 15,
        PercentagesToSuccess = 90,
        TimeToNextVoting = 15,
        TimeToNextSameVoting = 20,
        MinimumPlayersToStartVoting = 2,
    };

    public VotingCommandDefinition ShuffleCommand { get; init; } = new()
    {
        Aliases = ["shuffle"],
        Permission = "@HeroShift/admin",
        TimeToVote = 15,
        PercentagesToSuccess = 90,
        TimeToNextVoting = 15,
        TimeToNextSameVoting = 20,
        MinimumPlayersToStartVoting = 2,
    };

    public VotingCommandDefinition PauseCommand { get; init; } = new()
    {
        Aliases = ["pause", "unpause"],
        Permission = "@HeroShift/admin",
        TimeToVote = 15,
        PercentagesToSuccess = 60,
        TimeToNextVoting = 15,
        TimeToNextSameVoting = 2,
        MinimumPlayersToStartVoting = 2,
    };

    public VotingCommandDefinition SetScoreCommand { get; init; } = new()
    {
        Aliases = ["setscore"],
        Permission = "@HeroShift/owner",
        TimeToVote = 15,
        PercentagesToSuccess = 90,
        TimeToNextVoting = 15,
        TimeToNextSameVoting = 90,
        MinimumPlayersToStartVoting = 2,
    };
}
