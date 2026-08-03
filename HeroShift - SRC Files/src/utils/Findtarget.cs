using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using static src.HeroShift;

namespace src.utils
{
    /*
     * FindTarget - resolves the target argument of a command into a concrete player
     * list plus a human-readable name for chat messages.
     *
     * It wraps CounterStrikeSharp's own targeting, so it accepts the standard
     * target syntax rather than just a plain name: an individual player, plus the
     * @-groups (@all, @bots, @humans, @alive, @dead, @!me, @ct, @t, @spec). The
     * returned targetname is what you print back to chat - for a single player it
     * is their name, and for a group it is the localized label for that group
     * ("all", "bots", "ct", ...) taken from Instance.Localizer rather than from
     * Localization/en.json.
     *
     * Two behaviours worth knowing:
     *   - singletarget: true rejects a multi-player match outright (used by
     *     commands that only make sense against one person) and reports
     *     "duplicate_player".
     *   - On any failure it returns an EMPTY list, never null, so callers can
     *     simply check players.Count. ignoreMessage: true suppresses the
     *     "no_player" reply for silent lookups.
     *
     * NOTE: nothing in the plugin currently calls this class - the admin commands
     * in Command.cs resolve their targets by comparing SteamID/PlayerName by hand
     * instead. It is available if a hero or command wants proper @-group targeting.
     */
    public class FindTarget
    {
        // Resolves argument 1 of the command as a target specifier.
        // minArgCount guards against reading a missing argument; singletarget forbids
        // multi-player matches; ignoreMessage silences the "no player found" reply.
        public static (List<CCSPlayerController> players, string targetname) Find
            (
                CommandInfo command,
                int minArgCount,
                bool singletarget,
                bool ignoreMessage = false
            )
        {
            if (command.ArgCount < minArgCount)
            {
                return (new List<CCSPlayerController>(), string.Empty);
            }

            // Does the actual parsing: handles both a single player and the @-groups.
            TargetResult targetresult = command.GetArgTargetResult(1);

            if (targetresult.Players.Count == 0)
            {
                if (!ignoreMessage && command.CallingPlayer != null)
                    command.ReplyToCommand(command.CallingPlayer.GetTranslation("no_player"));

                return (new List<CCSPlayerController>(), string.Empty);
            }
            else if (singletarget && targetresult.Players.Count > 1)
            {
                if (command.CallingPlayer != null)
                    command.ReplyToCommand(command.CallingPlayer.GetTranslation("duplicate_player"));
                return (new List<CCSPlayerController>(), string.Empty);
            }

            string targetname;

            if (targetresult.Players.Count == 1)
            {
                targetname = targetresult.Players.Single().PlayerName;
            }
            else
            {
                // Multiple players matched, so the display name has to describe the group
                // instead of a person. The raw argument is mapped back to its TargetType to
                // pick the right localized label.
                Target.TargetTypeMap.TryGetValue(command.GetArg(1), out TargetType type);

                targetname = type switch
                {
                    TargetType.GroupAll => Instance.Localizer["all"],
                    TargetType.GroupBots => Instance.Localizer["bots"],
                    TargetType.GroupHumans => Instance.Localizer["humans"],
                    TargetType.GroupAlive => Instance.Localizer["alive"],
                    TargetType.GroupDead => Instance.Localizer["dead"],
                    TargetType.GroupNotMe => Instance.Localizer["notme"],
                    TargetType.PlayerMe => targetresult.Players.First().PlayerName,
                    TargetType.TeamCt => Instance.Localizer["ct"],
                    TargetType.TeamT => Instance.Localizer["t"],
                    TargetType.TeamSpec => Instance.Localizer["spec"],
                    _ => targetresult.Players.First().PlayerName
                };
            }

            return (targetresult.Players, targetname);
        }
    }
}