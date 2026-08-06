using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player
{
    /*
     * PlayerOnTick.cs - the per-tick HUD driver, plus GameRules and map lifecycle.
     *
     * This is a SEPARATE OnTick listener from the hero dispatch in PlayerEvents.cs.
     * Split of duties:
     *   Event.OnTick (PlayerEvents.cs) - runs each hero's own per-frame logic
     *   this file                      - refreshes the center-HTML skill HUD, keeps
     *                                    Instance.GameRules resolved, and starts/stops
     *                                    BotManager on map change
     *
     * TICK BUDGET
     *   The listener runs at the full tick rate (64/s) but the HUD work is gated to
     *   every FOURTH tick (SkillUtils.IsHudFrame), which is still far faster than a client can
     *   perceive and halves the cost. Entity statistics are logged only every 1920
     *   ticks (~30s) and only when the perf log is on. The player list comes from
     *   PlayerManager.GetTickPlayers(), the per-tick cached snapshot - the hero OnTick
     *   loop already paid for that native scan this frame, so this reuses it instead
     *   of calling Utilities.GetPlayers() a second time.
     *
     * GAMERULES
     *   CCSGameRules lives in a map entity, so it does not exist until the map has
     *   loaded and the pointer dies on every map change. UpdateGameRules re-resolves it
     *   whenever it is null or its handle went stale, which is why OnMapStart simply
     *   sets Instance.GameRules = null.
     *
     * HUD PRIORITY, highest first (see UpdatePlayerHud)
     *   1. bots and non-players            - no HUD at all
     *   2. warmup / GamePhase >= 5 (match over)
     *   3. HideHUD tick guard              - another plugin owns the HUD slot
     *   4. HudSuppressedUntil              - a hero asked for silence
     *   5. an open WASD menu               - the menu owns the slot; unpause and yield
     *   6. IsDrawing                       - slot-machine animation of random names
     *   7. alive                           - own hero name + description or PrintHTML
     *   8. dead/spectating                 - the OBSERVED player's hero
     *
     * CS2 has exactly one center-HTML slot, so every branch above is competing for the
     * same real estate; the function returns early rather than overwriting.
     */
    public static class PlayerOnTick
    {
        // Registers the HUD tick listener and the map start/end hooks.
        public static void Load()
        {
            Instance.RegisterListener<OnTick>(Tick);

            Instance.RegisterListener<OnMapStart>(OnMapStart);
            Instance.RegisterListener<OnMapEnd>(OnMapEnd);
        }

        public static void Unload()
        {
            BotManager.Stop();
            Instance.RemoveListener<OnTick>(Tick);
            Instance.RemoveListener<OnMapStart>(OnMapStart);
            Instance.RemoveListener<OnMapEnd>(OnMapEnd);
            Instance.GameRules = null;
        }

        private static void Tick()
        {
            // GameRules is checked every tick (cheap, and other code depends on it);
            // the HUD itself only refreshes every fourth tick.
            UpdateGameRules();
            if (!SkillUtils.IsHudFrame()) return;

            // ~every 30 seconds at 64 tick: entity leak watchdog. Compares the total
            // live server entities against what EntityManager believes it owns.
            if (PerfLog.Enabled && Server.TickCount % 1920 == 0)
            {
                int server = Utilities.GetAllEntities().Count(e => e != null && e.IsValid);
                var (tracked, owners) = EntityManager.GetStatistics();
                PerfLog.Info($"ENTITIES server={server} tracked={tracked} owners={owners}");
            }

            long perfStart = PerfLog.Start();
            // Shared per-tick controller snapshot: the skill OnTick loop already scans the
            // player list this frame, so reuse that native scan instead of running a second one.
            var now = DateTime.Now;
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player != null && player.IsValid)
                    UpdatePlayerHud(player, now);
            }
            PerfLog.Sample("OnTick(hud)", perfStart);
        }

        // Dropping GameRules forces UpdateGameRules to re-resolve it from the new map's
        // cs_gamerules entity; Event.OnMapChange wipes all per-map skill state.
        private static void OnMapStart(string mapName)
        {
            Instance.GameRules = null;
            Event.OnMapChange();
            BotManager.Initialize();
        }

        // Bot timers are stopped here rather than relying on STOP_ON_MAPCHANGE alone.
        private static void OnMapEnd()
        {
            PerfLog.Info("===== MAP END (clean map change) =====");
            Debug.WriteToDebug("===== MAP END (clean map change) =====");
            BotManager.Stop();
        }

        // CCSGameRules is reached through the map's cs_gamerules proxy entity, so it can
        // only be found after the map has spawned its entities.
        private static void InitializeGameRules()
        {
            if (Instance.GameRules != null) return;
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();

            if (gameRulesProxy != null)
                Instance.GameRules = gameRulesProxy.GameRules;
        }

        // Re-resolves GameRules whenever it is missing or its native handle went stale
        // (map change frees it). Otherwise, when the flashing-HUD fix is enabled, it
        // keeps m_bGameRestart in sync with the restart time: the client only flickers
        // center HTML while it thinks a restart is pending, so holding this flag correct
        // is what stops the HUD from blinking.
        private static void UpdateGameRules()
        {
            if (Instance?.GameRules == null || Instance.GameRules.Handle == IntPtr.Zero)
                InitializeGameRules();
            else if (Instance != null && ConfigurationStore.Settings.General.EnableFlashingHtmlHudFix && !Instance.GameRules.WarmupPeriod)
                Instance.GameRules.GameRestart = Instance.GameRules.RestartRoundTime < Server.CurrentTime;
        }

        // Builds and sends one player's skill HUD for this tick. `now` is passed in so
        // every player in the same tick is compared against one timestamp.
        // See the priority list in the class header for the order of the branches.
        private static void UpdatePlayerHud(CCSPlayerController player, DateTime now)
        {
            if (player == null || !player.IsValid || player.IsBot) return;

            // No skill HUD during warmup or after the match ended.
            var gameRules = Instance?.GameRules;
            if (gameRules == null || gameRules.WarmupPeriod == true || gameRules.GamePhase >= 5) return;

            // GetPlayerEvent routes through the bot controller during bot takeover, so a
            // human driving a bot still reads the skill state attached to that pawn.
            // HideHUD is a TICK stamp: >= TickCount means another plugin's HUD is still
            // in its grace window (see Event.GetPrintToCenterHtml).
            var skillPlayer = PlayerManager.GetPlayerByIndex(PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index);
            if (skillPlayer == null || skillPlayer.HideHUD >= Server.TickCount) return;

            // A hero asked for the HUD to stay quiet for a while.
            if (skillPlayer.HudSuppressedUntil > now) return;

            // Alive players stop seeing the HUD once the hero-name timer expires - unless
            // the hero is pushing live text through PrintHTML (cooldowns, charges, etc.).
            // Dead players fall through, because the spectator HUD has no such timeout.
            if (player.PawnIsAlive && skillPlayer.SkillHudExpired < now && string.IsNullOrEmpty(skillPlayer.PrintHTML)) return;

            // The WASD menu draws into the same center-HTML slot. Unpause it (it may have
            // been paused by the other-plugin detection) and let it own the slot.
            if (SkillUtils.HasMenu(player))
            {
                SkillUtils.SetMenuPaused(player, false);
                return;
            }

            string infoLine = string.Empty;
            string skillLine = string.Empty;
            string remainingLine = string.Empty;

            bool showDescriptionHUD = skillPlayer.SkillDescriptionHudExpired >= now || ConfigurationStore.Settings.General.DisplayAlwaysDescription;
            bool isDescription = true;

            var skills = SkillData.GetSnapshot();

            if (skills.Length == 0)
            {
                infoLine = player.GetTranslation("your_skill");
                skillLine = player.GetTranslation("none");
            }
            // Drawing: a NEW random hero name is shown every refresh, which is what makes
            // the freeze-time slot machine. Purely cosmetic - the real hero is decided in
            // RoundEvents.SetSkillCore.
            else if (skillPlayer.IsDrawing && player.PawnIsAlive)
            {
                var randomSkill = skills[Instance.Random.Next(skills.Length)];

                infoLine = player.GetTranslation("drawing_skill");
                skillLine = $"<font color='{randomSkill.Color}'>{player.GetSkillName(randomSkill.Skill)}</font>";
            }
            else
            {
                if (player.PawnIsAlive)
                {
                    var skillInfo = SkillData.GetInfo(skillPlayer.Skill);

                    if (skillInfo != null)
                    {
                        infoLine = player.GetTranslation("your_skill");
                        skillLine = $"<font color='{skillInfo.Color}'>{player.GetSkillName(skillInfo.Skill, skillPlayer.SkillChance)}</font>";

                        // Third line is either the hero's own live text (PrintHTML, set by
                        // the hero itself) or the static description. isDescription only
                        // selects which font/colour block the HUD builder uses.
                        if (skillInfo.Skill != BuiltInSkillIds.None)
                        {
                            remainingLine = string.IsNullOrEmpty(skillPlayer.PrintHTML)
                                ? (showDescriptionHUD ? player.GetSkillDescription(skillInfo.Skill, skillPlayer.SkillChance) : "")
                                : skillPlayer.PrintHTML;

                            isDescription = string.IsNullOrEmpty(skillPlayer.PrintHTML);
                        }
                    }
                }
                // Dead or spectating: show the hero of whoever is being WATCHED, so the
                // spectator HUD stays useful after death.
                else
                {
                    if (player.Team is CsTeam.Spectator or CsTeam.None && ConfigurationStore.Settings.General.DisableSpectateHUD)
                        return;

                    // Admin-permission answer cached on the record (?? = resolve once) so
                    // the permission lookup does not run on every tick. Reset each round
                    // in SetSkillCore.
                    skillPlayer.HudOnDeathBlocked ??= AdminManager.PlayerHasPermissions(player, ConfigurationStore.Settings.General.DisableHUDOnDeathPermission);
                    if (skillPlayer.HudOnDeathBlocked == true) return;

                    // While dead the controller's Pawn is the observer pawn, and
                    // ObserverServices.ObserverTarget points at the PAWN being watched.
                    var pawn = player.Pawn.Value;
                    if (pawn?.ObserverServices == null) return;

                    var observerTarget = pawn.ObserverServices.ObserverTarget?.Value;
                    if (observerTarget == null || !observerTarget.IsValid) return;

                    // Map that pawn back to its controller by comparing native handles -
                    // there is no direct pawn->controller lookup on the cached snapshot.
                    var observedPlayer = PlayerManager.GetTickPlayers().FirstOrDefault(p =>
                        p != null && p.IsValid && p.Pawn?.Value?.Handle == observerTarget.Handle);

                    if (observedPlayer == null) return;

                    // Route again through GetPlayerEvent: if the person being watched is a
                    // human inside a bot, the skill state hangs off the bot's index.
                    var observedEvent = PlayerManager.GetPlayerEvent(observedPlayer);
                    if (observedEvent == null || !observedEvent.IsValid) return;

                    var observedSkill = PlayerManager.GetPlayerByIndex(observedEvent.Index);
                    if (observedSkill == null) return;

                    var observedSkillInfo = SkillData.GetInfo(observedSkill.Skill);
                    var observedSpecialInfo = observedSkill.SpecialSkill != BuiltInSkillIds.None
                        ? SkillData.GetInfo(observedSkill.SpecialSkill)
                        : null;

                    string primaryName = player.GetSkillName(observedSkill.Skill, observedSkill.SkillChance);
                    string primaryColor = observedSkillInfo?.Color ?? SkillRuntime.GetMetadata(BuiltInSkillIds.None).Color;
                    // The HUD is real HTML, so a nickname containing < or & must be encoded
                    // or it would break the markup. Names are also truncated so a long one
                    // cannot push the hero name out of the box.
                    string pName = System.Net.WebUtility.HtmlEncode(observedSkill.PlayerName);

                    if (pName.Length > 18)
                        pName = $"{pName[..17]}...";

                    var observerSkill = player.GetTranslation("observer_skill");
                    infoLine = string.IsNullOrEmpty(observerSkill) ? pName : $"{observerSkill} {pName}";

                    // A transformed player is shown as "original(current)".
                    if (observedSkill.SpecialSkill == BuiltInSkillIds.None || observedSpecialInfo == null)
                        skillLine = $"<font color='{primaryColor}'>{primaryName}</font>";
                    else
                    {
                        string specialName = player.GetSkillName(observedSpecialInfo.Skill);
                        skillLine = $"<font color='{observedSpecialInfo.Color}'>{specialName}({primaryName})</font>";
                    }

                    if (showDescriptionHUD)
                        remainingLine = player.GetSkillDescription(observedSkill.Skill, observedSkill.SkillChance);
                }
            }

            if (string.IsNullOrEmpty(skillLine)) return;

            Event.UpdateSkillHUD(player, skillPlayer, infoLine, skillLine, remainingLine, isDescription);
        }
    }
}
