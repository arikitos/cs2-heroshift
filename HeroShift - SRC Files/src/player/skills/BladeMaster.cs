using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * BladeMaster - Knife hits against you are reflected back at the attacker.
     *
     * LOGIC
     *   PlayerHurtPre: on a knife hit, rolls the reflect chance by hitgroup
     *     (torso vs legs) and bounces the damage back.
     *   OnTick: applies the movement speed change while holding a knife.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   torseReflectionChance = .95f
     *                             -> reflect chance for hits on the torso (0.95 =
     *                                95%)
     *   legReflectionChance   = .70f
     *                             -> reflect chance for hits on the legs
     *   velocityModifier      = .85f
     *                             -> movement speed multiplier while the skill is
     *                                active
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
    public class BladeMaster : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.BladeMaster;
        private static BladeMasterOptions Options => SkillConfigurationResolver.Get<BladeMasterOptions>(BuiltInSkillIds.BladeMaster);
        private static readonly string[] noReflectionWeapon = ["inferno", "flashbang", "smokegrenade", "decoy", "hegrenade", "knife", "taser", "bayonet"];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void OnTick()
        {
            var modifier = Options.VelocityModifier;

            foreach (var player in PlayerManager.GetTickPlayers())
            {
                if (player == null || !player.IsValid) continue;

                var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
                if (playerInfo?.Skill != skillName) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || !playerPawn.IsValid || playerPawn.VelocityModifier == 0) continue;

                var weaponServices = playerPawn.WeaponServices;
                if (weaponServices == null) continue;

                if (weaponServices.ActiveWeapon == null
                    || !weaponServices.ActiveWeapon.IsValid
                    || weaponServices.ActiveWeapon.Value == null
                    || !weaponServices.ActiveWeapon.Value.IsValid
                    || (weaponServices.ActiveWeapon.Value.DesignerName != "weapon_knife"
                        && weaponServices.ActiveWeapon.Value.DesignerName != "weapon_bayonet"))
                    continue;

                playerPawn.VelocityModifier = modifier;
            }
        }

        public static bool PlayerHurtPre(EventPlayerHurt @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            var victim = PlayerManager.GetPlayerEvent(@event.Userid);
            var weapon = @event.Weapon;
            int hitgroup = @event.Hitgroup;

            if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid || attacker == victim) return false;

            var playerInfo = PlayerManager.GetPlayerByIndex(victim.Index);
            if (playerInfo?.Skill != skillName) return false;

            if (string.IsNullOrEmpty(weapon) || noReflectionWeapon.Contains(weapon)) return false;

            float chance = (hitgroup == (int)HitGroup_t.HITGROUP_LEFTLEG || hitgroup == (int)HitGroup_t.HITGROUP_RIGHTLEG)
                ? Options.LegReflectionChance
                : Options.TorseReflectionChance;

            var victimPawn = victim.PlayerPawn?.Value;
            if (victimPawn == null || !victimPawn.IsValid || Instance.Random.NextDouble() > chance)
                return false;

            var weaponServices = victimPawn.WeaponServices;
            if (weaponServices == null) return false;

            if (weaponServices.ActiveWeapon == null
                || !weaponServices.ActiveWeapon.IsValid
                || weaponServices.ActiveWeapon.Value == null
                || !weaponServices.ActiveWeapon.Value.IsValid
                || (weaponServices.ActiveWeapon.Value.DesignerName != "weapon_knife"
                    && weaponServices.ActiveWeapon.Value.DesignerName != "weapon_bayonet"))
                return false;

            SkillUtils.RestoreHealth(victim);
            return true;
        }
    }
}