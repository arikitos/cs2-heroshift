using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using System.Drawing;
using static src.HeroShift;

using src.SkillsCore;
namespace src.player.skills
{
    /*
     * C4Camouflage - You look like the dropped C4 to the enemy team.
     *
     * LOGIC
     *   OnTick/CheckTransmit: hides your real model and shows the bomb prop
     *     instead.
     *   PlayerHurt: taking damage breaks the disguise.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.Terrorist
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = 1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Uncommon
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class C4Camouflage : ISkill
    {
        private const Skills skillName = Skills.C4Camouflage;
        private static readonly ConcurrentDictionary<uint, byte> invisiblePlayers = [];
        private const string bloodParticle = "particles/blood_impact/blood_impact_high.vpcf";
        private const string cameraViewModel = "models/sprays/spray_plane.vmdl";

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
            Instance.AddToManifest(bloodParticle);
            Instance.AddToManifest(cameraViewModel);
        }

        public static void NewRound()
        {
            invisiblePlayers.Clear();
        }

        public static void WeaponPickup(EventItemPickup @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            Event.EnableTransmit();
            if (EntityManager.GetPlayerEntities(player.Index, "empty_prop").Count == 0)
                CreatePlayerPosProp(player);
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Item;

            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            if (weapon == "c4" && player.PlayerPawn?.Value?.Health > 0)
            {
                invisiblePlayers.TryAdd(player.Index, 0);
                SkillUtils.SetPlayerInvisibility(player, .5f);
            }
            else
            {
                invisiblePlayers.TryRemove(player.Index, out _);
                SkillUtils.SetPlayerInvisibility(player, 0);
            }
        }

        public static void OnTick()
        {
            if (Server.TickCount % 2 != 0) return;

            var bomb = invisiblePlayers.IsEmpty ? null : PlayerManager.GetTickBomb();

            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player.PlayerPawn?.Value?.Health <= 0 && invisiblePlayers.ContainsKey(player.Index))
                {
                    invisiblePlayers.TryRemove(player.Index, out _);
                    SkillUtils.SetPlayerInvisibility(player, 0);
                }

                // CheckTransmit hides the model but the radar blip comes from spotted state, so clear it every tick.
                if (invisiblePlayers.ContainsKey(player.Index))
                    ClearSpottedState(player, bomb);

                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill != skillName) continue;

                var props = EntityManager.GetPlayerEntities(player.Index, "empty_prop");
                if (props.Count == 0) continue;

                var prop = Utilities.GetEntityFromIndex<CDynamicProp>((int)props[0]);
                if (prop == null || !prop.IsValid) continue;

                var pawn = player.PlayerPawn?.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) continue;

                Vector newPos = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 30);
                QAngle newAngle = new(0, pawn.V_angle.Y, 0);
                prop.Teleport(newPos, newAngle, null);
            }
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            if (invisiblePlayers.IsEmpty) return;

            var bomb = PlayerManager.GetTickBomb();

            foreach (var (info, player) in infoList)
            {
                if (player == null || !player.IsValid || player.Team == CsTeam.Spectator) continue;

                var targetHandle = player.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;

                foreach (var playerIndex in invisiblePlayers.Keys)
                {
                    var playerController = PlayerManager.GetPlayerEvent(Utilities.GetPlayerFromIndex((int)playerIndex));
                    if (playerController == null || !playerController.IsValid || playerController.Index == player.Index)
                        continue;

                    if (player.Team == playerController.Team)
                        continue;

                    var playerPawn = playerController.PlayerPawn.Value;
                    if (playerPawn == null || !playerPawn.IsValid) continue;

                    // Only the actively spectated pawn stays transmitted; hiding it breaks the camera.
                    if (targetHandle != nint.Zero && playerPawn.Handle == targetHandle)
                        continue;

                    var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)playerPawn.Index);
                    if (entity == null || !entity.IsValid) continue;

                    if (info.TransmitEntities.Contains(entity.Index))
                        info.TransmitEntities.Remove(entity.Index);

                    SkillUtils.HideCarriedEntities(info, playerPawn);

                    if (bomb == null) continue;

                    if (info.TransmitEntities.Contains(bomb.Index))
                        info.TransmitEntities.Remove(bomb.Index);
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            Event.EnableTransmit();

            if (player == null || !player.IsValid) return;
            var playerPawn = player.PlayerPawn.Value;

            if (playerPawn == null || !playerPawn.IsValid) return;

            if (EntityManager.GetPlayerEntities(player.Index, "empty_prop").Count == 0)
                CreatePlayerPosProp(player);

            if (playerPawn.WeaponServices == null || playerPawn.WeaponServices.ActiveWeapon == null || !playerPawn.WeaponServices.ActiveWeapon.IsValid) return;
            if (playerPawn.WeaponServices.ActiveWeapon.Value == null || !playerPawn.WeaponServices.ActiveWeapon.Value.IsValid) return;

            var activeWeapon = playerPawn.WeaponServices.ActiveWeapon.Value;
            if (activeWeapon.DesignerName != "weapon_c4") return;

            invisiblePlayers.TryAdd(player.Index, 0);
            SkillUtils.SetPlayerInvisibility(player, .5f);

            SkillUtils.ForceFullUpdateToAll();
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            invisiblePlayers.TryRemove(player.Index, out _);
            SkillUtils.SetPlayerInvisibility(player, 0);
            EntityManager.DestroyPlayerEntities(player.Index);
        }

        private static void CreatePlayerPosProp(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var emptyProp = EntityManager.CreateTrackedDynamicProp(player.Index);
            if (emptyProp == null || !emptyProp.IsValid) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null) return;

            emptyProp.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(emptyProp.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            emptyProp.SetModel(cameraViewModel);
            emptyProp.Render = Color.FromArgb(1, 255, 255, 255);

            Vector newPos = new(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z + 30);
            QAngle newAngle = new(playerPawn.AbsRotation.X, playerPawn.AbsRotation.Y, playerPawn.AbsRotation.Z);

            emptyProp.Teleport(newPos, newAngle);
            emptyProp.DispatchSpawn();

            Utilities.SetStateChanged(emptyProp, "CBaseEntity", "m_CBodyComponent");

            EntityManager.RegisterExisting(emptyProp, player.Index, "empty_prop");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;
            if (!invisiblePlayers.ContainsKey(player.Index)) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null) return;

            var particle = EntityManager.CreateTrackedParticleSystem(player.Index, bloodParticle, autoDestroySeconds: 3f);
            if (particle == null) return;

            Vector pos = new(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z + 50);
            particle.Teleport(pos);
            particle.AcceptInput("Start");
        }

        // Wipe spotted state (pawn + carried bomb) so a disguised carrier produces no radar blip.
        private static void ClearSpottedState(CCSPlayerController player, CC4? bomb)
        {
            var pawn = player.PlayerPawn?.Value;
            if (pawn != null && pawn.IsValid)
            {
                pawn.EntitySpottedState.Spotted = false;
                pawn.EntitySpottedState.SpottedByMask[0] = 0;
                pawn.EntitySpottedState.SpottedByMask[1] = 0;
            }

            if (bomb != null && bomb.IsValid && bomb.OwnerEntity?.Index == player.Index)
            {
                bomb.EntitySpottedState.Spotted = false;
                bomb.EntitySpottedState.SpottedByMask[0] = 0;
                bomb.EntitySpottedState.SpottedByMask[1] = 0;
            }
        }
    }
}