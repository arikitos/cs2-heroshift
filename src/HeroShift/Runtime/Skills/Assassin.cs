using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Assassin - Hitting an enemy from behind deals multiplied damage.
     *
     * LOGIC
     *   OnTakeDamage: compares your view angle to the victim's, and if you are
     *     within toleranceDeg of their back, multiplies the damage.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   damageMultiplier = 2f
     *                        -> damage multiplier for a successful backstab-style
     *                           hit
     *   toleranceDeg     = 45f
     *                        -> angle window (degrees) that still counts as 'from
     *                           behind'
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
    public class Assassin : ISkill
    {
        private static readonly SkillId skillName = BuiltInSkillIds.Assassin;
        private static AssassinOptions Options => SkillConfigurationResolver.Get<AssassinOptions>(BuiltInSkillIds.Assassin);
        private static readonly string[] nadeWeapons =
        [
            "weapon_inferno", "weapon_molotov", "weapon_incgrenade", "weapon_flashbang",
            "weapon_smokegrenade", "weapon_decoy", "weapon_hegrenade"
        ];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
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

            if (attackerPawn.Controller?.Value == null || victimPawn.Controller?.Value == null)
                return;

            var attacker = PlayerManager.GetPlayerEvent(attackerPawn.Controller.Value.As<CCSPlayerController>());
            var victim = PlayerManager.GetPlayerEvent(victimPawn.Controller.Value.As<CCSPlayerController>());
            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid) return;
            if (attacker.Index == victim.Index) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(attacker.Index);
            if (playerInfo?.Skill != skillName) return;

            var weapon = param2.Ability?.Value;
            if (weapon == null || !weapon.IsValid) return;
            if (nadeWeapons.Contains(weapon.DesignerName)) return;

            if (IsBehind(attacker, victim))
                param2.Damage *= Options.DamageMultiplier;
        }

        private static bool IsBehind(CCSPlayerController attacker, CCSPlayerController victim)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            var victimPawn = victim.PlayerPawn.Value;

            if (attackerPawn == null || !attackerPawn.IsValid || victimPawn == null || !victimPawn.IsValid) return false;
            if (victimPawn.AbsRotation == null || attackerPawn.AbsRotation == null) return false;

            var angles = GetAngleRange(victimPawn.AbsRotation.Y);
            return IsBetween(angles.Item1, angles.Item2, attackerPawn.AbsRotation.Y);
        }

        private static (float, float) GetAngleRange(float angle)
        {
            var toleranceDeg = Options.ToleranceDeg;
            float min = angle - toleranceDeg;
            float max = angle + toleranceDeg;

            if (min < -180) min += 360f;
            if (max > 180f) max -= 360f;

            return (min, max);
        }

        private static bool IsBetween(float a, float b, float target)
        {
            if (a <= b)
                return (target >= a && target <= b);
            return (target >= a || target <= b);
        }
    }
}