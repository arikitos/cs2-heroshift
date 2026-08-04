using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using src.menu;
using src.player;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

using src.SkillsCore;
using src.Configuration;
namespace src.command
{
    /*
     * Command - every chat/console command the plugin exposes.
     *
     * Nothing here hardcodes a command name. Load() reads the aliases out of
     * the typed runtime command/voting configuration snapshot,
     * iterates each validated alias array and registers "css_<alias>" for each
     * one. So a single handler can answer to !skills, !skill, !list, etc., and an
     * admin can rename any command without touching this file. Load() is called
     * again on !reload, which is why it first removes the previously registered
     * names it remembers in oldCommands - re-registering the same name twice
     * would leave a stale duplicate handler behind.
     *
     * Permissions come from the same config entry (e.g.
     * Commands.SetSkillCommand.Permission, a CounterStrikeSharp admin
     * flag/group string such as "@css/root"). Empty string means "no permission
     * required". Every handler therefore does the same two-part check:
     * skip the check when the string is empty, otherwise require
     * AdminManager.PlayerHasPermissions.
     *
     * The commands under the typed voting configuration behave differently for non-admins: if
     * the caller lacks the permission but EnableVoting is true, the command is
     * turned into a player vote (player.Vote(...) in VoteSystem) instead of
     * being refused. Admins bypass the vote and execute directly, which is why
     * each voting command is split into Command_X (permission/vote gate) and a
     * plain X() (the actual work, also the target of the vote's success action).
     *
     * Notes for a hero/skill author:
     *   - Command_UseTypeSkill is the entry point of the "use my skill" button.
     *     With no arguments it invokes UseSkill on the player's typed hero definition;
     *     with arguments it calls TypeSkill and hands the split arguments over.
     *     Both go through the explicit typed lifecycle coordinator, so a
     *     hero only needs `public static void UseSkill(CCSPlayerController)` or
     *     `TypeSkill(CCSPlayerController, string[])` to be reachable here.
     *   - Whenever a player's hero changes (setskill / setstaticskill / next) the
     *     old hero is disabled before the new one is enabled, and
     *     Event.UpdateSkillHudExpired refreshes the HUD. Copy that order in any
     *     new command that reassigns a skill, or the old hero keeps its hooks.
     *   - Handlers run on the game thread. player == null means the command came
     *     from the server console (RCON), which is why every message has a
     *     Server.PrintToConsole fallback using the untranslated-for-player
     *     Localization.GetTranslation instead of player.GetTranslation.
     */
    public static class Command
    {
        private static bool gamePaused = false;
        private static HeroShiftConfiguration config = ConfigurationStore.Settings;
        private static readonly ConcurrentDictionary<string, CommandInfo.CommandCallback> oldCommands = [];
        private static readonly ConcurrentDictionary<uint, int> nextSkill = [];
        private static readonly object setLock = new();

        // Registers (or re-registers, on !reload) every configured command alias.
        // setLock serialises this against Command_Reload, which mutates the same state.
        public static void Load()
        {
            config = ConfigurationStore.Settings;
            if (config == null) return;

            lock (setLock)
            {
                // Drop the previous registration set first; CounterStrikeSharp would
                // otherwise keep both the old and the new callback on the same name.
                if (!oldCommands.IsEmpty)
                {
                    foreach (var command in oldCommands)
                        Instance.RemoveCommand(command.Key, command.Value);
                    oldCommands.Clear();
                }

                // Alias list -> (console description, handler). The keys are the
                // validated alias arrays from config, not fixed command names.
                var commands = new Dictionary<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)>
                {
                    { config.Commands.SetSkillCommand.Aliases, ("Set skill", Command_SetSkill) },
                    { config.Commands.SkillsListCommand.Aliases, ("Delete all records", Command_SkillsListMenu) },
                    { config.Commands.UseSkillCommand.Aliases, ("Use/Type skill", Command_UseTypeSkill) },
                    { config.Commands.ConsoleCommand.Aliases, ("Console command", Command_CustomCommand) },
                    { config.Commands.HealCommand.Aliases, ("Heal", Command_Heal) },
                    { config.Commands.HealthCommand.Aliases, ("Set heath", Command_Health) },
                    { config.Commands.PlantedBomb.Aliases, ("Spawn planted bomb", Command_PlantedBomb) },
                    { config.Commands.BotPlace.Aliases, ("Place bot on your position", Command_BotPlace) },
                    { config.Commands.HudCommand.Aliases, ("Enable/Disable HUD", Command_HUD) },
                    { config.Commands.SetStaticSkillCommand.Aliases, ("Set static skill", Command_SetStaticSkill) },
                    { config.Commands.ReloadCommand.Aliases, ("Reaload configs", Command_Reload) },
                    { config.Commands.NextCommand.Aliases, ("Next skill", Command_Next) },
                    { config.Commands.CheckEntityCommand.Aliases, ("Check entity", Command_CheckEntity) },

                    // Voting commands: admins execute directly, everyone else votes
                    // (when the entry's EnableVoting is true).
                    { config.Voting.ChangeMapCommand.Aliases, ("Change map", Command_ChangeMap) },
                    { config.Voting.StartGameCommand.Aliases, ("Start game", Command_StartGame) },
                    { config.Voting.SwapCommand.Aliases, ("Swap team", Command_Swap) },
                    { config.Voting.ShuffleCommand.Aliases, ("Shuffle team", Command_Shuffle) },
                    { config.Voting.PauseCommand.Aliases, ("Pause game", Command_Pause) },
                    { config.Voting.SetScoreCommand.Aliases, ("Set teams score", Command_SetScore) },
                };

                foreach (var commandPair in commands)
                    foreach (var command in commandPair.Key)
                    {
                        Instance.AddCommand($"css_{command}", commandPair.Value.description, commandPair.Value.handler);
                        oldCommands.TryAdd($"css_{command}", commandPair.Value.handler);
                    }
            }
        }

        // The "use my skill" command. No arguments -> the hero's UseSkill hook;
        // with arguments -> its TypeSkill hook, receiving the arguments as string[].
        // Both are invoked through the typed lifecycle coordinator.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_UseTypeSkill(CCSPlayerController? player, CommandInfo _)
        {
            // A human who took over a bot (ControllingBot) still issues commands
            // through their own controller, but the pawn that exists in the world
            // belongs to the bot. GetPlayerEvent redirects to that bot controller,
            // so the pawn/skill lookups below act on the body actually being played.
            player = PlayerManager.GetPlayerEvent(player);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index);
            if (playerInfo == null || playerInfo.IsDrawing) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (!player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            // Per-skill metadata: heroes flagged
            // "disableOnFreezeTime" simply cannot be triggered during freeze time.
            if (SkillRuntime.GetMetadata(playerInfo.Skill).DisableOnFreezeTime && SkillUtils.IsFreezeTime())
                return;

            string[] commands = _.ArgString.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            Debug.WriteToDebug($"Player {player.PlayerName} used the skill: {playerInfo.Skill}");

            if (commands == null || commands.Length == 0)
                Instance.InvokeUseSkill(playerInfo.Skill, player);
            else
                Instance.InvokeTypeSkill(playerInfo.Skill, player, commands);
        }

        // Assigns a hero to a target player for the current round only.
        // Usage: <command> <steamid|name> <skill name or enum name>
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_SetSkill(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_setskill {command.ArgString} command.");
            if (!string.IsNullOrEmpty(config.Commands.SetSkillCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.SetSkillCommand.Permission)) return;
            var targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => p != null && p.IsValid
                                                                          && (p.SteamID.ToString().Equals(command.GetArg(1), StringComparison.CurrentCultureIgnoreCase)
                                                                          || p.PlayerName.Equals(command.GetArg(1), StringComparison.OrdinalIgnoreCase)));

            if (command.ArgCount < 2)
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("correct_form_setskill"));
                    return;
                }
                SkillUtils.PrintToChat(player, player.GetTranslation("correct_form_setskill"));
                return;
            }

            if (targetPlayer == null)
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("player_not_found_setskill"));
                    return;
                }
                SkillUtils.PrintToChat(player, player.GetTranslation("player_not_found_setskill"));
                return;
            }

            // Skill names can contain a space ("Second Life"), so a 4th argument is
            // treated as the second half of the name rather than a separate option.
            var skillName = command.ArgCount > 3 ? $"{command.GetArg(2)} {command.GetArg(3)}" : command.GetArg(2);
            // Matched against both the localized display name and the raw Skills enum name.
            var skill = SkillData.Skills.FirstOrDefault(s => player != null && player.GetSkillName(s.Skill).Equals(skillName, StringComparison.OrdinalIgnoreCase) || s.Skill.ToString().Equals(skillName, StringComparison.OrdinalIgnoreCase));

            if (skill == null)
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("skill_not_found_setskill"));
                    return;
                }
                SkillUtils.PrintToChat(player, player.GetTranslation("skill_not_found_setskill"));
                return;
            }

            var skillPlayer = PlayerManager.GetPlayerByIndex(targetPlayer.Index);
            if (skillPlayer != null)
            {
                // Order matters: tear down the old hero's hooks/entities first, then
                // swap the stored skill, then let the new hero set itself up.
                Instance.InvokeDisableSkill(skillPlayer.Skill, targetPlayer);
                skillPlayer.Skill = skill.Skill;
                skillPlayer.SpecialSkill = Skills.None;
                Instance.InvokeEnableSkill(skill.Skill, targetPlayer);
                Event.UpdateSkillHudExpired(skillPlayer, skill.Skill);

                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("done_setskill"));
                    return;
                }

                SkillUtils.PrintToChat(player, $"{player.GetTranslation("done_setskill")}: {ChatColors.LightRed}{player.GetSkillName(skill.Skill)} {ChatColors.Lime}{player.GetTranslation("for_setskill")} {ChatColors.LightRed}\u202A{targetPlayer.PlayerName}\u202C");

                if (skill.Display)
                    SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skill.Skill)}{ChatColors.Lime}: {player.GetSkillDescription(skill.Skill)}", border: "b");
            }
            else
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("error_setskill"));
                    return;
                }

                SkillUtils.PrintToChat(player, player.GetTranslation("error_setskill"));
            }
        }

        // Opens the center HTML menu listing every loaded hero (see menu/Menu.cs).
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_SkillsListMenu(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_skills {command.ArgString} command.");
            if (player == null) return;
            if (!string.IsNullOrEmpty(config.Commands.SkillsListCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.SkillsListCommand.Permission)) return;
            Menu.DisplaySkillsList(player);
        }

        // Debug helper: reports whether an entity index (or raw handle value) is
        // still alive and what its DesignerName is. Useful when chasing leaked
        // skill entities that EntityManager should have cleaned up.
        [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_CheckEntity(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_entity {command.ArgString} command.");

            if (player != null && player.IsValid)
                if (!string.IsNullOrEmpty(config.Commands.CheckEntityCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.CheckEntityCommand.Permission))
                        return;

            int.TryParse(command.GetArg(1), out int index);
            if (index == -1)
            {
                if (player == null)
                {
                    Server.PrintToConsole("Invalid entity index!");
                    return;
                }
                player.PrintToChat("Invalid entity index!");
                return;
            }

            var entity = Utilities.GetAllEntities().FirstOrDefault(e => e != null && e.IsValid && (e.Index == index || e.Handle == (nint)index));
            
            if (player == null)
                Server.PrintToConsole(entity != null ? $"Entity {entity?.DesignerName} exists!" : "Entity does not exist!");
            else
                player.PrintToChat(entity != null ? $"Entity {entity?.DesignerName} exists!" : "Entity does not exist!");
        }

        // Voting command. Admin -> change map now; anyone else -> cast a ChangeMap
        // vote carrying the requested map name as the vote's argument.
        [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_ChangeMap(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_map {command.ArgString} command.");
            if (player != null && player.IsValid)
                if (!string.IsNullOrEmpty(config.Voting.ChangeMapCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Voting.ChangeMapCommand.Permission))
                {
                    if (!config.Voting.ChangeMapCommand.EnableVoting) return;
                    player.Vote(VoteType.ChangeMap, command.ArgString);
                    return;
                }
            ChangeMap(command);
        }

        // A purely numeric argument is treated as a Workshop item ID
        // (host_workshop_map); anything else must be an installed map (changelevel).
        private static void ChangeMap(CommandInfo command)
        {
            string map = command.GetArg(1).ToLowerInvariant();

            if (string.IsNullOrEmpty(map))
            {
                command.ReplyToCommand($" {ChatColors.Red}{command.CallingPlayer?.GetTranslation("invalid_map")}");
                return;
            }

            Localization.PrintTranslationToChatAll($" {ChatColors.Yellow}{{0}} ({ChatColors.Green}{map}{ChatColors.Yellow})...", ["loading_map"]);

            if (uint.TryParse(map, out _))
                Server.ExecuteCommand($"host_workshop_map {map}");
            else if (!Server.IsMapValid(map))
                command.ReplyToCommand($" {ChatColors.Red}{command.CallingPlayer?.GetTranslation("invalid_map")}");
            else
                Server.ExecuteCommand($"changelevel {map}");
        }

        // Voting command: starts/restarts the match.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_StartGame(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_start {command.ArgString} command.");
            if (player != null && player.IsValid)
                if (!string.IsNullOrEmpty(config.Voting.StartGameCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Voting.StartGameCommand.Permission))
                {
                    if (!config.Voting.StartGameCommand.EnableVoting) return;
                    player.Vote(VoteType.StartGame);
                    return;
                }
            StartGame(command);
        }

        // Runs the ';'-separated ConVar list from config (StartParams, or SVStartParams
        // when the command was called with the "sv" argument), then either ends warmup
        // or issues mp_restartgame.
        private static void StartGame(CommandInfo command)
        {
            int cheats = command.GetArg(1) == "sv" ? 1 : 0;

            foreach (string consoleCommand in cheats == 1
                                ? ConfigurationStore.Settings.Voting.StartGameCommand.SvStartParams.Split(";")
                                : ConfigurationStore.Settings.Voting.StartGameCommand.StartParams.Split(";"))
                Server.ExecuteCommand(consoleCommand);

            if (Instance?.GameRules?.WarmupPeriod == true)
            {
                Server.ExecuteCommand("mp_warmup_end");
                Localization.PrintTranslationToChatAll($" {ChatColors.Green}{{0}}", ["game_start"]);
            }
            else
            {
                Server.ExecuteCommand("mp_restartgame 2");
                Instance?.AddTimer(2.0f, () => Localization.PrintTranslationToChatAll($" {ChatColors.Green}{{0}}", ["game_start"]), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        // Voting command: swaps the two teams wholesale.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Swap(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_swap {command.ArgString} command.");
            if (player != null && player.IsValid)
                if (!string.IsNullOrEmpty(config.Voting.SwapCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Voting.SwapCommand.Permission))
                {
                    if (!config.Voting.SwapCommand.EnableVoting) return;
                    player.Vote(VoteType.SwapTeam);
                    return;
                }
            Swap();
        }

        // Spectators are left alone; only CT/T are flipped, then the round is ended
        // so the new teams start cleanly.
        private static void Swap()
        {
            foreach (var player in Utilities.GetPlayers())
                if (Instance.IsPlayerValid(player) && new CsTeam[] { CsTeam.CounterTerrorist, CsTeam.Terrorist }.Contains(player.Team))
                    player.SwitchTeam(player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist);
            Server.ExecuteCommand($"endround");
        }

        // Voting command: randomly redistributes players across both teams.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Shuffle(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_shuffle {command.ArgString} command.");
            if (player != null && player.IsValid)
                if (!string.IsNullOrEmpty(config.Voting.ShuffleCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Voting.ShuffleCommand.Permission))
                {
                    if (!config.Voting.ShuffleCommand.EnableVoting) return;
                    player.Vote(VoteType.ShuffleTeam);
                    return;
                }
            Shuffle();
        }

        private static void Shuffle()
        {
            var players = Utilities.GetPlayers().FindAll(p => Instance.IsPlayerValid(p) && new CsTeam[] { CsTeam.CounterTerrorist, CsTeam.Terrorist }.Contains(p.Team));
            // With an odd player count the extra player is randomly given to CT or T
            // by rounding the CT quota down or up.
            double CTlimit = Instance.Random.Next(0, 2) == 0 ? Math.Floor(players.Count / 2.0) : Math.Ceiling(players.Count / 2.0);

            foreach (var player in players.OrderBy(_ => Instance.Random.Next()).ToList())
            {
                player?.SwitchTeam(CTlimit > 0 ? CsTeam.CounterTerrorist : CsTeam.Terrorist);
                CTlimit--;
            }
            Server.ExecuteCommand($"mp_restartgame 1");
        }

        // Voting command: toggles the match pause.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Pause(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_pause {command.ArgString} command.");
            if (player != null && player.IsValid)
                if (!string.IsNullOrEmpty(config.Voting.PauseCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Voting.PauseCommand.Permission))
                {
                    if (!config.Voting.PauseCommand.EnableVoting) return;
                    player.Vote(VoteType.PauseGame);
                    return;
                }
            Pause();
        }

        // gamePaused is the plugin's own toggle state; the engine has no readable
        // "is paused" flag here, so pause/unpause alternate off this local bool.
        private static void Pause()
        {
            Localization.PrintTranslationToChatAll($" {(gamePaused ? ChatColors.Green : ChatColors.Red)}{{0}}", [gamePaused ? "unpause" : "pause"]);
            Server.ExecuteCommand(gamePaused ? "mp_unpause_match" : "mp_pause_match");
            gamePaused = !gamePaused;
        }

        // Adds a flat 100 HP to the caller's own pawn (AddHealth respects the pawn's
        // MaxHealth handling in SkillUtils).
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_Heal(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_heal {command.ArgString} command.");
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (!string.IsNullOrEmpty(config.Commands.HealCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.HealCommand.Permission)) return;
            SkillUtils.AddHealth(player.PlayerPawn.Value, 100);
            player.PrintToChat($" {ChatColors.Green}{player.GetTranslation("healed")}");
        }

        // Sets an absolute HP value. AddHealth only takes a delta and clamps to a max,
        // so the requested value is passed both as the delta (value - current) and as
        // the new maximum - that is what allows HP above the default 100.
        [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_Health(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_health {command.ArgString} command.");
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (!string.IsNullOrEmpty(config.Commands.HealthCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.HealthCommand.Permission)) return;

            var pawn = player.PlayerPawn.Value;
            if (int.TryParse(command.GetArg(1), out int health))
                SkillUtils.AddHealth(pawn, health - pawn.Health, health);

            player.PrintToChat($" {ChatColors.Green}{player.GetTranslation("set_health")}");
        }

        // Test helper: spawns an already-planted, ticking C4 at the caller's feet.
        // Optional argument is the fuse in seconds (default 40).
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_PlantedBomb(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_plantedbomb {command.ArgString} command.");
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (!string.IsNullOrEmpty(config.Commands.PlantedBomb.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.PlantedBomb.Permission)) return;

            CPlantedC4? bomb = Utilities.CreateEntityByName<CPlantedC4>("planted_c4");
            if (bomb == null || !bomb.IsValid) return;

            var pawn = player.PlayerPawn.Value;
            bomb.Teleport(pawn.AbsOrigin, pawn.AbsRotation);
            bomb.DispatchSpawn();
            bomb.BombTicking = true;

            if (!int.TryParse(command.GetArg(1), out int time))
                time = 40;

            // C4Blow is an absolute game time, not a countdown.
            bomb.C4Blow = Server.CurrentTime + time;
            player.PrintToChat($" {ChatColors.Green}{player.GetTranslation("planted_bomb_spawned", [time])}");
        }

        // Test helper: teleports a bot onto the caller's position and rotation.
        // Arg 1 = bot slot (-1 / omitted picks any alive bot).
        // Arg 2 = godmode, accepted as either "true"/"false" or 1/0; when set it
        //         clears the bot pawn's TakesDamage so skills cannot hurt it.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_BotPlace(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_bot_place {command.ArgString} command.");
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (!string.IsNullOrEmpty(config.Commands.BotPlace.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.BotPlace.Permission)) return;

            var pawn = player.PlayerPawn.Value;
            if (player.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null || pawn.AbsRotation == null) return;

            if (!int.TryParse(command.GetArg(1), out int botSlot))
                botSlot = -1;

            var bot = Utilities.GetPlayers().Where(p => p != null && p.IsValid && p.IsBot && p.PawnIsAlive && (botSlot == -1 || p.Slot == botSlot)).FirstOrDefault();
            if (bot == null || bot.PlayerPawn.Value == null || !bot.PlayerPawn.Value.IsValid)
            {
                player.PrintToChat($" {ChatColors.Green}{player.GetTranslation("bot_placed_not_found")}");
                return;
            }

            bot.PlayerPawn.Value.Teleport(new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z), new QAngle(pawn.AbsRotation.X, pawn.AbsRotation.Y, pawn.AbsRotation.Z), Vector.Zero);

            bot.PlayerPawn.Value.TakesDamage = true;
            if ((bool.TryParse(command.GetArg(2), out bool godmode) && godmode == true)
                || (int.TryParse(command.GetArg(2), out int godmodeInt) && godmodeInt == 1))
                bot.PlayerPawn.Value.TakesDamage = false;

            player.PrintToChat($" {ChatColors.Green}{player.GetTranslation("bot_placed")}");
        }

        // Toggles the plugin's own skill HUD for the caller.
        // playerInfo.HideHUD is a "hidden until this tick" watermark (the HUD draws
        // only while HideHUD < Server.TickCount), so int.MaxValue means hidden forever
        // and int.MinValue means always shown. This is plugin state and unrelated to
        // the pawn's engine-side m_iHideHUD bit field.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_HUD(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_hud {command.ArgString} command.");
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (!string.IsNullOrEmpty(config.Commands.HudCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.HudCommand.Permission)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index);
            if (playerInfo == null) return;

            int tickCount = Server.TickCount;
            bool isDisplayHUD = playerInfo.HideHUD < tickCount;

            playerInfo.HideHUD = isDisplayHUD ? int.MaxValue : int.MinValue;
            SkillUtils.SetMenuPaused(player, isDisplayHUD);
            player.PrintToChat($" {(!isDisplayHUD ? ChatColors.Green : ChatColors.Red)}{player.GetTranslation(!isDisplayHUD ? "hud_on" : "hud_off")}");
        }

        // Voting command: overwrites both team scores. Usage: <command> <ct> <t>.
        [CommandHelper(minArgs: 2, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_SetScore(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_setscore {command.ArgString} command.");
            if (player != null && player.IsValid)
                if (!string.IsNullOrEmpty(config.Voting.SetScoreCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Voting.SetScoreCommand.Permission))
                {
                    if (!config.Voting.SetScoreCommand.EnableVoting) return;
                    player.Vote(VoteType.SetScore, command.ArgString);
                    return;
                }
            SetScore(player, command);
        }

        private static void SetScore(CCSPlayerController? player, CommandInfo command)
        {
            if (!int.TryParse(command.GetArg(1), out int ctScore) || !int.TryParse(command.GetArg(2), out int tScore))
            {
                if (player != null && player.IsValid)
                    SkillUtils.PrintToChat(player, player.GetTranslation("correct_form_setscore"));
                return;
            }

            // Scores are engine shorts. SetTeamScores also terminates the current round
            // (needed for the new scores to stick), using RoundDraw so neither side is
            // credited with a win.
            SkillUtils.SetTeamScores((short)ctScore, (short)tScore, RoundEndReason.RoundDraw);
        }

        // Runs the argument string as a raw server console command. This grants full
        // server control, so the ConsoleCommand.Permission entry should stay
        // restricted; note the permission check is the only gate here.
        [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_CustomCommand(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_console {command.ArgString} command.");
            if (player == null) return;
            if (!string.IsNullOrEmpty(config.Commands.ConsoleCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.ConsoleCommand.Permission)) return;
            string param = command.ArgString;
            Server.ExecuteCommand(param);
        }

        // Like Command_SetSkill, but the assignment is remembered in Event.staticSkills
        // and re-applied every round instead of being replaced by the random roll.
        // Assigning Skills.None removes the player's static entry again.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_SetStaticSkill(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_setstaticskill {command.ArgString} command.");
            if (!string.IsNullOrEmpty(config.Commands.SetStaticSkillCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.SetStaticSkillCommand.Permission)) return;
            var targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => p != null && p.IsValid
                                                                          && (p.SteamID.ToString().Equals(command.GetArg(1), StringComparison.CurrentCultureIgnoreCase)
                                                                          || p.PlayerName.Equals(command.GetArg(1), StringComparison.OrdinalIgnoreCase)));

            if (command.ArgCount < 2)
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("correct_form_setskill"));
                    return;
                }

                SkillUtils.PrintToChat(player, player.GetTranslation("correct_form_setskill"));
                return;
            }

            if (targetPlayer == null)
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("player_not_found_setskill"));
                    return;
                }

                SkillUtils.PrintToChat(player, player.GetTranslation("player_not_found_setskill"));
                return;
            }

            var skillName = command.ArgCount > 3 ? $"{command.GetArg(2)} {command.GetArg(3)}" : command.GetArg(2);
            var skill = SkillData.Skills.FirstOrDefault(s => player != null && player.GetSkillName(s.Skill).Equals(skillName, StringComparison.OrdinalIgnoreCase) || s.Skill.ToString().Equals(skillName, StringComparison.OrdinalIgnoreCase));

            if (skill == null)
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("skill_not_found_setskill"));
                    return;
                }

                SkillUtils.PrintToChat(player, player.GetTranslation("skill_not_found_setskill"));
                return;
            }

            var skillPlayer = PlayerManager.GetPlayerByIndex(targetPlayer.Index);
            if (skillPlayer != null)
            {
                Instance.InvokeDisableSkill(skillPlayer.Skill, targetPlayer);
                skillPlayer.Skill = skill.Skill;
                skillPlayer.SpecialSkill = Skills.None;
                Event.UpdateSkillHudExpired(skillPlayer, skill.Skill);

                // The round-start draw checks staticSkills first (RoundEvents), so an
                // entry here pins the player's hero across rounds. Entries are cleared
                // on map change.
                if (skill.Skill == Skills.None)
                    Event.staticSkills.TryRemove(targetPlayer.Index, out _);
                else
                    Event.staticSkills.TryAdd(targetPlayer.Index, skill);
                Instance.InvokeEnableSkill(skill.Skill, targetPlayer);

                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("done_setskill"));
                    return;
                }

                SkillUtils.PrintToChat(player, $"{player.GetTranslation("done_setskill")}: {ChatColors.LightRed}{player.GetSkillName(skill.Skill)} {ChatColors.Lime}{player.GetTranslation("for_setskill")} {ChatColors.LightRed}\u202A{targetPlayer.PlayerName}\u202C");

                if (skill.Display)
                    SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skill.Skill)}{ChatColors.Lime}: {player.GetSkillDescription(skill.Skill)}", border: "b");
            }
            else
            {
                if (player == null)
                {
                    Server.PrintToConsole(Localization.GetTranslation("error_setskill"));
                    return;
                }

                SkillUtils.PrintToChat(player, player.GetTranslation("error_setskill"));
            }
        }

        // Live-reloads heroshift.json and the selected optional language file, then
        // re-registers commands and rebuilds the active hero list by calling
        // invokes LoadSkill on every Skills value whose effective metadata is active.
        // Finally it downgrades any player currently holding a hero that was just
        // deactivated, so nobody keeps a skill that is no longer loaded.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Reload(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerName} used the css_reload {command.ArgString} command.");
            if (!string.IsNullOrEmpty(config.Commands.ReloadCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.ReloadCommand.Permission)) return;

            lock (setLock)
            {
                try
                {
                    ConfigurationStore.Reload();
                }
                catch (ConfigurationValidationException ex)
                {
                    foreach (var error in ex.Errors)
                        Server.PrintToConsole($"[HeroShift] {error}");
                    return;
                }
                Localization.Load();
                Load();

                SkillData.Skills.Clear();
                foreach (var skill in Enum.GetValues<Skills>())
                    if (SkillRuntime.GetMetadata(skill).Active)
                        Instance.InvokeLoadSkill(skill);

                // Both are lazy caches derived from the list just rebuilt: the
                // Skills -> info lookup map and the set of disableOnFreezeTime heroes.
                // They must be dropped here or they keep describing the old skill list.
                SkillData.Invalidate();
                Event.InvalidateFreezeDisabledCache();

                if (player != null && player.IsValid)
                    player.PrintToChat($" {ChatColors.Green}{player.GetTranslation("reload")}");
                else
                    Server.PrintToConsole($" {ChatColors.Green}{Localization.GetTranslation("reload")}");

                foreach (var target in Instance.SkillPlayer)
                {
                    if (SkillRuntime.GetMetadata(target.Skill).Active == false)
                        target.Skill = Event.noneSkill.Skill;
                    if (SkillRuntime.GetMetadata(target.SpecialSkill).Active == false)
                        target.SpecialSkill = Event.noneSkill.Skill;
                }
            }
        }

        // Testing aid: steps a target player through the alphabetically sorted hero
        // list, one hero per invocation, remembering the position per player index in
        // nextSkill. Usage: <command> <steamid|name> [index], where the optional
        // second argument is "-1" to step backwards or an absolute list index to jump
        // to. Note a target argument is required - without one the command returns.
        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_Next(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            Debug.WriteToDebug($"Player {player.PlayerName} used the css_next {command.ArgString} command.");
            if (!string.IsNullOrEmpty(config.Commands.NextCommand.Permission) && !AdminManager.PlayerHasPermissions(player, config.Commands.NextCommand.Permission)) return;

            var skillsList = SkillData.Skills.OrderBy(s => s.Skill.ToString()).ToList();
            if (skillsList.Count == 0) return;

            string playerString = command.GetArg(1);
            CCSPlayerController? targetPlayer = null;

            if (!string.IsNullOrEmpty(playerString))
            {
                targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => p != null && p.IsValid
                                                                  && (p.SteamID.ToString().Equals(playerString, StringComparison.CurrentCultureIgnoreCase)
                                                                  || p.PlayerName.Equals(playerString, StringComparison.OrdinalIgnoreCase)));
                
                if (targetPlayer == null)
                {
                    if (player == null)
                    {
                        Server.PrintToConsole(Localization.GetTranslation("player_not_found_setskill"));
                        return;
                    }
                    SkillUtils.PrintToChat(player, player.GetTranslation("player_not_found_setskill"));
                    return;
                }
            }

            if (targetPlayer == null)
                return;

            nextSkill.TryGetValue(targetPlayer.Index, out int currentIndex);

            int nextIndex = (currentIndex + 1) % skillsList.Count;

            string arg = command.GetArg(2);
            if (!string.IsNullOrEmpty(arg))
            {
                if (arg == "-1")
                    nextIndex--;
                else if (int.TryParse(arg, out int newIndex))
                    nextIndex = newIndex;
                else
                    return;
            }

            var skill = skillsList[nextIndex];
            player.PrintToChat(nextIndex.ToString());

            var skillPlayer = PlayerManager.GetPlayerByIndex(targetPlayer!.Index);
            if (skillPlayer == null) return;

            Instance.InvokeDisableSkill(skillPlayer.Skill, targetPlayer);
            skillPlayer.Skill = skill.Skill;
            skillPlayer.SpecialSkill = Skills.None;

            nextSkill[targetPlayer.Index] = nextIndex;

            Instance.InvokeEnableSkill(skill.Skill, targetPlayer);
            Event.UpdateSkillHudExpired(skillPlayer, skill.Skill);

            if (skill.Display)
                SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skill.Skill)}{ChatColors.Lime}: {player.GetSkillDescription(skill.Skill)}", border: "b");
        }
    }
}