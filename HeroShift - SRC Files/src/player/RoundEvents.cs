using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using RayTraceAPI;
using src.player.skills;
using src.utils;
using System.Collections.Concurrent;
using static CounterStrikeSharp.API.Core.Listeners;
using static src.HeroShift;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using src.SkillsCore;
namespace src.player
{
    /*
     * RoundEvents.cs - the ROUND LIFECYCLE and the hero draw.
     *
     * Second half of the partial `Event` class (see PlayerEvents.cs for the event
     * routing). This file owns the question "who gets which hero, and when".
     *
     * ROUND TIMELINE
     *   RoundEnd
     *     +0.5s  optional chat summary of everyone's hero
     *     +0.6s  PrecomputeNextRoundSkills - the expensive draw is done HERE, at round
     *            end, and cached in nextRoundPicks so round start stays cheap. It runs
     *            before the optional disable below on purpose, so the "do not repeat
     *            your current hero" rule can still see this round's heroes.
     *     +1.0s  DisableAll (only when DisableSkillsOnRoundEnd is set)
     *   RoundStart
     *     +0.1s  DisableAll - reset every hero and player back to a clean state
     *     +Xs    SetSkill, where X is derived from mp_freezetime (minus
     *            SkillTimeBeforeStart, plus 7s if the team-intro cinematic plays).
     *            Until it fires, IsDrawing == true and the HUD shows the slot-machine
     *            animation of random hero names.
     *   SetSkillCore
     *     applies the cached pick if it is still legal (IsPickStillValid), otherwise
     *     re-draws, then DisableSkill(old) -> assign -> +0.2s EnableSkill(new).
     *
     * WHY THE 0.2s DELAY BEFORE EnableSkill
     *   Pawns are not fully networked at the instant the round flips; enabling a hero
     *   that immediately touches the pawn on the same frame is unreliable. Heroes
     *   flagged disableOnFreezeTime are delayed further, until freeze time is over.
     *   Every deferred callback re-resolves the controller from the stored INDEX and
     *   re-checks that the player still holds that hero, because they can disconnect
     *   or be reassigned inside the delay.
     *
     * DRAW RULES applied to the candidate list (in order, PickSkillForPlayer):
     *   admin permission (requiredPermission) -> not the hero you just had ->
     *   NeedsTeammates when alone -> team restriction (OnlyTeam) -> NoRepeat history
     *   -> rarity roll + MaxPerServer cap (ChooseSkillByRarityAndMax).
     *
     * GAME MODES (Config.LoadedConfig.GameMode)
     *   Normal / FullRandom / NoRepeat - per player, full rule set above
     *   TeamSkills - one hero per team; SameSkills - one hero for everyone
     *   Debug      - walks the whole hero list one at a time, for testing
     *
     * MAP CHANGE
     *   OnMapChange wipes everything, including per-map history and the CheckTransmit
     *   listener, because controller indices are reused on the next map and stale
     *   state would be applied to whoever inherits the index.
     */
    public static partial class Event
    {
        // Weighted hero pick: roll a rarity tier (RarityManager), keep only candidates
        // in that tier that are still under their MaxPerServer cap, pick one at random.
        // Retried up to 6 times because a rolled tier can be empty after filtering.
        // Then two fallbacks: any candidate under its cap, else any candidate at all -
        // so a player always gets something rather than Skills.None.
        // SameSkills/TeamSkills ignore MaxPerServer, since by definition the whole team
        // shares one hero.
        private static jSkill_SkillInfo ChooseSkillByRarityAndMax(List<jSkill_SkillInfo> candidates, Dictionary<Skills, int> assignmentCounts, Config.GameModes gameMode)
        {
            if (candidates == null || candidates.Count == 0) return noneSkill;

            bool ignoreMax = gameMode == Config.GameModes.SameSkills || gameMode == Config.GameModes.TeamSkills;

            const int attempts = 6;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                var (roll, rolled) = RarityManager.RollRarity();

                var filtered = candidates.Where(s =>
                {
                    if (s == null) return false;
                    var def = SkillRuntime.All.FirstOrDefault(d => d.Name == s.Skill.ToString());
                    if (def == null) return false;

                    if (!string.Equals(def.Rarity ?? string.Empty, rolled.ToString(), StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (!ignoreMax && def.MaxPerServer >= 0)
                    {
                        int current = assignmentCounts.TryGetValue(s.Skill, out var c) ? c : 0;
                        if (current >= def.MaxPerServer) return false;
                    }

                    return true;
                }).ToList();

                if (filtered.Count > 0)
                    return filtered[Random.Shared.Next(filtered.Count)];
            }

            var fallback = candidates.Where(s =>
            {
                var def = SkillRuntime.All.FirstOrDefault(d => d.Name == s.Skill.ToString());
                if (def == null) return true;
                if (ignoreMax) return true;
                if (def.MaxPerServer < 0) return true;
                int current = assignmentCounts.TryGetValue(s.Skill, out var c) ? c : 0;
                return current < def.MaxPerServer;
            }).ToList();

            if (fallback.Count > 0)
                return fallback[Random.Shared.Next(fallback.Count)];

            return candidates[Random.Shared.Next(candidates.Count)];
        }

        // Resets round-scoped state, puts everyone into the "drawing" HUD animation and
        // schedules the draw. During warmup there is no timed draw - it just polls once
        // a second until warmup ends.
        private static HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
        {
            lock (setLock)
            {
                bool isWarmup = Instance.GameRules == null || Instance.GameRules.WarmupPeriod == true;
                isTransmitRegistered = false;
                SkillUtils.ClearKillCredits();
                SkillUtils.ClearCurses();
                Instance.AddTimer(.1f, () => DisableAll(), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                foreach (var player in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsHLTV && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist))
                {
                    var skillPlayer = PlayerManager.GetPlayerByIndex(player!.Index);
                    if (skillPlayer == null) continue;
                    skillPlayer.IsDrawing = !isWarmup;
                    skillPlayer.PrintHTML = null;
                }

                // CheckTransmit is the most expensive listener in the plugin, so it is
                // dropped whenever no hero needs visibility filtering and re-registered
                // by EnableTransmit() (see EntityEvents.cs) when one does.
                Instance.RemoveListener<CheckTransmit>(CheckTransmit);
                // The team-intro cinematic adds ~7s that mp_freezetime does not account
                // for, so every freeze-time deadline in this file adds it back manually.
                int freezetime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<Int32>() ?? 0;
                freezeTimeEnd = DateTime.Now.AddSeconds(freezetime + (Instance?.GameRules?.TeamIntroPeriod == true ? 7 : 0));

                // Kill any pending draw from a round that ended early (e.g. instant win).
                setSkillTimer?.Kill();

                if (isWarmup)
                {
                    setSkillTimer = Instance?.AddTimer(1f, SetSkill, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                    return HookResult.Continue;
                }

                // Land the draw SkillTimeBeforeStart seconds before freeze time ends, so
                // players can read their hero before they can move. The +0.3s is slack
                // so the timer never fires on the exact boundary tick.
                float timeToDraw = (Instance?.GameRules?.TeamIntroPeriod == true ? 7 : 0) + Math.Max(freezetime - Config.LoadedConfig.SkillTimeBeforeStart, 0) + .3f;
                setSkillTimer = Instance?.AddTimer(timeToDraw, SetSkill, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                return HookResult.Continue;
            }
        }

        // Perf wrapper around DisableAllCore.
        private static void DisableAll()
        {
            long perfStart = PerfLog.Start();
            DisableAllCore();
            PerfLog.End("DisableAll total", perfStart, 2.0);
        }

        // The round reset. Per player: DisableSkill on their hero, then clear their
        // per-round record (Skill, SpecialSkill, PrintHTML, SkillChance, SkillUsed) and
        // restore their view/HUD. Then a NewRound sweep over every hero used so far on
        // this map. Also destroys all tracked entities heroes spawned.
        private static void DisableAllCore()
        {
            lock (setLock)
            {
                // Re-register CheckTransmit so the dying-entity filter covers the kills below.
                EnableTransmit();

                Fortnite.skillInThisRound = false;
                EntityManager.DestroyAllTracked();

                foreach (var player in Utilities.GetPlayers().Where(p => p != null && p.IsValid))
                {
                    if (player == null || !player.IsValid) continue;

                    var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                    if (playerInfo == null) continue;

                    ActiveSkillsThisRound.TryAdd(playerInfo.Skill.ToString(), 0);
                    SkillsUsedThisMap.TryAdd(playerInfo.Skill.ToString(), 0);
                    if (playerInfo.SpecialSkill != noneSkill.Skill)
                    {
                        ActiveSkillsThisRound.TryAdd(playerInfo.SpecialSkill.ToString(), 0);
                        SkillsUsedThisMap.TryAdd(playerInfo.SpecialSkill.ToString(), 0);
                    }

                    Instance.InvokeDisableSkill(playerInfo.Skill, player);

                    playerInfo.Skill = noneSkill.Skill;
                    playerInfo.SpecialSkill = noneSkill.Skill;
                    playerInfo.PrintHTML = null;
                    playerInfo.SkillChance = 1;
                    playerInfo.SkillUsed = false;

                    RestorePlayer(player);
                }

                // Reset every skill used so far on this map, not only the ones held this round: a skill
                // nobody drew now would otherwise never clear state left over from an earlier round.
                // Skills that never ran cannot hold state, so they stay out of the sweep.
                foreach (var skillName in SkillsUsedThisMap.Keys)
                    if (Enum.TryParse<Skills>(skillName, ignoreCase: true, out var skill))
                        Instance.InvokeNewRoundSkill(skill);
                ActiveSkillsThisRound.Clear();
                tickFailuresLogged.Clear();
            }
        }

        // Full wipe on map change (called from PlayerOnTick's OnMapStart).
        // SuppressKills is raised around the NewRound sweep because the old map's
        // entities are already gone with the level - trying to kill them would just log
        // errors against freed handles. Player records, per-map history, precomputed
        // picks and the team/global mode picks are all cleared, since controller indices
        // are reused on the new map and stale state would land on the wrong player.
        public static void OnMapChange()
        {
            lock (setLock)
            {
                isTransmitRegistered = false;
                Instance.RemoveListener<CheckTransmit>(CheckTransmit);

                Fortnite.skillInThisRound = false;

                EntityManager.SuppressKills = true;
                EntityManager.DestroyAllTracked();
                foreach (var skill in SkillData.Skills)
                    Instance.InvokeNewRoundSkill(skill.Skill);
                EntityManager.SuppressKills = false;

                ActiveSkillsThisRound.Clear();
                SkillsUsedThisMap.Clear();
                nextRoundPicks.Clear();

                playersSkills.Clear();
                staticSkills.Clear();

                ctSkill = noneSkill;
                tSkill = noneSkill;
                allSkill = noneSkill;

                PlayerManager.Clear();

                // Forced back to 1 on every map change. No other code in the plugin
                // writes this ConVar, so this is a defensive reset of the jump
                // behaviour the plugin expects rather than the undo of a hero effect.
                ConVar.Find("sv_legacy_jump")?.SetValue("1");
            }
        }

        // Round end: stop the Illiterate text scrambler, tell every hero the round is
        // over, then schedule the summary, the next-round precompute and (optionally)
        // the reset. See the timeline in the class header for the ordering rationale.
        private static HookResult RoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            Illiterate.Disable();
            Instance.SkillDispatcher.DispatchRoundEnd(GetActiveSkillIds());

            lock (setLock)
            {
                // Deferred half a second so the round-end messages the game itself
                // prints do not interleave with the summary block.
                Instance.AddTimer(.5f, () =>
                {
                    if (!Config.LoadedConfig.SummaryAfterTheRound) return;

                    var _players = Utilities.GetPlayers().Where(p => p.IsValid && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist).OrderBy(p => p.Team).ToList();

                    foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid))
                    {
                        string skillsText = "";
                        foreach (var _player in _players)
                        {
                            var _playerSkill = PlayerManager.GetPlayerByIndex(_player.Index);
                            if (_playerSkill == null) continue;

                            var skillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == _playerSkill.Skill);
                            var specialSkillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == _playerSkill.SpecialSkill);
                            if (skillInfo == null) continue;

                            skillsText += $" {ChatColors.DarkRed}\u202A{_player.PlayerName}\u202C{ChatColors.Lime}: {(_playerSkill.SpecialSkill == Skills.None || specialSkillInfo == null ? player.GetSkillName(skillInfo.Skill, _playerSkill.SkillChance) : $"{player.GetSkillName(specialSkillInfo.Skill)} -> {player.GetSkillName(skillInfo.Skill, _playerSkill.SkillChance)}")}\n";
                        }

                        if (string.IsNullOrEmpty(skillsText)) continue;

                        SkillUtils.PrintToChat(player, string.Empty, title: player.GetTranslation("summary"), border: "t");
                        foreach (string text in skillsText.Split("\n"))
                            if (!string.IsNullOrEmpty(text))
                                SkillUtils.PrintToChat(player, text, title: player.GetTranslation("teammate_skills"), border: "");
                        SkillUtils.PrintToChat(player, string.Empty, border: "b");
                    }
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                // Before the optional disable below, so the "don't repeat the current skill"
                // exclusion still sees this round's skills.
                Instance.AddTimer(.6f, PrecomputeNextRoundSkills, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                if (Config.LoadedConfig.DisableSkillsOnRoundEnd)
                {
                    isTransmitRegistered = false;
                    Instance.AddTimer(1f, () => DisableAll(), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                    Instance.RemoveListener<CheckTransmit>(CheckTransmit);
                }
                return HookResult.Continue;
            }
        }

        // Perf wrapper around SetSkillCore; also the timer callback target.
        private static void SetSkill()
        {
            long perfStart = PerfLog.Start();
            SetSkillCore();
            PerfLog.End("SetSkill total", perfStart, 2.0);
        }

        // Everything the draw filters on, resolved ONCE per draw instead of per player.
        // The team counts are snapshots taken at build time.
        private sealed class PickContext
        {
            public required List<jSkill_SkillInfo> BaseList { get; init; }
            public required Dictionary<Skills, string> RequiredPermissions { get; init; }
            public required HashSet<Skills> NeedsTeammates { get; init; }
            public required HashSet<Skills> CtOnly { get; init; }
            public required HashSet<Skills> TOnly { get; init; }
            public required int TerroristCount { get; init; }
            public required int CounterTerroristCount { get; init; }
        }

        // Flattens the effective skill snapshot into fast lookup sets for one draw pass.
        // Skills.None is excluded from BaseList so it is never drawn on purpose.
        private static PickContext BuildPickContext(List<CCSPlayerController> validPlayers)
        {
            Dictionary<Skills, string> perms = [];
            foreach (var s in SkillData.Skills)
            {
                if (s == null || s.Skill == Skills.None) continue;
                string perm = SkillRuntime.GetMetadata(s.Skill).RequiredPermission;
                if (!string.IsNullOrEmpty(perm)) perms[s.Skill] = perm;
            }

            return new PickContext
            {
                BaseList = [.. SkillData.Skills.Where(s => s != null && s.Skill != Skills.None)],
                RequiredPermissions = perms,
                NeedsTeammates = ToSkillSet(SkillRuntime.All.Where(s => s.NeedsTeammates).Select(s => s.Name)),
                CtOnly = ToSkillSet(counterterroristSkills.Select(s => s.Name)),
                TOnly = ToSkillSet(terroristSkills.Select(s => s.Name)),
                TerroristCount = validPlayers.Count(p => p.Team == CsTeam.Terrorist),
                CounterTerroristCount = validPlayers.Count(p => p.Team == CsTeam.CounterTerrorist),
            };
        }

        // Converts typed skill identities into the legacy enum used by player state.
        private static HashSet<Skills> ToSkillSet(IEnumerable<string> names)
        {
            HashSet<Skills> set = [];
            foreach (var name in names)
                if (Enum.TryParse<Skills>(name, out var skill)) set.Add(skill);
            return set;
        }

        // Builds this player's candidate list by removing everything they may not have,
        // then hands it to the weighted picker. Filters, in order:
        //   admin permission, previous hero, needs-teammates, team restriction, NoRepeat
        // history. assignmentCounts is the running per-hero tally used for MaxPerServer.
        private static jSkill_SkillInfo PickSkillForPlayer(CCSPlayerController player, jSkill_PlayerInfo skillPlayer, PickContext ctx, Dictionary<Skills, int> assignmentCounts, Config.GameModes gameMode)
        {
            List<jSkill_SkillInfo> skillList = [.. ctx.BaseList];

            // Bots bypass permission checks - they have no SteamID to hold admin flags.
            if (!player.IsBot && ctx.RequiredPermissions.Count != 0)
                skillList.RemoveAll(s => ctx.RequiredPermissions.TryGetValue(s.Skill, out var perm) && !AdminManager.PlayerHasPermissions(player, perm));

            // Every mode except FullRandom refuses to hand out the same hero twice in a
            // row (both the current hero and the one it was transformed from).
            if (gameMode != Config.GameModes.FullRandom)
                skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill);

            // Heroes whose ability needs someone to target/buff are unusable solo.
            int teamCount = player.Team == CsTeam.Terrorist ? ctx.TerroristCount : ctx.CounterTerroristCount;
            if (teamCount == 1)
                skillList.RemoveAll(s => ctx.NeedsTeammates.Contains(s.Skill));

            if (player.Team == CsTeam.Terrorist)
                skillList.RemoveAll(s => ctx.CtOnly.Contains(s.Skill));
            else
                skillList.RemoveAll(s => ctx.TOnly.Contains(s.Skill));

            // NoRepeat: exclude every hero this player already had. Once the history has
            // consumed all of them the history is wiped and the cycle starts over.
            if (gameMode == Config.GameModes.NoRepeat && playersSkills.TryGetValue(player.Index, out ConcurrentBag<jSkill_SkillInfo>? skills))
            {
                skillList.RemoveAll(s => skills.Any(s2 => s2.Skill == s.Skill));
                if (skillList.Count == 0) skills.Clear();
            }

            var randomSkill = skillList.Count == 0 ? noneSkill : ChooseSkillByRarityAndMax(skillList, assignmentCounts, gameMode);

            if (gameMode == Config.GameModes.NoRepeat)
            {
                if (playersSkills.TryGetValue(player.Index, out ConcurrentBag<jSkill_SkillInfo>? value))
                    value.Add(randomSkill);
                else
                    playersSkills.TryAdd(player.Index, [randomSkill]);
            }

            return randomSkill;
        }

        // Re-validates a pick made at the END of the previous round against the state at
        // the START of this one. Between those two moments a player can switch teams,
        // teammates can leave (breaking NeedsTeammates), a hero can hit MaxPerServer, or
        // a config reload can remove the hero entirely - each of those forces a re-draw.
        private static bool IsPickStillValid(jSkill_SkillInfo pick, CCSPlayerController player, List<CCSPlayerController> validPlayers, Dictionary<Skills, int> assignmentCounts)
        {
            if (pick.Skill == Skills.None) return true;
            if (!SkillData.Skills.Any(s => s.Skill == pick.Skill)) return false;

            string name = pick.Skill.ToString();
            if (player.Team == CsTeam.Terrorist && counterterroristSkills.Any(s => s.Name == name)) return false;
            if (player.Team == CsTeam.CounterTerrorist && terroristSkills.Any(s => s.Name == name)) return false;

            var def = SkillRuntime.All.FirstOrDefault(d => d.Name == name);
            if (def == null) return false;
            if (def.NeedsTeammates && validPlayers.Count(p => p.Team == player.Team) == 1) return false;
            if (def.MaxPerServer >= 0 && assignmentCounts.TryGetValue(pick.Skill, out var c) && c >= def.MaxPerServer) return false;

            return true;
        }

        // Runs at round end so the expensive skill selection is off the round-start hot path;
        // SetSkillCore then only applies the picks.
        private static void PrecomputeNextRoundSkills()
        {
            long perfStart = PerfLog.Start();
            lock (setLock)
            {
                nextRoundPicks.Clear();

                // Only the per-player modes benefit from precomputing; TeamSkills,
                // SameSkills and Debug pick a single hero at draw time anyway.
                var gameMode = (Config.GameModes)Config.LoadedConfig.GameMode;
                if (gameMode is not (Config.GameModes.Normal or Config.GameModes.FullRandom or Config.GameModes.NoRepeat)) return;
                if (Instance?.GameRules == null || Instance.GameRules.WarmupPeriod == true) return;

                // Reading .Team can throw on a controller that is being torn down between
                // the IsValid check and the read, so the whole read is guarded.
                var validPlayers = Utilities.GetPlayers()
                    .Where(p => p != null && p.IsValid && !p.IsHLTV)
                    .Where(p => { try { return p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist; } catch { return false; } }).ToList();

                var ctx = BuildPickContext(validPlayers);

                Dictionary<Skills, int> assignmentCounts = [];
                foreach (var player in validPlayers)
                {
                    var skillPlayer = PlayerManager.GetPlayerByIndex(player.Index);
                    if (skillPlayer == null) continue;

                    var pick = PickSkillForPlayer(player, skillPlayer, ctx, assignmentCounts, gameMode);
                    nextRoundPicks[player.Index] = pick;

                    if (pick.Skill != Skills.None)
                        assignmentCounts[pick.Skill] = assignmentCounts.TryGetValue(pick.Skill, out var c) ? c + 1 : 1;
                }
            }
            PerfLog.End("PrecomputeSkills total", perfStart, 2.0);
        }

        // Stamps the two HUD deadlines on the player record: how long the hero name
        // stays on screen, and how long its description does. A per-hero value from
        // Per-skill metadata (hudDuration / descriptionHudDuration) wins over the
        // global Config value; -1 in either place means "never expire" and is stored as
        // DateTime.MaxValue, which PlayerOnTick then compares against DateTime.Now.
        public static void UpdateSkillHudExpired(jSkill_PlayerInfo skillPlayer, Skills skill)
        {
            float globalHudExpired = Config.LoadedConfig.SkillHudDuration;
            float? skillHudExpired = SkillRuntime.GetMetadata(skill).HudDuration;

            skillPlayer.SkillHudExpired =
                !skillHudExpired.HasValue ?
                    (globalHudExpired == -1 ? DateTime.MaxValue : DateTime.Now.AddSeconds(globalHudExpired))
                : skillHudExpired.Value == -1 ? DateTime.MaxValue
                : DateTime.Now.AddSeconds(skillHudExpired.Value);

            float globalDescriptionHudExpired = Config.LoadedConfig.SkillDescriptionDuration;
            float? skillDescriptionHudExpired = SkillRuntime.GetMetadata(skill).DescriptionHudDuration;

            skillPlayer.SkillDescriptionHudExpired =
                !skillDescriptionHudExpired.HasValue ?
                    (globalDescriptionHudExpired == -1 ? DateTime.MaxValue : DateTime.Now.AddSeconds(globalDescriptionHudExpired))
                : skillDescriptionHudExpired.Value == -1 ? DateTime.MaxValue
                : DateTime.Now.AddSeconds(skillDescriptionHudExpired.Value);
        }

        // THE DRAW. Ends the drawing animation and gives every player their hero for the
        // round. Clearing setSkillTimer first is what tells PlayerSpawned that the draw
        // is no longer pending. Per player the sequence is:
        //   resolve the hero (cached precompute -> re-draw -> or mode-specific pick)
        //   -> DisableSkill(previous) -> write Skill/SpecialSkill on the record
        //   -> +0.2s EnableSkill(new), delayed past freeze time for freeze-disabled heroes
        //   -> stamp the HUD expiry, announce in chat, optionally list teammates' heroes.
        private static void SetSkillCore()
        {
            setSkillTimer = null;
            lock (setLock)
            {
                if (Instance == null) return;

                // GameRules null = not ready; keep polling so skills land right after warmup ends.
                if (Instance.GameRules == null || Instance.GameRules.WarmupPeriod == true)
                {
                    setSkillTimer?.Kill();
                    setSkillTimer = Instance.AddTimer(1f, SetSkill, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                    return;
                }

                var validPlayers = Utilities.GetPlayers()
                    .Where(p => p != null && p.IsValid && !p.IsHLTV)
                    .Where(p =>
                    {
                        try { return p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist; }
                        catch { return false; }
                    }).ToList();

                // TeamSkills / SameSkills draw their shared hero ONCE here, before the
                // per-player loop; excluding the previous pick avoids two identical
                // rounds in a row, and the team filters keep a CT-only hero off the Ts.
                if (Config.LoadedConfig.GameMode == (int)Config.GameModes.TeamSkills)
                {
                    List<jSkill_SkillInfo> tSkills = [.. SkillData.Skills];
                    tSkills.RemoveAll(s => s.Skill == tSkill.Skill || s.Skill == Skills.None || counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    tSkill = tSkills.Count == 0 ? noneSkill : tSkills[Instance.Random.Next(tSkills.Count)];

                    List<jSkill_SkillInfo> ctSkills = [.. SkillData.Skills];
                    ctSkills.RemoveAll(s => s.Skill == ctSkill.Skill || s.Skill == Skills.None || terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    ctSkill = ctSkills.Count == 0 ? noneSkill : ctSkills[Instance.Random.Next(ctSkills.Count)];
                }
                else if (Config.LoadedConfig.GameMode == (int)Config.GameModes.SameSkills)
                {
                    List<jSkill_SkillInfo> allSkills = [.. SkillData.Skills];
                    allSkills.RemoveAll(s => s.Skill == allSkill.Skill || s.Skill == Skills.None || !allTeamsSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    allSkill = allSkills.Count == 0 ? noneSkill : allSkills[Instance.Random.Next(allSkills.Count)];
                }
                else if (Config.LoadedConfig.GameMode == (int)Config.GameModes.Debug && debugSkills.Count == 0)
                    debugSkills = [.. SkillData.Skills];

                // Live per-hero headcount, seeded from whatever players already hold and
                // incremented as picks are applied below. This is what enforces
                // MaxPerServer across the whole draw.
                Dictionary<Skills, int> assignmentCounts = new();
                foreach (var sp in Instance.SkillPlayer)
                {
                    if (sp == null) continue;
                    if (assignmentCounts.TryGetValue(sp.Skill, out var cnt)) assignmentCounts[sp.Skill] = cnt + 1;
                    else assignmentCounts[sp.Skill] = 1;
                }

                // Built lazily: if every precomputed pick is still valid it is never needed.
                PickContext? pickContext = null;

                foreach (var player in validPlayers)
                {
                    if (player == null) continue;
                    var teammates = validPlayers.Where(p => p != null && p.IsValid && p.Team == player.Team && p != player).ToList();
                    string teammateSkills = "";

                    var skillPlayer = PlayerManager.GetPlayerByIndex(player!.Index);
                    if (skillPlayer == null) continue;

                    // Ends the slot-machine HUD for this player. HudOnDeathBlocked is the
                    // cached admin-permission answer used by the death HUD; reset so it is
                    // re-evaluated (permissions can change between rounds).
                    skillPlayer.IsDrawing = false;
                    skillPlayer.HudOnDeathBlocked = null;
                    // No pawn = nothing a hero could act on, so leave them on None.
                    if (player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid)
                    {
                        skillPlayer.Skill = Skills.None;
                        continue;
                    }

                    jSkill_SkillInfo randomSkill = noneSkill;

                    Config.GameModes gameMode = (Config.GameModes)Config.LoadedConfig.GameMode;
                    if (gameMode == Config.GameModes.Normal || gameMode == Config.GameModes.FullRandom || gameMode == Config.GameModes.NoRepeat)
                    {
                        // Prefer the pick made at the end of the previous round; re-pick only when
                        // it no longer fits (team change, missing player, max reached).
                        if (nextRoundPicks.TryGetValue(player.Index, out var pre) && IsPickStillValid(pre, player, validPlayers, assignmentCounts))
                            randomSkill = pre;
                        else
                        {
                            pickContext ??= BuildPickContext(validPlayers);
                            randomSkill = PickSkillForPlayer(player, skillPlayer, pickContext, assignmentCounts, gameMode);
                        }
                    }
                    else if (gameMode == Config.GameModes.TeamSkills)
                        randomSkill = player.Team == CsTeam.Terrorist ? tSkill : ctSkill;
                    else if (gameMode == Config.GameModes.SameSkills)
                        randomSkill = allSkill;
                    else if (gameMode == Config.GameModes.Debug)
                    {
                        if (debugSkills.Count == 0)
                            debugSkills = [.. SkillData.Skills];
                        randomSkill = debugSkills[0];
                        debugSkills.RemoveAt(0);
                        player.PrintToChat($"{SkillData.Skills.Count - debugSkills.Count}/{SkillData.Skills.Count}");
                    }

                    Instance?.InvokeDisableSkill(skillPlayer.Skill, player);
                    skillPlayer.Skill = randomSkill.Skill;
                    skillPlayer.SpecialSkill = Skills.None;

                    if (randomSkill.Skill != Skills.None)
                    {
                        if (assignmentCounts.TryGetValue(randomSkill.Skill, out var cnt)) assignmentCounts[randomSkill.Skill] = cnt + 1;
                        else assignmentCounts[randomSkill.Skill] = 1;
                    }

                    if (randomSkill.Skill == Skills.Illiterate)
                        Illiterate.Enable();

                    // Only the INDEX is captured, never the controller: the deferred
                    // callbacks below re-resolve it, so a player who disconnected inside
                    // the delay cannot be acted on through a stale reference.
                    var playerIndex = player.Index;
                    Instance?.AddTimer(.2f, () =>
                    {
                        var playerTarget = Utilities.GetPlayerFromIndex((int)playerIndex);
                        if (playerTarget == null || !playerTarget.IsValid) return;

                        if (randomSkill.Display)
                            SkillUtils.PrintToChat(playerTarget, $"{ChatColors.DarkRed}{playerTarget.GetSkillName(randomSkill.Skill)}{ChatColors.Lime}: {playerTarget.GetSkillDescription(randomSkill.Skill)}",
                                border: !Utilities.GetPlayers().Any(p => p != null && p.IsValid && p.Team == playerTarget.Team && p != playerTarget) ? "tb" : "t");

                        // A hero flagged disableOnFreezeTime must not activate while players
                        // are still frozen, so its EnableSkill waits out the remaining
                        // freeze time instead of firing now.
                        if (SkillRuntime.GetMetadata(randomSkill.Skill).DisableOnFreezeTime && SkillUtils.IsFreezeTime())
                            Instance?.AddTimer(Config.LoadedConfig.SkillTimeBeforeStart, () =>
                            {
                                var playerTarget = Utilities.GetPlayerFromIndex((int)playerIndex);
                                if (playerTarget == null || !playerTarget.IsValid) return;

                                // The hero may have been replaced during the wait (death,
                                // transform, admin command) - enabling it now would leave a
                                // hero running that the player no longer has.
                                if (PlayerManager.GetPlayerByIndex(playerTarget!.Index)?.Skill != randomSkill.Skill) return;
                                Debug.WriteToDebug("Enabling skill after freeze time: " + randomSkill.Skill);
                                Instance?.InvokeEnableSkill(randomSkill.Skill, playerTarget);
                            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                        else
                        {
                            if (PlayerManager.GetPlayerByIndex(playerTarget!.Index)?.Skill != randomSkill.Skill) return;
                            Debug.WriteToDebug("Enabling skill: " + randomSkill.Skill);
                            Instance?.InvokeEnableSkill(randomSkill.Skill, playerTarget);
                        }
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                    Debug.WriteToDebug($"Player {skillPlayer.PlayerName} has got the skill \"{player.GetSkillName(randomSkill.Skill)}\".");
                    UpdateSkillHudExpired(skillPlayer, randomSkill.Skill);

                    // Deferred to 0.6s - later than the 0.2s block above - so every player
                    // in the loop already has their hero assigned and the list is complete.
                    if (Config.LoadedConfig.TeamMateSkillChatInfo)
                    {
                        Instance?.AddTimer(.6f, () =>
                        {
                            if (player == null || !player.IsValid) return;

                            foreach (var teammate in teammates)
                            {
                                var teammateInfo = PlayerManager.GetPlayerByIndex(teammate.Index);
                                if (teammateInfo != null && teammateInfo?.Skill != null)
                                {
                                    var skillInfo = SkillData.Skills.FirstOrDefault(p => p.Skill == teammateInfo.Skill);
                                    teammateSkills += $" {ChatColors.DarkRed}\u202A{teammate.PlayerName}\u202C{ChatColors.Lime}: {(skillInfo == null ? player.GetSkillName(Skills.None) : player.GetSkillName(skillInfo.Skill, teammateInfo.SkillChance))}\n";
                                }
                            }

                            if (!string.IsNullOrEmpty(teammateSkills))
                            {
                                SkillUtils.PrintToChat(player, string.Empty, title: player.GetTranslation("teammate_skills"), border: "t");
                                foreach (string text in teammateSkills.Split("\n"))
                                    if (!string.IsNullOrEmpty(text))
                                        SkillUtils.PrintToChat(player, text, title: player.GetTranslation("teammate_skills"), border: "");
                                SkillUtils.PrintToChat(player, string.Empty, title: player.GetTranslation("teammate_skills"), border: "b");
                            }
                        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                    }
                }

                // Consumed - the cache must not leak into a later round, where the picks
                // would be stale.
                nextRoundPicks.Clear();
            }
        }

        // Single-player draw for someone who joined the round late (see PlayerSpawned).
        // Same rule set as PickSkillForPlayer but written out inline, and it honours
        // staticSkills (an admin-forced hero) before drawing anything random. Unlike
        // SetSkillCore it does not use nextRoundPicks, since there is no cached pick for
        // a player who was not present at the previous round end.
        public static void SetRandomSkill(CCSPlayerController player)
        {
            lock (setLock)
            {
                var validPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsHLTV && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist).ToList();

                if (Config.LoadedConfig.GameMode == (int)Config.GameModes.TeamSkills)
                {
                    List<jSkill_SkillInfo> tSkills = [.. SkillData.Skills];
                    tSkills.RemoveAll(s => s.Skill == tSkill.Skill || s.Skill == Skills.None || counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    tSkill = tSkills.Count == 0 ? noneSkill : tSkills[0];

                    List<jSkill_SkillInfo> ctSkills = [.. SkillData.Skills];
                    ctSkills.RemoveAll(s => s.Skill == ctSkill.Skill || s.Skill == Skills.None || terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    ctSkill = ctSkills.Count == 0 ? noneSkill : ctSkills[0];
                }

                if (player == null) return;
                var skillPlayer = PlayerManager.GetPlayerByIndex(player!.Index);
                if (skillPlayer == null) return;

                skillPlayer.IsDrawing = false;
                if (player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid)
                {
                    skillPlayer.Skill = Skills.None;
                    return;
                }

                jSkill_SkillInfo randomSkill = noneSkill;
                if (Instance?.GameRules != null && Instance?.GameRules.WarmupPeriod == false)
                {
                    Config.GameModes gameMode = (Config.GameModes)Config.LoadedConfig.GameMode;
                    if (staticSkills.TryGetValue(player.Index, out var staticSkill))
                        randomSkill = staticSkill;
                    else if (gameMode == Config.GameModes.Normal || gameMode == Config.GameModes.FullRandom || gameMode == Config.GameModes.NoRepeat)
                    {
                        List<jSkill_SkillInfo> skillList = [.. SkillData.Skills];
                        skillList.RemoveAll(s => s?.Skill == Skills.None);
                        if (!player.IsBot)
                            skillList.RemoveAll(s => !string.IsNullOrEmpty(SkillRuntime.GetMetadata(s.Skill).RequiredPermission) && !AdminManager.PlayerHasPermissions(player, SkillRuntime.GetMetadata(s.Skill).RequiredPermission));

                        if (gameMode != Config.GameModes.FullRandom)
                            skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill);

                        if (validPlayers.Count(p => p.Team == player.Team) == 1)
                        {
                            var skillsNeedsTeammates = SkillRuntime.All.Where(s => s.NeedsTeammates).ToArray();
                            skillList.RemoveAll(s => skillsNeedsTeammates.Any(s2 => s2.Name == s.Skill.ToString()));
                        }

                        if (player.Team == CsTeam.Terrorist)
                            skillList.RemoveAll(s => counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                        else
                            skillList.RemoveAll(s => terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));

                        if (gameMode == Config.GameModes.NoRepeat && playersSkills.TryGetValue(player.Index, out ConcurrentBag<jSkill_SkillInfo>? skills))
                        {
                            skillList.RemoveAll(s => skills.Any(s2 => s2.Skill == s.Skill));
                            if (skillList.Count == 0) skills.Clear();
                        }

                        var assignmentCounts = new Dictionary<Skills, int>();
                        foreach (var sp in Instance.SkillPlayer)
                        {
                            if (sp == null) continue;
                            if (assignmentCounts.TryGetValue(sp.Skill, out var cnt)) assignmentCounts[sp.Skill] = cnt + 1;
                            else assignmentCounts[sp.Skill] = 1;
                        }

                        randomSkill = skillList.Count == 0 ? noneSkill : ChooseSkillByRarityAndMax(skillList, assignmentCounts, gameMode);
                    }
                    else if (gameMode == Config.GameModes.TeamSkills)
                        randomSkill = player.Team == CsTeam.Terrorist ? tSkill : ctSkill;
                    else if (gameMode == Config.GameModes.SameSkills)
                        randomSkill = allSkill;
                    else if (gameMode == Config.GameModes.Debug)
                    {
                        if (debugSkills.Count == 0)
                            debugSkills = [.. SkillData.Skills];
                        randomSkill = debugSkills[0];
                        debugSkills.RemoveAt(0);
                        player.PrintToChat($"{SkillData.Skills.Count - debugSkills.Count}/{SkillData.Skills.Count}");
                    }
                }

                Instance?.InvokeDisableSkill(skillPlayer.Skill, player);
                skillPlayer.Skill = randomSkill.Skill;
                skillPlayer.SpecialSkill = Skills.None;

                if (randomSkill.Display && Config.LoadedConfig.YourSkillChatInfo)
                    SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(randomSkill.Skill)}{ChatColors.Lime}: {player.GetSkillDescription(randomSkill.Skill)}",
                        border: !Utilities.GetPlayers().Any(p => p != null && p.IsValid && p.Team == player.Team && p != player) ? "tb" : "t");

                if (randomSkill.Skill == Skills.Illiterate)
                    Illiterate.Enable();

                Instance?.AddTimer(.2f, () =>
                {
                    if (SkillRuntime.GetMetadata(randomSkill.Skill).DisableOnFreezeTime && SkillUtils.IsFreezeTime())
                        Instance?.AddTimer(Config.LoadedConfig.SkillTimeBeforeStart, () =>
                        {
                            if (PlayerManager.GetPlayerByIndex(player!.Index)?.Skill != randomSkill.Skill) return;
                            Instance?.InvokeEnableSkill(randomSkill.Skill, player);
                        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                    else
                        Instance?.InvokeEnableSkill(randomSkill.Skill, player);
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                Debug.WriteToDebug($"Player {skillPlayer.PlayerName} has got the skill \"{player.GetSkillName(randomSkill.Skill)}\".");
                UpdateSkillHudExpired(skillPlayer, randomSkill.Skill);
            }
        }

        // Wall-clock moment this round's freeze time ends (team-intro period included).
        // Heroes use it to schedule their own "start acting now" logic.
        public static DateTime GetFreezeTimeEnd() => freezeTimeEnd;
    }
}
