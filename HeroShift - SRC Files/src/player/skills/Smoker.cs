using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using System.Collections.Concurrent;
using static src.HeroShift;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Smoker - Your smoke grenade keeps re-spawning more smoke clouds after it
     * lands.
     *
     * LOGIC
     *   SmokegrenadeDetonate: starts timers (playerSmokes) that create follow-up
     *     smokes.
     *   NewRound: kills all pending timers so smokes do not carry into the next
     *     round.
     *   GrenadeThrown/WeaponEquip/WeaponPickup: keeps the grenade count in the
     *     HUD.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
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
     *   maxPerServer = 1
     *                    -> how many players may have this hero at once (-1 =
     *                       unlimited)
     *   rarity       = Rarity.Common
     *                    -> draw chance bucket - see RarityManager
     *                       (Common..Legendary)
     */
    public class Smoker : ISkill
    {
        private const Skills skillName = Skills.Smoker;
        private static SmokerOptions Options => SkillConfigurationResolver.Get<SmokerOptions>(BuiltInSkillIds.Smoker);
        private readonly static ConcurrentDictionary<uint, List<Timer>> playerSmokes = [];
        private readonly static ConcurrentDictionary<uint, int> playersWithSkill = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            lock (setLock)
            {
                foreach (var timers in playerSmokes.Values)
                    timers.ForEach(t => t?.Kill());
                playerSmokes.Clear();
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
            if (playerSmokes.TryRemove(player.Index, out var timers))
                timers.ForEach(t => t?.Kill());

            playersWithSkill.TryRemove(player.Index, out _);
            SkillUtils.UpdateGrenadeCount(player, CsItem.SmokeGrenade, 1);
        }

        public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo?.Skill != skillName) return;

            uint playerIndex = player.Index;

            Vector pos = new(@event.X, @event.Y, @event.Z);
            float refillInterval = 15.5f;

            Timer? smokeTimer = null;
            smokeTimer = Instance.AddTimer(refillInterval, () =>
            {
                var player = Utilities.GetPlayerFromIndex((int)playerIndex);

                if (player == null || !player.IsValid)
                {
                    smokeTimer?.Kill();
                    return;
                }

                SkillUtils.CreateSmokeGrenadeProjectile(pos, QAngle.Zero, Vector.Zero, player.TeamNum);
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

            playerSmokes.AddOrUpdate(player.Index, [smokeTimer], (_, list) => { list.Add(smokeTimer); return list; });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#b5ab8f", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = 1, Rarity rarity = Rarity.Common, int grenadeLimit = 1) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public int GrenadeLimit { get; set; } = grenadeLimit;
        }
    }
}