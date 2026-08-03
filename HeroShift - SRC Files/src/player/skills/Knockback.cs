using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Knockback - Your shots knock enemies backwards.
     *
     * LOGIC
     *   WeaponFire: applies knockbackUnits of push to the player you hit.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   knockbackUnits = 120f
     *                      -> push strength applied to the target per hit
     *   maxSpeed       = 1200f
     *                      -> cap on the resulting knockback speed (units/s)
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
    public class Knockback : ISkill
    {
        private const Skills skillName = Skills.Knockback;

        private static KnockbackOptions Options => SkillConfigurationResolver.Get<KnockbackOptions>(BuiltInSkillIds.Knockback);
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = PlayerManager.GetPlayerEvent(@event.Userid);
            if (player == null || !player.IsValid) return;

            var playerInfo = PlayerManager.GetPlayerByIndex(player.Index);
            if (playerInfo?.Skill != skillName) return;
            if (!SkillUtils.FiresBullets(@event.Weapon)) return;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid || pawn.Health <= 0) return;
            if ((pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0) return;

            float force = Options.KnockbackUnits;
            if (force <= 0) return;

            Vector push = SkillUtils.GetForwardVector(pawn.EyeAngles) * -force;

            pawn.AbsVelocity.X += push.X;
            pawn.AbsVelocity.Y += push.Y;
            pawn.AbsVelocity.Z += push.Z;

            float maxSpeed = Options.MaxSpeed;
            if (maxSpeed <= 0) return;

            float speed = pawn.AbsVelocity.Length();
            if (speed <= maxSpeed) return;

            float scale = maxSpeed / speed;
            pawn.AbsVelocity.X *= scale;
            pawn.AbsVelocity.Y *= scale;
            pawn.AbsVelocity.Z *= scale;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ff8c42", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, float knockbackUnits = 120f, float maxSpeed = 1200f) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public float KnockbackUnits { get; set; } = knockbackUnits;
            public float MaxSpeed { get; set; } = maxSpeed;
        }
    }
}
