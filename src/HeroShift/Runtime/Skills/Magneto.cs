using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Magneto - Nearby dropped weapons and items fly to you.
     *
     * LOGIC
     *   OnEntitySpawned/OnTick: pulls pickups within 'radius' toward you.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   radius = 100
     *              -> pickup attraction radius in game units
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
    public class Magneto : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Magneto;
        private static MagnetoOptions Options => SkillConfigurationResolver.Get<MagnetoOptions>(BuiltInSkillIds.Magneto);
        private readonly static ConcurrentDictionary<uint, byte> nades = [];
        private readonly static ConcurrentDictionary<uint, byte> players = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            nades.Clear();
            players.Clear();
        }

        public static void OnTick()
        {
            if (Server.TickCount % 5 != 0) return;
            float radius = Options.Radius;

            foreach (var nadeIndex in nades.Keys)
            {
                var nade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)nadeIndex);
                if (nade == null || !nade.IsValid)
                {
                    nades.TryRemove(nadeIndex, out _);
                    continue;
                }

                foreach (var playerIndex in players.Keys)
                {
                    var player = Utilities.GetPlayerFromIndex((int)playerIndex);
                    if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)
                    {
                        players.TryRemove(playerIndex, out _);
                        continue;
                    }

                    var pawn = player.PlayerPawn.Value;
                    double distanceMoved = SkillUtils.GetDistance(nade.AbsOrigin ?? Vector.Zero, pawn.AbsOrigin ?? Vector.Zero);

                    if (distanceMoved < radius && nade.TeamNum != player.TeamNum)
                    {
                        nade.Teleport(null, null, -nade.AbsVelocity);
                        nades.TryRemove(nadeIndex, out _);
                    }
                }
            }
        }

        public static void OnEntitySpawned(CEntityInstance @event)
        {
            var name = @event.DesignerName;
            if (!name.EndsWith("_projectile")) return;

            var grenade = @event.As<CBaseCSGrenadeProjectile>();
            if (grenade == null || !grenade.IsValid) return;

            nades.TryAdd(grenade.Index, 0);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            players.TryAdd(player.Index, 0);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            players.TryRemove(player.Index, out _);
        }
    }
}