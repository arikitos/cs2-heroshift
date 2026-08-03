using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * LongKnife - Your knife reaches across the whole map.
     *
     * LOGIC
     *   WeaponFire: traces up to maxDistance from your crosshair and applies the
     *     knife hit.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   maxDistance  = 4096f
     *                    -> knife reach in game units (4096 = practically
     *                       map-wide)
     *   friendlyFire = true
     *                    -> true = can also hit teammates
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.None
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = -1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class LongKnife : ISkill
    {
        private const Skills skillName = Skills.LongKnife;

        private static LongKnifeOptions Options => SkillConfigurationResolver.Get<LongKnifeOptions>(BuiltInSkillIds.LongKnife);
        private static bool hooked = false;
        private const int actionCode = 503;
        private static readonly ConcurrentDictionary<uint, byte> playersInAction = [];
        private static readonly MemoryFunctionVoid<IntPtr, short>? Shoot_Secondary = ResolveShootSecondary();

        private static MemoryFunctionVoid<IntPtr, short>? ResolveShootSecondary()
        {
            try { return new(GameData.GetSignature("Shoot_Secondary")); }
            catch (Exception ex) { Server.PrintToConsole($"[HeroShift] Shoot_Secondary signature unresolved: {ex.Message}"); return null; }
        }

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            playersInAction.Clear();

            if (!hooked) return;

            hooked = false;
            Shoot_Secondary?.Unhook(ShootSecondary, HookMode.Pre);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (hooked || Shoot_Secondary == null) return;
            hooked = true;
            Shoot_Secondary.Hook(ShootSecondary, HookMode.Pre);
            playersInAction.TryAdd(player.Index, 0);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            playersInAction.TryRemove(player.Index, out _);
            if (playersInAction.IsEmpty)
            {
                Shoot_Secondary?.Unhook(ShootSecondary, HookMode.Pre);
                hooked = false;
            }
        }

        public static HookResult ShootSecondary(DynamicHook hook)
        {
            var weapon = hook.GetParam<CBasePlayerWeapon>(0);
            var action = hook.GetParam<short>(1);

            if (action != actionCode || (weapon?.DesignerName != "weapon_knife" && weapon?.DesignerName != "weapon_bayonet")) return HookResult.Continue;
            if (weapon.OwnerEntity.Value == null || !weapon.OwnerEntity.Value.IsValid) return HookResult.Continue;

            var pawn = weapon.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return HookResult.Continue;

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid) return HookResult.Continue;

            var eventPlayer = PlayerManager.GetPlayerEvent(player);
            if (eventPlayer == null || !eventPlayer.IsValid) return HookResult.Continue;

            var playerInfo = PlayerManager.GetPlayerByIndex(eventPlayer.Index);
            if (playerInfo == null || playerInfo.Skill != skillName) return HookResult.Continue;

            KillfeedIcons? killfeedIcon = KillfeedIconsExtensions.FromWeapon(weapon);
            KnifeHit(eventPlayer, true, killfeedIcon);
            return HookResult.Continue;
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.WeaponServices == null) return;

            var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;
            if (activeWeapon == null || !activeWeapon.IsValid || (activeWeapon.DesignerName != "weapon_knife" && activeWeapon.DesignerName != "weapon_bayonet")) return;

            KillfeedIcons? killfeedIcon = KillfeedIconsExtensions.FromWeapon(activeWeapon);
            KnifeHit(player, false, killfeedIcon);
        }

        private unsafe static void KnifeHit(CCSPlayerController player, bool heavyHit, KillfeedIcons? killfeedIcon)
        {
            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                return;

            var result = RayTrace.EyeTrace(player);
            if (result == null || !result.HasValue)
                return;

            if (!result.Value.HitPlayer(out CCSPlayerController? target) || target == null)
                return;

            if (target.Handle == player.Handle || result.Value.Distance() <= 70)
                return;

            if (target.PlayerPawn.Value == null || !target.PlayerPawn.Value.IsValid)
                return;

            if (!Options.FriendlyFire && player.Team == target.Team) return;

            target.PlayerPawn.Value.EmitSound("Player.DamageBody.Onlooker");
            SkillUtils.TakeHealth(target.PlayerPawn.Value, heavyHit ? Instance.Random.Next(45, 55) : Instance.Random.Next(21, 34), player, killfeedIcon ?? KillfeedIcons.Knife);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#c9f8ff", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, float maxDistance = 4096f, bool friendlyFire = true) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float MaxDistance { get; set; } = maxDistance;
            public bool FriendlyFire { get; set; } = friendlyFire;
        }
    }
}