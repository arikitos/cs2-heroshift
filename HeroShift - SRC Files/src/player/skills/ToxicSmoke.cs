using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;
using src.utils;
using System.Collections.Concurrent;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * ToxicSmoke - Your smoke grenades poison everyone standing inside them.
     *
     * LOGIC
     *   SmokegrenadeDetonate: registers the smoke position in the 'smokes'
     *     dictionary.
     *   SmokegrenadeExpired: removes it again.
     *   OnEntitySpawned: recolours your smoke purple so it is recognisable.
     *   OnTick: every tickCooldown ticks, everyone within smokeRadius of a
     *     registered smoke takes smokeDamage - the thrower is credited for the
     *     kill.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   smokeDamage  = 2
     *                    -> damage per tick to players inside the smoke
     *   smokeRadius  = 180
     *                    -> radius of the damaging cloud (game units)
     *   tickCooldown = 17
     *                    -> server ticks between damage pulses (64 ticks = 1
     *                       second, so 17 is ~4x/sec)
     *   grenadeLimit = 1
     *                    -> how many smoke grenades the hero gets
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
    public class ToxicSmoke : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.ToxicSmoke;
        private static ToxicSmokeOptions Options => SkillConfigurationResolver.Get<ToxicSmokeOptions>(BuiltInSkillIds.ToxicSmoke);
        private static readonly ConcurrentDictionary<Vector, uint> smokes = [];
        private readonly static ConcurrentDictionary<uint, int> playersWithSkill = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void NewRound()
        {
            smokes.Clear();
        }

        public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            smokes.TryAdd(new Vector(@event.X, @event.Y, @event.Z), player.Index);
        }

        public static void SmokegrenadeExpired(EventSmokegrenadeExpired @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            foreach (var smoke in smokes.Keys.Where(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z))
                smokes.TryRemove(smoke, out _);
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            var name = entity.DesignerName;
            if (name != "smokegrenade_projectile") return;

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade == null || !grenade.IsValid || grenade.OwnerEntity == null || !grenade.OwnerEntity.IsValid || grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid) return;

            var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex((PlayerManager.GetPlayerEvent(player)?.Index ?? player.Index));
            if (playerInfo?.Skill != skillName) return;

            Server.NextFrame(() =>
            {
                if (entity == null || !entity.IsValid) return;
                var smoke = entity.As<CSmokeGrenadeProjectile>();
                smoke.SmokeColor.X = 255;
                smoke.SmokeColor.Y = 0;
                smoke.SmokeColor.Z = 255;
            });
        }

        public static void OnTick()
        {
            int tick = Math.Max(1, Options.TickCooldown);
            if (Server.TickCount % tick != 0) return;

            float smokeRadius = Options.SmokeRadius;
            int smokeDamage = Options.SmokeDamage;

            foreach (var smoke in smokes)
            {
                var thrower = Utilities.GetPlayerFromIndex((int)smoke.Value);
                if (thrower != null && !thrower.IsValid) thrower = null;

                foreach (var player in PlayerManager.GetTickPlayers().Where(p => p.IsValid))
                {
                    var eventPlayer = PlayerManager.GetPlayerEvent(player);
                    if (eventPlayer == null || !eventPlayer.IsValid) continue;

                    var pawn = eventPlayer.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) continue;

                    if (SkillUtils.GetDistance(smoke.Key, pawn.AbsOrigin) <= smokeRadius)
                        if (SkillUtils.TakeHealth(pawn, smokeDamage, thrower, KillfeedIcons.Smokegrenade))
                            player.EmitSound("Player.DamageBody.Onlooker");
                }
            }
        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Weapon;
            if (weapon != "smokegrenade") return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
            {
                playersWithSkill[player.Index] = grenadesLeft - 1;
                player!.GiveNamedItem($"weapon_{weapon}");
                SkillUtils.UpdateGrenadeCount(player, CsItem.SmokeGrenade, grenadesLeft - 1);
            }
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Item;
            if (player == null || !player.IsValid) return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.SmokeGrenade, grenadesLeft);
        }

        public static void WeaponPickup(EventItemPickup @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var weapon = @event.Item;
            if (string.IsNullOrEmpty(weapon) || weapon != "smokegrenade") return;

            if (playersWithSkill.TryGetValue(player.Index, out int grenadesLeft) && grenadesLeft > 1)
                SkillUtils.UpdateGrenadeCount(player, CsItem.SmokeGrenade, grenadesLeft);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            int grenadeLimit = Options.GrenadeLimit;
            playersWithSkill.TryAdd(player.Index, grenadeLimit);

            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
            SkillUtils.UpdateGrenadeCount(player, CsItem.SmokeGrenade, grenadeLimit);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            playersWithSkill.TryRemove(player.Index, out _);
            SkillUtils.UpdateGrenadeCount(player, CsItem.SmokeGrenade, 1);
        }
    }
}