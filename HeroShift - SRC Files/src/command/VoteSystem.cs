using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace src.command
{
    /*
     * VoteSystem - lets ordinary players trigger the admin-only match commands
     * (start, pause, shuffle, swap, changemap, setscore) by majority vote.
     *
     * Entry point is the extension method player.Vote(voteType, args), called from
     * the Command_X handlers in Command.cs when the caller lacks the command's
     * admin permission but the command's EnableVoting flag is set. The first
     * caller implicitly creates the vote and counts as its first voter; later
     * callers just add themselves to it.
     *
     * A vote is identified by (Type, Args), so "!map de_dust2" and "!map de_nuke"
     * are separate votes, while two players typing the same thing join one vote.
     *
     * Thresholds and timings are hardcoded in CreateVote():
     *   TimeToVote                 10s  - how long the vote stays open
     *   PercentagesToSuccess       60   - percent of humans needed to pass
     *   TimeToNextVoting            2s  - cooldown before any new vote
     *   TimeToNextSameVoting        5s  - cooldown before the same vote type again
     *   MinimumPlayersToStartVoting  2  - humans required for a vote to exist at all
     * Note that The typed voting command entries define their own
     * TimeToVote / PercentagesToSuccess / TimeToNextVoting / TimeToNextSameVoting /
     * MinimumPlayersToStartVoting values, but CreateVote() does not read them - only
     * EnableVoting, aliases and permission from config actually affect voting today.
     * Bots never count: every headcount uses Count(p => !p.IsBot).
     *
     * When the threshold is reached the vote's SuccessAction runs, which simply
     * executes the matching console command string from VoteTypeCommands (e.g.
     * "css_shuffle") - so the command re-enters Command.cs, this time from the
     * server console where player == null and the permission check is skipped.
     * That is why the plain X() worker methods in Command.cs must stay callable
     * with a null player.
     */
    public static class VoteSystem
    {
        private static readonly ConcurrentDictionary<VoteData, byte> votes = [];

        // (Re)arms the vote's expiry timer. Called once when the vote opens and again
        // after every counted vote, so the window is effectively extended by each new
        // voter rather than being a fixed deadline from vote start. On expiry the vote
        // is closed and its same-type cooldown is set.
        private static void StartVoteTimer(VoteData vote, string commandName)
        {
            vote.ActiveTimer?.Kill();

            vote.ActiveTimer = HeroShift.Instance.AddTimer(vote.TimeToVote, () =>
            {
                if (!votes.ContainsKey(vote) || !vote.GetActive()) return;

                vote.SetActive(false);
                // On timeout the same-type cooldown is overwritten with the general
                // cooldown value (5s -> 2s with the current constants). Since
                // NextSameVoting() is computed from this field, a timed-out vote's
                // same-type cooldown effectively shortens.
                vote.TimeToNextSameVoting = vote.TimeToNextVoting;
                Localization.PrintTranslationToChatAll($" {ChatColors.Red}{{0}}", ["vote_timeout"], [commandName]);
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Opens a new vote, announces it and plays a UI sound for everyone.
        // Returns null (and registers nothing) when there are too few humans on the
        // server, which the caller reports as "not enough players".
        //
        // Constructor arguments in order: timeToVote 10s, the success action,
        // percentagesToSuccess 60, timeToNextVoting 2s, timeToNextSameVoting 5s,
        // minimumPlayersToStartVoting 2, then the vote type and its argument string.
        private static VoteData? CreateVote(VoteType voteType, string? args = null)
        {
            var vote = new VoteData(10,
                () => {
                    Server.ExecuteCommand($"{VoteTypeCommands.GetCommand(voteType)}{(!string.IsNullOrEmpty(args) ? $" {args}" : "")}");
                }, 60, 2, 5, 2, voteType, args);

            if (vote.MinimumPlayersToStartVoting > Utilities.GetPlayers().Count(p => !p.IsBot))
                return null;

            votes.TryAdd(vote, 0);
            // Turns the internal "css_shuffle" back into the "!shuffle" form players
            // type, so the chat prompt tells them exactly what to repeat to join in.
            string commandName = $"!{VoteTypeCommands.GetCommand(vote.Type)?.Replace("css_", "")}{(!string.IsNullOrEmpty(vote?.Args) ? $" {vote?.Args}" : "")}";

            Localization.PrintTranslationToChatAll($" {ChatColors.Lime}{{0}}", ["vote_started"], [commandName]);
            foreach (var player in Utilities.GetPlayers())
                player.EmitSound("UIPanorama.tab_mainmenu_news");

            StartVoteTimer(vote!, commandName);

            // The vote object has to outlive the voting window itself, because its
            // CreatedTime is what the cooldown checks in Vote() read. It is therefore
            // only removed once the last of the three deadlines has passed.
            float[] times = [vote!.TimeToVote, vote.TimeToVote + vote.TimeToNextVoting, vote.TimeToVote + vote.TimeToNextSameVoting];
            HeroShift.Instance.AddTimer(times.Max(), () =>
            {
                if (!votes.ContainsKey(vote)) return;
                votes.TryRemove(vote, out _);
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

            return vote;
        }

        // Extension method used by Command.cs: casts one player's vote, creating the
        // vote first if no matching one is open. Cooldowns are checked only on that
        // creation path, so joining an already-open vote is never rate-limited.
        // A player may vote once per vote (tracked by SteamID).
        public static void Vote(this CCSPlayerController player, VoteType voteType, string? args = null)
        {
            // A vote is matched on type AND args, so !map de_dust2 and !map de_nuke
            // are distinct votes rather than one shared "change map" vote.
            var vote = votes.Keys.FirstOrDefault(v => v.Type == voteType && v.Args == args && v.GetActive());
            if (vote == null)
            {
                // Global cooldown: any recent vote blocks starting a new one at all.
                if (votes.Keys.Any(v => v.NextVoting() > DateTime.Now))
                {
                    player.PrintToChat($" {ChatColors.Red}{player.GetTranslation("vote_wait")}");
                    return;
                }
                // Per-type cooldown: the same kind of vote has its own longer wait.
                else if (votes.Keys.Any(v => v.Type == voteType && v.NextSameVoting() > DateTime.Now))
                {
                    player.PrintToChat($" {ChatColors.Red}{player.GetTranslation("vote_same_wait")}");
                    return;
                }

                vote = CreateVote(voteType, args);
            }

            if (vote == null)
            {
                player.PrintToChat($" {ChatColors.Red}{player.GetTranslation("vote_not_enough_players")}");
                return;
            }

            // TryAdd doubles as the duplicate-vote check: it fails if this SteamID is
            // already in the set. The creator of the vote reaches this line too, so
            // starting a vote counts as voting for it.
            if (!vote.PlayersVoted.TryAdd(player.SteamID, 0))
                player.PrintToChat($" {ChatColors.Red}{player.GetTranslation("vote_alredy_voted")}");
            else CheckVote(vote);
        }

        // Evaluates the threshold after each new vote: passes when the number of
        // voters reaches ceil(humans * percentage / 100), otherwise re-arms the timer
        // and announces the running tally.
        private static void CheckVote(VoteData vote)
        {
            int voted = vote.PlayersVoted.Count;
            // Bots are excluded from the denominator, so a server full of bots still
            // only needs the humans present to agree.
            int playerCount = Utilities.GetPlayers().Count(p => !p.IsBot);
            // Rounded up: with 60% and 3 humans, 2 votes are required.
            int playersNeeded = (int)Math.Ceiling(playerCount * (vote.PercentagesToSuccess / 100f));
            string commandName = $"!{VoteTypeCommands.GetCommand(vote.Type)?.Replace("css_", "")}{(!string.IsNullOrEmpty(vote?.Args) ? $" {vote?.Args}" : "")}";

            if (voted >= playersNeeded)
            {
                // Kill the expiry timer before acting, otherwise the timeout message
                // would still fire for a vote that already succeeded.
                vote!.ActiveTimer?.Kill();
                // Executes the console command for this vote type, which re-enters the
                // Command.cs handler as a server (player == null) call.
                vote.SuccessAction.Invoke();
                vote.SetActive(false);
            }
            else
            {
                StartVoteTimer(vote!, commandName);
                Localization.PrintTranslationToChatAll($" {ChatColors.Yellow}{{0}} '': {ChatColors.Green}{voted}/{playersNeeded}", ["vote_vote"]);
            }
        }
    }

    /*
     * VoteData - the state of one in-flight vote (primary-constructor class).
     *
     * Active is private with SetActive/GetActive accessors: an inactive vote no
     * longer accepts votes, but the object deliberately stays in the dictionary
     * afterwards because its CreatedTime still drives the cooldown checks.
     * Both NextVoting() and NextSameVoting() are absolute wall-clock deadlines
     * measured from CreatedTime, not remaining durations.
     */
    public class VoteData(float timeToVote, Action successAction, float percentagesToSuccess, float timeToNextVoting, float timeToNextSameVoting, int minimumPlayersToStartVoting, VoteType type, string? args = null)
    {
        private bool Active { get; set; } = true;
        public float TimeToVote { get; set; } = timeToVote;
        public Action SuccessAction { get; set; } = successAction;
        public float PercentagesToSuccess { get; set; } = percentagesToSuccess;
        public float TimeToNextVoting { get; set; } = timeToNextVoting;
        public float TimeToNextSameVoting { get; set; } = timeToNextSameVoting;
        public int MinimumPlayersToStartVoting { get; set; } = minimumPlayersToStartVoting;
        public VoteType Type { get; set; } = type;
        public string? Args { get; set; } = args;
        // Used as a set of SteamIDs; the byte value is ignored.
        public ConcurrentDictionary<ulong, byte> PlayersVoted { get; set; } = [];
        public Timer? ActiveTimer { get; set; }
        private DateTime CreatedTime { get; set; } = DateTime.Now;

        public void SetActive(bool active)
        {
            Active = active;
        }

        public bool GetActive()
        {
            return Active;
        }

        // Wall-clock time after which ANY new vote may be started.
        public DateTime NextVoting()
        {
            return CreatedTime.AddSeconds(TimeToVote + TimeToNextVoting);
        }

        // Wall-clock time after which a vote of THIS type may be started again.
        public DateTime NextSameVoting()
        {
            return CreatedTime.AddSeconds(TimeToVote + TimeToNextSameVoting);
        }
    }

    public enum VoteType
    {
        StartGame,
        PauseGame,
        ShuffleTeam,
        SwapTeam,
        ChangeMap,
        SetScore,
    }

    /*
     * VoteTypeCommands - maps each VoteType to the console command a successful
     * vote executes.
     *
     * These are the built-in "css_*" names, NOT the configurable aliases from
     * heroshift.json. That works because Command.Load() always registers each
     * handler under every configured alias, and these canonical names are defaults; if an alias list is changed so that the default name is no longer
     * registered, the corresponding vote would execute a command that no longer
     * exists. The same strings are also reversed into the "!name" hint shown in
     * chat.
     */
    public static class VoteTypeCommands
    {
        private static readonly ConcurrentDictionary<VoteType, string> names = new(
        [
            new KeyValuePair<VoteType, string>(VoteType.StartGame, "css_start"),
            new KeyValuePair<VoteType, string>(VoteType.PauseGame, "css_pause"),
            new KeyValuePair<VoteType, string>(VoteType.ShuffleTeam, "css_shuffle"),
            new KeyValuePair<VoteType, string>(VoteType.SwapTeam, "css_swap"),
            new KeyValuePair<VoteType, string>(VoteType.ChangeMap, "css_map"),
            new KeyValuePair<VoteType, string>(VoteType.SetScore, "css_setscore"),
        ]);

        public static string GetCommand(VoteType type) => names[type];
    }
}
