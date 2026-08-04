using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Berserker - The faster you move, the more damage you deal and take.
     *
     * LOGIC
     *   OnTick: reads your current velocity and scales speed/damage from it.
     *   OnTakeDamage: applies the velocity-based damage multiplier.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   maxSpeedVelocity  = 2f
     *                         -> upper bound of the speed bonus multiplier
     *   maxDamageVelocity = 2f
     *                         -> upper bound of the damage bonus multiplier
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
    public class Berserker : ISkill
    {
        private const Skills skillName = Skills.Berserker;
        private static BerserkerOptions Options => SkillConfigurationResolver.Get<BerserkerOptions>(BuiltInSkillIds.Berserker);
        public static readonly ConcurrentDictionary<uint, int> jumpedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void NewRound()
        {
            jumpedPlayers.Clear();
        }

        private static float CalculateNewVelocity(CCSPlayerPawn pawn, float maxValue)
        {
            if (pawn!.Health <= 0) return 1f;

            float healthPercentage = pawn.MaxHealth > 0
                ? Math.Clamp((float)pawn.Health / pawn.MaxHealth, 0f, 1f)
                : 0f;

            float newValue = 1f + (maxValue - 1f) * (1f - healthPercentage);
            return Math.Max(1, Math.Min(maxValue, newValue));
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
                return;

            CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(param.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return;

            CCSPlayerController attacker = PlayerManager.GetPlayerEvent(attackerPawn.Controller.Value.As<CCSPlayerController>())!;

            var playerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
            if (playerInfo == null) return;

            if (playerInfo.Skill == skillName)
            {
                float damageMultiplier = CalculateNewVelocity(attackerPawn, Options.MaxDamageVelocity);
                param2.Damage *= damageMultiplier;
            }
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;
            if (!jumpedPlayers.TryGetValue(player.Index, out _)) return;
            jumpedPlayers.AddOrUpdate(player.Index, Server.TickCount + 20, (k, v) => Server.TickCount + 20);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerPawn == null || playerInfo == null) return;
            jumpedPlayers.TryAdd(player.Index, 0);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return;
            playerPawn.VelocityModifier = 1;
            jumpedPlayers.TryRemove(player.Index, out _);
        }

        public static void OnTick()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill != skillName) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || playerPawn.VelocityModifier == 0) continue;

                var buttons = player.Buttons;
                float newSpeedVelocity = CalculateNewVelocity(playerPawn, Options.MaxSpeedVelocity);

                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = newSpeedVelocity;

                if (jumpedPlayers.TryGetValue(player.Index, out var time) && time > Server.TickCount)
                    continue;

                if (!((PlayerFlags)player.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
                    playerPawn.AbsVelocity.Z = Math.Min(playerPawn.AbsVelocity.Z, 10);
            }
        }
    }
}