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
     * Phoenix - A chance to revive instead of dying.
     *
     * LOGIC
     *   EnableSkill: rolls the revive chance between chanceFrom and chanceTo.
     *   OnTakeDamage/TryConsumeRevive: lethal damage is converted into a revive
     *     if the roll wins.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   chanceFrom = .2f
     *                  -> lowest revive chance that can be rolled (0.2 = 20%)
     *   chanceTo   = .4f
     *                  -> highest revive chance that can be rolled
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
    public class Phoenix : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Phoenix;
        private static PhoenixOptions Options => SkillConfigurationResolver.Get<PhoenixOptions>(BuiltInSkillIds.Phoenix);
        private const float DefaultHeadshotMultiplier = 4f;
        private static readonly ConcurrentDictionary<uint, int> phoenixTicks = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
        }

        public static void NewRound()
        {
            phoenixTicks.Clear();
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            phoenixTicks.TryRemove(player.Index, out _);
        }

        public static void PlayerDisconnect(uint playerIndex)
        {
            phoenixTicks.TryRemove(playerIndex, out _);
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            var victimEntity = h.GetParam<CEntityInstance>(0);
            var info = h.GetParam<CTakeDamageInfo>(1);
            if (victimEntity == null || !victimEntity.IsValid || info == null) return;

            var victimPawn = victimEntity.As<CCSPlayerPawn>();
            if (victimPawn == null || !victimPawn.IsValid || victimPawn.DesignerName != "player") return;

            var victimController = victimPawn.Controller.Value;
            if (victimController == null || !victimController.IsValid) return;

            var victim = victimController.As<CCSPlayerController>();
            if (victim == null || !victim.IsValid || !victim.PawnIsAlive) return;

            var victimInfo = PlayerManager.GetPlayerByIndex(PlayerManager.GetPlayerEvent(victim)?.Index ?? victim.Index);
            if (victimInfo == null || victimInfo.Skill != skillName) return;

            if (SkillUtils.IsFriendlyFireBlocked(info, victimPawn)) return;

            float effectiveDamage = info.Damage;
            if (SkillUtils.GetHitGroup(info) == HitGroup_t.HITGROUP_HEAD)
                effectiveDamage *= GetHeadshotMultiplier(info);

            if (effectiveDamage < victimPawn.Health) return;

            if (TryConsumeRevive(victim, victimPawn))
                info.Damage = 0;
        }

        public static bool TryConsumeRevive(CCSPlayerController? victim, CCSPlayerPawn? victimPawn)
        {
            if (victim == null || !victim.IsValid || !victim.PawnIsAlive) return false;
            if (victimPawn == null || !victimPawn.IsValid) return false;

            var victimInfo = PlayerManager.GetPlayerByIndex(PlayerManager.GetPlayerEvent(victim)?.Index ?? victim.Index);
            if (victimInfo == null || victimInfo.Skill != skillName) return false;

            if (phoenixTicks.TryGetValue(victim.Index, out int savedTick) && savedTick + 4 > Server.TickCount)
                return true;

            if (victim.TeamChanged) return false;
            if (victimInfo.SkillChance is float chance && Instance.Random.NextDouble() > chance) return false; // roll failed -> die

            phoenixTicks[victim.Index] = Server.TickCount;

            victimPawn.Health = 100;
            Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");

            SkillUtils.PrintToChat(victim, victim.GetTranslation("phoenix_respawn"));

            Server.NextFrame(() =>
            {
                if (victim == null || !victim.IsValid) return;
                var pawn = victim.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) return;

                var spawnpoint = SkillUtils.GetSpawnPointVector(victim);
                if (spawnpoint == null) return;

                pawn.Teleport(spawnpoint);
            });

            return true;
        }

        private static float GetHeadshotMultiplier(CTakeDamageInfo info)
        {
            var ability = info.Ability?.Value;
            if (ability == null || !ability.IsValid) return DefaultHeadshotMultiplier;

            var weapon = ability.As<CCSWeaponBase>();
            if (weapon == null || !weapon.IsValid) return DefaultHeadshotMultiplier;

            var vdata = weapon.GetVData<CCSWeaponBaseVData>();
            if (vdata == null || vdata.HeadshotMultiplier <= 0) return DefaultHeadshotMultiplier;

            return vdata.HeadshotMultiplier;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            float newChance = (float)Instance.Random.NextDouble() * (Options.ChanceTo - Options.ChanceFrom) + Options.ChanceFrom;
            playerInfo.SkillChance = newChance;

            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetSkillName(skillName)}{ChatColors.Lime}: {player.GetSkillDescription(skillName, newChance)}",
                border: !PlayerManager.GetTickPlayers().Any(p => p.Team == player.Team && p != player) ? "tb" : "t");
        }
    }
}