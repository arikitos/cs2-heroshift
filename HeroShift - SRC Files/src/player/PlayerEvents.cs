using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
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
using System.Text.RegularExpressions;
using static CounterStrikeSharp.API.Core.Listeners;
using static src.HeroShift;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using src.SkillsCore;
namespace src.player
{
    /*
     * PlayerEvents.cs - the main ROUTER between CS2 game events and the heroes.
     *
     * `Event` is one class split across several files (partial):
     *   PlayerEvents.cs - registration + player/weapon/damage/HUD routing (this file)
     *   RoundEvents.cs  - round lifecycle and the skill draw
     *   EntityEvents.cs - bomb/grenade/trigger/CheckTransmit routing
     *
     * WHAT THIS FILE DOES
     *   Load() registers every game event handler, listener, user-message hook and
     *   native VirtualFunctions hook the plugin needs. Each handler then does almost
     *   no work itself: it fans the event out to the heroes that are currently in
     *   play. A hero lives in src/player/skills/<Name>.cs as a static class with
     *   `public static` hook methods registered as typed delegates in the built-in
     *   SkillRegistry. Event callbacks resolve stable SkillIds and invoke those
     *   delegates directly. If a
     *   hero does not declare the hook, nothing happens - that is normal.
     *
     * FAN-OUT FLOW (the pattern almost every handler follows)
     *   game event -> lock (setLock) -> typed SkillDispatcher hook
     *     -> for every distinct Skill currently held by a non-drawing player
     *        -> SkillDispatcher -> registered <Skill>.HookName delegate
     *   The dispatch is per DISTINCT SKILL, not per player: a hero hook is called
     *   once per round even if ten players hold it, so hero code is expected to
     *   iterate the players itself (usually via PlayerManager.GetTickPlayers()).
     *   Players with IsDrawing == true (the slot-machine animation during freeze
     *   time) are skipped, because their skill is not final yet.
     *
     * ENGINE THINGS THAT TRIP PEOPLE UP
     *   - setLock serialises every hook against OnTick and the round draw. Hooks
     *     fire from the engine's main thread but timers/NextFrame callbacks can
     *     interleave, so shared state is always touched under this lock.
     *   - Controller vs pawn identity: PlayerManager.GetPlayerEvent(p) gives the
     *     BOT controller that actually owns the pawn while a human is in a bot
     *     (bot takeover) - use it to act on the world and to look up skill state.
     *     PlayerManager.GetPlayerFromEvent(p) gives the HUMAN controller behind a
     *     bot - use it for chat/HUD. Swapping them makes effects hit the wrong body
     *     or messages reach nobody.
     *   - A thrown exception inside a hook would otherwise abort the whole engine
     *     callback and silently kill every later hero in the same dispatch, so the
     *     invoke helpers catch and log instead of propagating.
     *   - Per-hero tunables come from SkillsInfo.GetValue<T>(skill, "key")
     *     (configs/skillsInfo.json); global switches from Config.LoadedConfig
     *     (configs/config.json).
     */
    public static partial class Event
    {
        // Pending timer for the round's skill draw; non-null means "still drawing",
        // which PlayerSpawned uses to decide whether a spawn joins the animation.
        private static Timer? setSkillTimer = null;
        private static DateTime freezeTimeEnd = DateTime.MinValue;
        private static bool isTransmitRegistered = false;
        public static readonly jSkill_SkillInfo noneSkill = new(Skills.None, SkillRuntime.GetMetadata(Skills.None).Color, false);

        // Per-team / global picks used by the TeamSkills, SameSkills and Debug game
        // modes (see RoundEvents.cs). Debug mode walks debugSkills one hero at a time.
        private static jSkill_SkillInfo ctSkill = noneSkill;
        private static jSkill_SkillInfo tSkill = noneSkill;
        private static jSkill_SkillInfo allSkill = noneSkill;
        private static List<jSkill_SkillInfo> debugSkills = [.. SkillData.Skills];

        // Team restrictions taken from skillsInfo.json: OnlyTeam 2 = T, 3 = CT, 0 = both.
        public static readonly SkillsInfo.DefaultSkillInfo[] terroristSkills = [.. SkillsInfo.LoadedConfig.Where(s => s.OnlyTeam == (int)CsTeam.Terrorist)];
        public static readonly SkillsInfo.DefaultSkillInfo[] counterterroristSkills = [.. SkillsInfo.LoadedConfig.Where(s => s.OnlyTeam == (int)CsTeam.CounterTerrorist)];
        private static readonly SkillsInfo.DefaultSkillInfo[] allTeamsSkills = [.. SkillsInfo.LoadedConfig.Where(s => s.OnlyTeam == 0)];

        // playersSkills: history per player index, used by the NoRepeat game mode.
        // staticSkills: admin-forced hero per player index, overrides the random draw.
        private static readonly ConcurrentDictionary<uint, ConcurrentBag<jSkill_SkillInfo>> playersSkills = [];
        public static readonly ConcurrentDictionary<uint, jSkill_SkillInfo> staticSkills = [];
        // Single lock guarding all routing, tick dispatch and skill assignment.
        private static readonly object setLock = new();

        // Registers every game event, listener and native hook the router needs.
        // Called once from HeroShift.Load(); Unload() below undoes the manual hooks.
        public static void Load()
        {
            Instance.RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFull);
            Instance.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnect);
            // Instance.RegisterEventHandler<EventPlayerChat>(PlayerChat);
            Instance.RegisterEventHandler<EventPlayerSpawned>(PlayerSpawned);
            Instance.RegisterEventHandler<EventRoundStart>(RoundStart);
            Instance.RegisterEventHandler<EventRoundEnd>(RoundEnd);

            // Death is hooked twice: Pre rewrites the attacker/weapon for the kill feed
            // before the engine builds it, Post does the hero fan-out and chat info.
            Instance.RegisterEventHandler<EventPlayerDeath>(PlayerDeathPre, HookMode.Pre);
            Instance.RegisterEventHandler<EventPlayerDeath>(PlayerDeath);
            Instance.RegisterEventHandler<EventPlayerBlind>(PlayerBlind);
            Instance.RegisterEventHandler<EventPlayerHurt>(PlayerHurtPre, HookMode.Pre);
            Instance.RegisterEventHandler<EventPlayerHurt>(PlayerHurt);
            Instance.RegisterEventHandler<EventPlayerJump>(PlayerJump);
            Instance.RegisterEventHandler<EventBotTakeover>(BotTakeover);

            Instance.RegisterEventHandler<EventWeaponFire>(WeaponFire);
            Instance.RegisterEventHandler<EventItemEquip>(WeaponEquip);
            Instance.RegisterEventHandler<EventItemPickup>(WeaponPickup);
            Instance.RegisterEventHandler<EventWeaponReload>(WeaponReload);
            Instance.RegisterEventHandler<EventGrenadeThrown>(GrenadeThrown);

            Instance.RegisterEventHandler<EventBombBeginplant>(BombBeginplant);
            Instance.RegisterEventHandler<EventBombAbortplant>(BombAbortplant);
            Instance.RegisterEventHandler<EventBombPlanted>(BombPlanted);
            Instance.RegisterEventHandler<EventBombBegindefuse>(BombBegindefuse);

            Instance.RegisterEventHandler<EventDecoyStarted>(DecoyStarted);
            Instance.RegisterEventHandler<EventDecoyDetonate>(DecoyDetonate);

            Instance.RegisterEventHandler<EventSmokegrenadeDetonate>(SmokegrenadeDetonate);
            Instance.RegisterEventHandler<EventSmokegrenadeExpired>(SmokegrenadeExpired);

            // Button listener drives the "use skill" key; OnTick drives every hero's
            // per-frame logic; OnClientPutInServer creates the per-player state record.
            Instance.RegisterListener<OnPlayerButtonsChanged>(CheckUseSkill);
            Instance.RegisterListener<OnEntitySpawned>(EntitySpawned);
            Instance.RegisterListener<OnTick>(OnTick);
            Instance.RegisterListener<OnClientPutInServer>(OnPlayerConnectedBot);

            // Raw user messages (numeric ids, not typed events):
            //   208 = PlayerMakeSound     - lets sound heroes mute/alter footsteps etc.
            //   207 = the center-HTML text message - used to detect other plugins
            //         writing to the same HUD slot.
            Instance.HookUserMessage(208, PlayerMakeSound);
            Instance.HookUserMessage(207, GetPrintToCenterHtml);

            // Native TakeDamage hook. Pre can still change the damage value (armor,
            // multipliers, immunity); Post only observes the result.
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamagePost, HookMode.Post);

            Instance.RegisterEventHandler<EventBulletImpact>(BulletImpact);

            // Trigger touch + weapon acquisition are also native hooks, not game events.
            VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(OnTriggerEnter, HookMode.Post);
            VirtualFunctions.CBaseTrigger_EndTouchFunc.Hook(OnTriggerExit, HookMode.Pre);
            VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);

            // Disabled after CS2 updates started crashing Linux servers on player join.
            // The hooked native signature is only used to block weapon drops for Iana clones.
            // Keeping the plugin alive is safer than installing a stale global hook at load time.
        }

        // Removes only the hooks CounterStrikeSharp does not clean up for us
        // (native VirtualFunctions, user messages, CheckTransmit). Each unhook is
        // wrapped so one stale signature cannot abort the rest of the teardown.
        public static void Unload()
        {
            TryUnhook(() => VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre));
            TryUnhook(() => VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamagePost, HookMode.Post));
            TryUnhook(() => VirtualFunctions.CBaseTrigger_StartTouchFunc.Unhook(OnTriggerEnter, HookMode.Post));
            TryUnhook(() => VirtualFunctions.CBaseTrigger_EndTouchFunc.Unhook(OnTriggerExit, HookMode.Pre));
            TryUnhook(() => VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnWeaponCanAcquire, HookMode.Pre));
            TryUnhook(() => Instance.UnhookUserMessage(208, PlayerMakeSound));
            TryUnhook(() => Instance.RemoveListener<CheckTransmit>(CheckTransmit));
        }

        private static void TryUnhook(Action unhook)
        {
            try { unhook(); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] unhook failed: {ex.Message}"); }
        }

        // Heroes that react to a killing blow (revive / second chance). They must see
        // the FINAL damage value, so DispatchOnTakeDamage runs them after every other
        // hero has had its chance to reduce or cancel the damage.
        private static readonly Skills[] lateDamageSkills = [Skills.SecondLife, Skills.Phoenix, Skills.ReZombie];

        // Skills whose OnTick already threw this round; used to log once, not 64x/sec.
        private static readonly HashSet<Skills> tickFailuresLogged = [];

        // Builds the distinct active typed IDs in player-list order. This is the
        // typed equivalent of the legacy DispatchToActiveSkills `seen` loop and
        // deliberately skips players whose draw animation has not resolved yet.
        private static IReadOnlyList<SkillId> GetActiveSkillIds()
        {
            var ids = new List<SkillId>();
            var seen = new HashSet<SkillId>();

            foreach (var player in Instance.SkillPlayer)
            {
                if (player.IsDrawing) continue;

                var id = SkillRuntime.GetId(player.Skill);
                if (seen.Add(id)) ids.Add(id);
            }

            return ids;
        }

        // Same fan-out as DispatchToActiveSkills, but ordered: normal heroes first,
        // then the lateDamageSkills, so revive-on-lethal-damage heroes read the damage
        // value everyone else already finished modifying.
        private static void DispatchOnTakeDamage(DynamicHook h, bool post = false)
        {
            var seen = new HashSet<Skills>();
            List<Skills>? deferred = null;

            foreach (var p in Instance.SkillPlayer)
            {
                if (p.IsDrawing || !seen.Add(p.Skill)) continue;

                if (Array.IndexOf(lateDamageSkills, p.Skill) >= 0)
                {
                    (deferred ??= []).Add(p.Skill);
                    continue;
                }

                InvokeOnTakeDamage(p.Skill, h, post);
            }

            if (deferred == null) return;
            foreach (var skill in deferred)
                InvokeOnTakeDamage(skill, h, post);
        }

        // Outside DebugMode this is just InvokeSkill. In DebugMode it snapshots
        // CTakeDamageInfo.Damage (hook param 1) before and after the call so the log
        // shows exactly which hero altered the damage and by how much.
        private static void InvokeOnTakeDamage(Skills skill, DynamicHook h, bool post)
        {
            if (Config.LoadedConfig.DebugMode != true)
            {
                Instance.SkillDispatcher.DispatchOnTakeDamage([SkillRuntime.GetId(skill)], h, post);
                return;
            }

            var info = h.GetParam<CTakeDamageInfo>(1);
            float before = info == null ? 0f : info.Damage;

            Instance.SkillDispatcher.DispatchOnTakeDamage([SkillRuntime.GetId(skill)], h, post);

            float after = info == null ? 0f : info.Damage;
            if (Math.Abs(before - after) > 0.01f)
                Debug.WriteToDebug($"[DMG] {skill} changed damage {before:0.#} -> {after:0.#}{DescribeDamageTarget(h)}");
        }

        // Debug-only: turns hook param 0 (the victim CEntityInstance) into a readable
        // label. Prints both the raw controller index/skill and the "routed" pair from
        // GetPlayerEvent, which differ during bot takeover - that mismatch is usually
        // the cause when damage lands on the wrong hero's state.
        private static string DescribeDamageTarget(DynamicHook h)
        {
            try
            {
                var victimEntity = h.GetParam<CEntityInstance>(0);
                if (victimEntity == null || !victimEntity.IsValid) return string.Empty;

                var pawn = victimEntity.As<CCSPlayerPawn>();
                if (pawn == null || !pawn.IsValid || pawn.DesignerName != "player") return string.Empty;

                var controller = pawn.Controller.Value?.As<CCSPlayerController>();
                if (controller == null || !controller.IsValid) return string.Empty;

                uint routedIndex = PlayerManager.GetPlayerEvent(controller)?.Index ?? controller.Index;
                return $" on {controller.PlayerName} [idx={controller.Index} skill={PlayerManager.GetPlayerByIndex(controller.Index)?.Skill}" +
                    $" routedIdx={routedIndex} routedSkill={PlayerManager.GetPlayerByIndex(routedIndex)?.Skill}]";
            }
            catch
            {
                return string.Empty;
            }
        }

        // User message 208: every sound a player emits. Sound heroes inspect the
        // soundevent hash and can clear um.Recipients to silence it for some clients.
        private static HookResult PlayerMakeSound(UserMessage um)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchPlayerMakeSound(GetActiveSkillIds(), um);
                return HookResult.Continue;
            }
        }

        // CS2 has only ONE center-HTML slot, so another plugin writing to it fights
        // with the skill HUD and both flicker. This watches user message 207 and,
        // when the text is not ours (our own output is prefixed "<jRS/>"), suppresses
        // the skill HUD for a short while by pushing HideHUD 15 ticks into the future.
        // HideHUD == int.MaxValue means "hidden permanently", so it is left alone.
        private static HookResult GetPrintToCenterHtml(UserMessage um)
        {
            if (!Config.LoadedConfig.HideHudForOtherPlugins) return HookResult.Continue;

            // Sampled every 10th tick only - parsing the message debug string is far
            // too expensive to do on every HUD write from every plugin.
            int tickCount = Server.TickCount;
            if (tickCount % 10 != 0) return HookResult.Continue;
            
            lock (setLock)
            {
                // 226 = the text-message subtype that carries center HTML.
                if (um.ReadUInt("eventid") != 226)
                    return HookResult.Continue;

                var debug = um.DebugString;
                var match = Regex.Match(debug, @"val_string:\s*""(.*?)""");

                if (match.Success)
                {
                    var html = match.Groups[1].Value.Replace("\\'", "'");
                    if (string.IsNullOrEmpty(html)) return HookResult.Continue;
                    
                    bool isOtherPlugin = !html.StartsWith("<jRS/>");

                    var player = um.Recipients.FirstOrDefault();
                    if (player == null || !player.IsValid) return HookResult.Continue;

                    var playerInfo = PlayerManager.GetPlayerByIndex(PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index);
                    if (playerInfo == null) return HookResult.Continue;

                    if (isOtherPlugin && playerInfo.HideHUD != int.MaxValue)
                    {
                        playerInfo.HideHUD = tickCount + 15;
                        SkillUtils.SetMenuPaused(player, true);
                    }
                }

                return HookResult.Continue;
            }
        }

        // Weapon and grenade events below are plain pass-through routers: no logic
        // here, each just fans the event out to whichever heroes implement the hook.
        private static HookResult WeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchWeaponFire(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult WeaponEquip(EventItemEquip @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchWeaponEquip(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult WeaponPickup(EventItemPickup @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchWeaponPickup(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult WeaponReload(EventWeaponReload @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchWeaponReload(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult GrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchGrenadeThrown(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        // Cosmetic hit suppression. The pawn's real health was already handled by the
        // TakeDamage hooks; this only edits the player_hurt EVENT so the client does
        // not show a hit marker / damage indicator for damage a hero nullified.
        // Asks the victim's hero first, then the attacker's hero (only if it is a
        // different hero), and stops at the first "yes".
        // Note both lookups go through GetPlayerEvent, i.e. the bot controller holding
        // the pawn, which is what the skill state is keyed on.
        private static HookResult PlayerHurtPre(EventPlayerHurt @event, GameEventInfo info)
        {
            lock (setLock)
            {
                try
                {
                    if (@event.DmgHealth <= 0 && @event.DmgArmor <= 0) return HookResult.Continue;

                    var victim = PlayerManager.GetPlayerEvent(@event.Userid);
                    if (victim == null || !victim.IsValid) return HookResult.Continue;

                    var victimInfo = PlayerManager.GetPlayerByIndex(victim.Index);
                    if (victimInfo == null || victimInfo.IsDrawing) return HookResult.Continue;

                    SkillId? attackerSkillId = null;
                    var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
                    if (attacker != null && attacker.IsValid && attacker.Index != victim.Index)
                    {
                        var attackerInfo = PlayerManager.GetPlayerByIndex(attacker.Index);
                        if (attackerInfo != null && !attackerInfo.IsDrawing)
                            attackerSkillId = SkillRuntime.GetId(attackerInfo.Skill);
                    }

                    bool suppressed = Instance.SkillDispatcher.DispatchPlayerHurtPre(
                        SkillRuntime.GetId(victimInfo.Skill), attackerSkillId, @event);

                    if (!suppressed) return HookResult.Continue;

                    // The engine already subtracted the armor before this event fires,
                    // so refund it and mark m_ArmorValue dirty for the clients.
                    if (@event.DmgArmor > 0)
                    {
                        var pawn = victim.PlayerPawn?.Value;
                        if (pawn != null && pawn.IsValid)
                        {
                            pawn.ArmorValue += @event.DmgArmor;
                            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                        }
                    }

                    // Zeroing the event numbers is what removes the client-side hit feedback.
                    @event.DmgHealth = 0;
                    @event.DmgArmor = 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteToDebug($"PlayerHurtPre failed: {ex.Message}");
                }

                return HookResult.Continue;
            }
        }

        private static HookResult PlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchPlayerHurt(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult PlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchPlayerJump(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        // Fires when a human takes control of a bot (e.g. after coach/spectate).
        // From here on the pawn belongs to the bot controller while chat/HUD belong to
        // the human one - heroes holding cached controller references must re-resolve.
        private static HookResult BotTakeover(EventBotTakeover @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchBotTakeover(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        private static HookResult PlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchPlayerBlind(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        // OnTick runs 64 times a second, so its two scratch collections are
        // allocated once and cleared/refilled in place every frame.
        private static readonly HashSet<Skills> _activeSkillsSet = [];
        private static readonly List<Skills> _activeSkillsList = [];
        private static readonly Comparison<Skills> _tickOrderCmp = (a, b) => TickOrder(a).CompareTo(TickOrder(b));
        private static HashSet<Skills>? _freezeDisabledSkills;

        // AreaReaper and ChillOut depend on other skills' tick results, so they must tick last.
        private static int TickOrder(Skills s) => s == Skills.AreaReaper ? 2 : s == Skills.ChillOut ? 1 : 0;

        // Set of heroes whose "disableOnFreezeTime" flag is true in skillsInfo.json.
        // Built lazily and cached because reading the config per hero per tick is
        // expensive; InvalidateFreezeDisabledCache() drops it after a config reload.
        private static HashSet<Skills> BuildFreezeDisabledSkills()
        {
            var set = new HashSet<Skills>();
            foreach (var s in SkillData.Skills)
                if (SkillRuntime.GetMetadata(s.Skill).DisableOnFreezeTime)
                    set.Add(s.Skill);
            return set;
        }

        public static void InvalidateFreezeDisabledCache() => _freezeDisabledSkills = null;

        // Per-frame hero driver. Collects the distinct heroes in play, sorts them by
        // TickOrder, then calls <Skill>.OnTick() for each. Heroes flagged
        // disableOnFreezeTime are skipped while the round is still frozen.
        private static void OnTick()
        {
            long perfStart = PerfLog.Start();
            lock (setLock)
            {
                _activeSkillsSet.Clear();
                _activeSkillsList.Clear();
                foreach (var p in Instance.SkillPlayer)
                {
                    if (p.IsDrawing) continue;
                    if (_activeSkillsSet.Add(p.Skill))
                        _activeSkillsList.Add(p.Skill);
                }

                _activeSkillsList.Sort(_tickOrderCmp);

                bool freeze = SkillUtils.IsFreezeTime();
                _freezeDisabledSkills ??= BuildFreezeDisabledSkills();

                foreach (var skill in _activeSkillsList)
                {
                    if (freeze && _freezeDisabledSkills.Contains(skill)) continue;
                    try
                    {
                        Instance.SkillDispatcher.InvokeTickUnchecked(SkillRuntime.GetId(skill));
                    }
                    catch (Exception ex)
                    {
                        // Without this one throwing skill cancels every later skill's tick, every frame.
                        // Logged once per skill per round; at 64 ticks a repeat would flood the console.
                        if (tickFailuresLogged.Add(skill))
                            Server.PrintToConsole($"[HeroShift] {skill}.OnTick failed: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }
            PerfLog.Sample("OnTick(skills)", perfStart);
        }

        // OnClientPutInServer: creates (or re-registers) the jSkill_PlayerInfo record
        // that all skill state hangs off. Runs for bots too - bots only get a record
        // when EnableBotSkills is on. If a record for this index already exists (e.g.
        // reconnect into the same slot) it is re-registered rather than replaced, so
        // the current round's hero survives.
        private static void OnPlayerConnectedBot(int playerSlot)
        {
            lock (setLock)
            {
                var player = Utilities.GetPlayerFromSlot(playerSlot);
                if (player == null || !player.IsValid) return;

                if (player.IsBot && !Config.LoadedConfig.EnableBotSkills)
                    return;

                var existing = Instance.SkillPlayer.FirstOrDefault(p => p.PlayerIndex == player.Index);
                if (existing != null)
                {
                    PlayerManager.Register(existing);
                    return;
                }

                var playerInfo = new jSkill_PlayerInfo
                {
                    IsBot = player.IsBot,
                    PlayerName = player.PlayerName,
                    PlayerIndex = player.Index,
                    Skill = Skills.None,
                    SpecialSkill = Skills.None,
                    IsDrawing = false,
                    SkillChance = 1,
                    PrintHTML = null,
                    // int.MinValue = never suppressed (compared against Server.TickCount).
                    HideHUD = int.MinValue,
                    SkillUsed = false,
                };

                UpdateSkillHudExpired(playerInfo, Skills.None);

                PlayerManager.Register(playerInfo);
            }
        }

        // Prints the localised welcome message, substituting the {PLAYER},
        // {SERVER_NAME}, {VERSION}, {SKILLS_COUNT} and {AUTHOR*} placeholders.
        // The ‪ / ‬ pair around player names is a bidirectional-text
        // isolate, so an RTL nickname cannot reverse the rest of the chat line.
        // SKILLS_COUNT subtracts 1 because Skills.None is part of SkillData.Skills.
        private static HookResult PlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var player = PlayerManager.GetPlayerEvent(@event.Userid);
                if (player == null || !player.IsValid) return HookResult.Continue;

                string welcomeMsg = player.GetTranslation("welcome_message", "welcome");
                foreach (string line in welcomeMsg.Split("\n"))
                    player.PrintToChat($" {ChatColors.Green}" + line.Replace("{PLAYER}", $" {ChatColors.Red}\u202A{player.PlayerName}\u202C{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                            .Replace("{SERVER_NAME}", $" {ChatColors.Red}{ConVar.Find("hostname")?.StringValue ?? "Default Server"}{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                            .Replace("{VERSION}", $" {ChatColors.Red}v{Instance.ModuleVersion}{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                            .Replace("{SKILLS_COUNT}", $" {ChatColors.Red}{SkillData.Skills.Count - 1}{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                            .Replace("{AUTHOR1}", $" {ChatColors.Red}Jakub Bartosik (D3X){ChatColors.Green} ({ChatColors.Red}https://github.com/jakubbartosik/dRandomSkills{ChatColors.Green})", StringComparison.OrdinalIgnoreCase)
                                            .Replace("{AUTHOR2}", $" {ChatColors.Red}Juzlus{ChatColors.Green} ({ChatColors.Red}https://github.com/Juzlus/HeroShift{ChatColors.Green})", StringComparison.OrdinalIgnoreCase));
                return HookResult.Continue;
            }
        }

        // Full teardown for a leaving player, in order:
        //   1. DisableSkill on the hero they were holding (undo their own effects)
        //   2. PlayerDisconnect on EVERY hero, not just theirs - other heroes may hold
        //      this index in their own state (targets, clones, curse victims) and must
        //      be told to drop it or they will act on a freed controller
        //   3. release curse bookkeeping, unregister the state record, destroy any
        //      entities this player owned
        private static HookResult PlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var player = PlayerManager.GetPlayerEvent(@event.Userid);
                if (player == null || !player.IsValid) return HookResult.Continue;

                var skillPlayer = PlayerManager.GetPlayerByIndex(player!.Index);
                if (skillPlayer == null) return HookResult.Continue;

                Instance.SkillDispatcher.InvokeDisableSkill(SkillRuntime.GetId(skillPlayer.Skill), player);

                uint leavingIndex = player.Index;
                var registeredSkillIds = SkillData.Skills
                    .Select(skill => SkillRuntime.GetId(skill.Skill))
                    .ToArray();
                Instance.SkillDispatcher.DispatchPlayerDisconnect(registeredSkillIds, leavingIndex);

                SkillUtils.ClearCursesFor(leavingIndex);

                PlayerManager.UnregisterPlayer(player.Index);
                EntityManager.DestroyPlayerEntities(player.Index);

                return HookResult.Continue;
            }
        }

        // A spawn mid-draw (setSkillTimer still pending) just joins the drawing
        // animation - SetSkill will hand out the real hero when the timer fires.
        // Otherwise a player who spawned outside warmup with no hero yet (late join,
        // team switch) gets one immediately via SetRandomSkill.
        private static HookResult PlayerSpawned(EventPlayerSpawned @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var player = PlayerManager.GetPlayerEvent(@event.Userid);
                if (player == null || !player.IsValid) return HookResult.Continue;

                var skillPlayer = PlayerManager.GetPlayerByIndex(player!.Index);
                if (skillPlayer == null) return HookResult.Continue;

                if (setSkillTimer != null)
                {
                    skillPlayer.IsDrawing = true;
                    return HookResult.Continue;
                }

                skillPlayer.IsDrawing = false;
                if (Instance?.GameRules != null &&
                    Instance?.GameRules.WarmupPeriod == false &&
                    skillPlayer.Skill == Skills.None &&
                    skillPlayer.SpecialSkill == Skills.None)
                    SetRandomSkill(player);
                return HookResult.Continue;
            }
        }

        // Undoes the generic view/HUD changes any hero may have applied, so a player
        // never carries a blinded HUD, hidden radar or zoomed FOV into the next round.
        public static void RestorePlayer(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid) return;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return;

            // m_iHideHUD is a bit field; bit 8 (HIDEHUD_RADAR) is the one heroes toggle.
            // Clearing just that bit leaves any other plugin's bits intact.
            pawn.HideHUD = (uint)(pawn.HideHUD & ~(1 << 8));
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_iHideHUD");

            // Per-client ConVar override, so it must be reset per client too.
            player.ReplicateConVar("sv_disable_radar", "0");

            // 0 means "use the default FOV" rather than an actual 0-degree view.
            player.DesiredFOV = 0;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
        }

        // Kill-credit rewrite. A kill dealt indirectly by a hero (falling damage, a
        // spawned entity, a scripted push) reaches the engine with no attacker, so it
        // would show up as a suicide or world kill. Heroes register their intent with
        // SkillUtils.RegisterKillCredit; here, in the PRE hook - before the engine
        // builds the kill feed - the pending credit is consumed and the event's
        // Attacker/Weapon are overwritten, plus the attacker's kill counter bumped by
        // hand because the engine will not count a kill it did not attribute.
        private static HookResult PlayerDeathPre(EventPlayerDeath @event, GameEventInfo info)
        {
            try
            {
                var victim = @event.Userid;
                if (victim == null || !victim.IsValid) return HookResult.Continue;

                if (!SkillUtils.TryConsumeKillCredit(victim.Index, out uint attackerIndex, out string? weapon))
                    return HookResult.Continue;

                var attacker = Utilities.GetPlayerFromIndex((int)attackerIndex);
                if (attacker == null || !attacker.IsValid || attacker.Index == victim.Index)
                    return HookResult.Continue;

                @event.Attacker = attacker;

                if (!string.IsNullOrEmpty(weapon))
                    @event.Weapon = weapon;

                var matchStats = attacker.ActionTrackingServices?.MatchStats;
                if (matchStats != null)
                {
                    matchStats.Kills++;
                    Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pActionTrackingServices");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteToDebug($"PlayerDeathPre kill credit failed: {ex.Message}");
            }

            return HookResult.Continue;
        }

        // Thin perf wrapper: the real work is in PlayerDeathCore, timed so a slow
        // hero death hook shows up in the perf log instead of just causing lag.
        private static HookResult PlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            long perfStart = PerfLog.Start();
            var result = PlayerDeathCore(@event, info);
            PerfLog.End("PlayerDeath total", perfStart, 2.0);
            return result;
        }

        // On death: fan the event out to every hero, then explicitly DisableSkill the
        // dead player's own hero so its effects stop while the body is down, and
        // optionally tell the victim in chat which hero the killer had.
        private static HookResult PlayerDeathCore(EventPlayerDeath @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchPlayerDeath(GetActiveSkillIds(), @event);

                var victim = PlayerManager.GetPlayerEvent(@event.Userid);
                if (victim == null) return HookResult.Continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(victim.Index);
                if (playerInfo == null || playerInfo.IsDrawing) return HookResult.Continue;
                Instance.SkillDispatcher.InvokeDisableSkill(SkillRuntime.GetId(playerInfo.Skill), victim);

                var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
                if (attacker == null || victim == attacker) return HookResult.Continue;

                if (victim == attacker) return HookResult.Continue;
                if (Config.LoadedConfig.KillerSkillChatInfo)
                {
                    var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
                    if (attackerInfo != null)
                    {
                        var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == attackerInfo.Skill);
                        var specialSkillData = SkillData.Skills.FirstOrDefault(s => s.Skill == attackerInfo.SpecialSkill);
                        if (skillData == null || specialSkillData == null) return HookResult.Continue;
                        // Translated with the VICTIM's language, since they read it.
                        // When the killer was transformed mid-round the line shows
                        // "originalSkill -> currentSkill".
                        string skillDesc = victim.GetSkillDescription(skillData.Skill);

                        SkillUtils.PrintToChat(victim,
                            $"{ChatColors.DarkRed}{(attackerInfo.SpecialSkill == Skills.None ? victim.GetSkillName(skillData.Skill) : $"{victim.GetSkillName(specialSkillData.Skill)} -> {victim.GetSkillName(skillData.Skill)}")}{ChatColors.Lime} - {skillDesc}",
                            title: $"{victim.GetTranslation("enemy_skill")} {ChatColors.DarkRed}\u202A{attacker.PlayerName}\u202C{ChatColors.Lime}");
                    }
                }
                return HookResult.Continue;
            }
        }

        // Activation path for heroes with a manual ability: watches the button the
        // config names in AlternativeSkillButton and calls <Skill>.UseSkill(player).
        // The config string is normalised to PascalCase to match the PlayerButtons
        // enum ("use" -> "Use"), so config casing does not matter.
        private static void CheckUseSkill(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            lock (setLock)
            {
                string? button = Config.LoadedConfig.AlternativeSkillButton;
                if (string.IsNullOrEmpty(button) || button.Length < 2) return;

                string buttonName = $"{char.ToUpperInvariant(button[0])}{button[1..].ToLowerInvariant()}";
                if (!Enum.TryParse<PlayerButtons>(buttonName, out var skillButton)) return;

                // PlayerButtons is a bit mask - test membership, do not compare equality,
                // or holding any second key would hide the press.
                if ((pressed & skillButton) == 0) return;

                // An open WASD menu owns the keys; firing the ability too would double-act.
                if (SkillUtils.HasMenu(player)) return;

                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo == null || playerInfo.IsDrawing) return;

                if (SkillRuntime.GetMetadata(playerInfo.Skill).DisableOnFreezeTime && SkillUtils.IsFreezeTime())
                    return;

                // Special case for the +use key, which the game itself needs. If the
                // player is defusing, or is looking at something genuinely usable
                // within 80 units (door, button, dropped weapon, blocker), the press is
                // treated as a real interaction and the ability is NOT fired - otherwise
                // opening a door would burn a one-shot hero ability.
                if (skillButton == PlayerButtons.Use)
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid) return;
                    if (pawn.AbsOrigin == null || pawn.AbsRotation == null) return;

                    if (pawn.IsDefusing) return;

                    // AbsOrigin is at the feet, so ViewOffset.Z lifts the trace to eye height.
                    Vector eyePos = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z);
                    Vector endPos = eyePos + SkillUtils.GetForwardVector(pawn.EyeAngles) * 80;

                    ulong mask = (ulong)(InteractionLayers.MASK_WORLD_ONLY | InteractionLayers.Player | InteractionLayers.NPC);
                    ulong contents = 0;
                    var result = RayTrace.TraceShape(player, eyePos, endPos, mask, contents);

                    if (result.HasValue && result.Value.DidHit)
                    {
                        // The trace returns a raw handle, so it is wrapped back into a
                        // CBaseEntity to read DesignerName.
                        var entity = Activator.CreateInstance(typeof(CBaseEntity), result.Value.HitEntity) as CBaseEntity;
                        if (entity == null || !entity.IsValid) return;

                        string designer = entity.DesignerName;
                        if (designer.Contains("door") || designer.Contains("button") || designer.Contains("weapon") || designer.Contains("blocker")) return;
                    }
                }

                Debug.WriteToDebug($"Player {player.PlayerName} used the skill: {playerInfo.Skill} by PlayerButtons: {pressed}");
                Instance.SkillDispatcher.InvokeUseSkill(SkillRuntime.GetId(playerInfo.Skill), player);
            }
        }

        private static HookResult BulletImpact(EventBulletImpact @event, GameEventInfo info)
        {
            lock (setLock)
            {
                Instance.SkillDispatcher.DispatchBulletImpact(GetActiveSkillIds(), @event);
                return HookResult.Continue;
            }
        }

        // Heroes chosen at the END of the previous round and applied at the start of
        // the next one; filled by PrecomputeNextRoundSkills in RoundEvents.cs.
        private static readonly Dictionary<uint, jSkill_SkillInfo> nextRoundPicks = [];

        // Renders the three HUD lines into the single center-HTML slot and sends them.
        // Called from PlayerOnTick.UpdatePlayerHud.
        //   headerLine - small label ("Your skill")
        //   centerLine - the hero name, already coloured by the caller
        //   extraLine  - description or a hero's own live status text
        // The "<jRS/>" prefix marks the output as ours so GetPrintToCenterHtml above
        // can tell our HUD apart from another plugin's. The empty <font> paddings widen
        // the box so short names do not jitter, and Illiterate scrambles the text here
        // rather than at every call site.
        public static void UpdateSkillHUD(CCSPlayerController? player, jSkill_PlayerInfo? skillPlayer, string? headerLine, string? centerLine, string? extraLine, bool isDescription)
        {
            lock (setLock)
            {
                if (player == null || !player.IsValid) return;

                if (Illiterate.CheckIlliterateSkill(player))
                {
                    headerLine = Illiterate.GetRandomText(headerLine);
                    centerLine = Illiterate.GetRandomText(centerLine);
                    extraLine = Illiterate.GetRandomText(extraLine);
                }

                var config = Config.LoadedConfig.HtmlHudCustomisation;
                var emptySymbol = $"<font class='fontSize-{(string.IsNullOrEmpty(headerLine) || string.IsNullOrEmpty(config.HeaderLineSize) ? "l" : "ml")}'> </font>";
                var emptySymbol2 = $"<font class='fontSize-ml'> </font>";

                string infoLine = string.IsNullOrEmpty(headerLine) || string.IsNullOrEmpty(config.HeaderLineSize)
                    ? ""
                    : $"<font class='fontWeight-Bold fontSize-{config.HeaderLineSize}' color='{config.HeaderLineColor}'>{headerLine}:</font><br>";

                string skillLine = $"{emptySymbol2}<font class='fontWeight-Bold fontSize-{config.SkillLineSize}'>{centerLine}</font>{emptySymbol2}";

                string remainingLine = string.IsNullOrWhiteSpace(extraLine)
                    ? ""
                    : $"<br>{emptySymbol}<font class='fontSize-{(isDescription ? config.SkillDescriptionLineSize : config.InfoLineSize)}' color='{(isDescription ? config.SkillDescriptionLineColor : config.InfoLineColor)}'>{extraLine}</font>{emptySymbol}";

                var hudContent = "<jRS/>" + infoLine + skillLine + remainingLine;
                player.PrintToCenterHtml(hudContent);
            }
        }
    }
}
