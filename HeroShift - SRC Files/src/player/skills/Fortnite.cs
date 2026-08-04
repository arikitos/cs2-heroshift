using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using System.Collections.Concurrent;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Fortnite - Build an instant barricade wall in front of you.
     *
     * LOGIC
     *   UseSkill: spawns the wall prop at your aim position.
     *   OnTakeDamage: the wall absorbs damage until barricadeHealth runs out.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   cooldown        = 2f
     *                       -> seconds before you can build again
     *   barricadeHealth = 115
     *                       -> hit points of the spawned wall before it breaks
     *   propModel       = "models/props/de_aztec/hr_aztec/aztec_scaffolding/aztec_scaffold_wall_support_128.vmdl"
     *                       -> model path of the barricade prop
     *
     *   Shared settings:
     *   active       = true
     *                    -> false disables this hero entirely (it will not be
     *                       handed out)
     *   onlyTeam     = CsTeam.None
     *                    -> restrict to one side: None = both, Terrorist /
     *                       CounterTerrorist
     *   maxPerServer = 5
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class Fortnite : ISkill
    {
        private const Skills skillName = Skills.Fortnite;
        private static FortniteOptions Options => SkillConfigurationResolver.Get<FortniteOptions>(BuiltInSkillIds.Fortnite);
        private static readonly ConcurrentDictionary<uint, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly ConcurrentDictionary<uint, int> barricades = [];
        public static bool skillInThisRound = false;
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color, false);
            Instance.AddToManifest(Options.PropModel);
        }

        public static void NewRound()
        {
            skillInThisRound = false;
            lock (setLock)
            {
                SkillPlayerInfo.Clear();
                barricades.Clear();
            }
        }

        public static void OnTick()
        {
            foreach (var player in PlayerManager.GetTickPlayers())
            {
                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill == skillName)
                    if (SkillPlayerInfo.TryGetValue(player.Index, out var skillInfo))
                        UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            skillInThisRound = true;
            SkillPlayerInfo.TryAdd(player.Index, new PlayerSkillInfo
            {
                SteamID = player.Index,
                CanUse = true,
                Cooldown = DateTime.MinValue,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryRemove(player.Index, out _);
            SkillUtils.ResetPrintHTML(player);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)Math.Ceiling((skillInfo.Cooldown.AddSeconds(Options.Cooldown) - DateTime.Now).TotalSeconds);
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            if (cooldown == 0)
                playerInfo.PrintHTML = null;
            else
                playerInfo.PrintHTML = $"{player.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}";
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.Index, out var skillInfo))
            {
                if (!player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

                if (skillInfo.CanUse)
                {
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;
                    CreateBox(player);
                }
            }
        }

        private static void CreateBox(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var box = EntityManager.CreateTrackedPropOverride(player.Index);
            if (box == null || playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null) return;

            float distance = 50;
            Vector pos = playerPawn.AbsOrigin + SkillUtils.GetForwardVector(playerPawn.AbsRotation) * distance;
            QAngle angle = new(playerPawn.AbsRotation.X, playerPawn.V_angle.Y + 90, playerPawn.AbsRotation.Z);

            box.Entity!.Name = box.Globalname = $"FortniteWall_{Server.TickCount}";
            box.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            box.DispatchSpawn();
            barricades.TryAdd(box.Index, Options.BarricadeHealth);
            Server.NextFrame(() =>
            {
                if (box == null || !box.IsValid) return;
                box.SetModel(Options.PropModel);
                box.Teleport(pos, angle, null);
            });
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
                return;

            if (string.IsNullOrEmpty(param.Entity.Name)) return;
            if (!param.Entity.Name.StartsWith("FortniteWall")) return;

            var box = param.As<CDynamicProp>();
            if (box == null || !box.IsValid) return;
            box.EmitSound("Wood_Plank.BulletImpact", volume: 1f);

            if (barricades.TryGetValue(box.Index, out int health))
            {
                int newHealth = health - (int)param2.Damage;

                if (newHealth <= 0)
                {
                    barricades.TryRemove(box.Index, out _);
                    EntityManager.DestroyEntity(box.Index);
                }
                else
                    barricades.AddOrUpdate(box.Index, newHealth, (k, v) => newHealth);
            }
            else EntityManager.DestroyEntity(box.Index);
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
        }
    }
}