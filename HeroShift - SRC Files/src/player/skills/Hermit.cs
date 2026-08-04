using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using System.Collections.Concurrent;
using src.utils;

using src.SkillsCore;
using src.SkillsCore.BuiltIn;
namespace src.player.skills
{
    /*
     * Hermit - Killing an enemy grants you bonus health.
     *
     * LOGIC
     *   PlayerDeath: when you are the killer, adds healthToAdd and plays the
     *     effect.
     *
     * TUNABLE VALUES  (defaults live in the typed skill options record;
     * override them under this skill in configs/heroshift.json)
     *   healthToAdd    = 100
     *                      -> health gained per kill
     *   effectDuration = 1.0f
     *                      -> how long (seconds) the visual effect is shown
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
    public class Hermit : ISkill
    {
        private const Skills skillName = Skills.Hermit;
        private static HermitOptions Options => SkillConfigurationResolver.Get<HermitOptions>(BuiltInSkillIds.Hermit);
        private static readonly ConcurrentDictionary<string, int> ConcurrentDictionary = new(
        [
            new KeyValuePair<string, int>("weapon_glock", 3), new KeyValuePair<string, int>("weapon_usp_silencer", 2), new KeyValuePair<string, int>("weapon_hkp2000", 4), new KeyValuePair<string, int>("weapon_p250", 3),
            new KeyValuePair<string, int>("weapon_cz75", 2), new KeyValuePair<string, int>("weapon_deagle", 3), new KeyValuePair<string, int>("weapon_fiveseven", 2), new KeyValuePair<string, int>("weapon_elite", 2),
            new KeyValuePair<string, int>("weapon_tec9", 3), new KeyValuePair<string, int>("weapon_revolver", 2), new KeyValuePair<string, int>("weapon_mac10", 3), new KeyValuePair<string, int>("weapon_mp9", 2),
            new KeyValuePair<string, int>("weapon_mp7", 3), new KeyValuePair<string, int>("weapon_mp5", 3), new KeyValuePair<string, int>("weapon_mp5sd", 3), new KeyValuePair<string, int>("weapon_ump45", 3),
            new KeyValuePair<string, int>("weapon_p90", 2), new KeyValuePair<string, int>("weapon_bizon", 2), new KeyValuePair<string, int>("weapon_ak47", 3), new KeyValuePair<string, int>("weapon_m4a1", 4),
            new KeyValuePair<string, int>("weapon_m4a1_silencer", 3), new KeyValuePair<string, int>("weapon_galilar", 4), new KeyValuePair<string, int>("weapon_famas", 4), new KeyValuePair<string, int>("weapon_aug", 3),
            new KeyValuePair<string, int>("weapon_sg556", 3), new KeyValuePair<string, int>("weapon_ssg08", 2), new KeyValuePair<string, int>("weapon_awp", 2), new KeyValuePair<string, int>("weapon_scar20", 2),
            new KeyValuePair<string, int>("weapon_g3sg1", 2), new KeyValuePair<string, int>("weapon_nova", 32), new KeyValuePair<string, int>("weapon_xm1014", 32), new KeyValuePair<string, int>("weapon_sawedoff", 32),
            new KeyValuePair<string, int>("weapon_mag7", 3), new KeyValuePair<string, int>("weapon_m249", 2), new KeyValuePair<string, int>("weapon_negev", 2)
        ]);
        private static readonly ConcurrentDictionary<string, int> maxReserveAmmo = ConcurrentDictionary;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillRuntime.GetMetadata(skillName).Color);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
            if (!Instance.IsPlayerValid(attacker)) return;

            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker!.Index);
            if (attackerInfo?.Skill != skillName) return;

            var pawn = attacker!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null) return;

            var weapon = pawn.WeaponServices.ActiveWeapon.Value;
            if (weapon == null || !weapon.IsValid || weapon.VData == null) return;

            var maxReserveAmmoClip = maxReserveAmmo.TryGetValue(weapon.DesignerName, out var reserve) ? reserve : 100;
            weapon.Clip1 = weapon.VData.MaxClip1;
            weapon.ReserveAmmo.Fill(maxReserveAmmoClip);

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            SkillUtils.AddHealth(pawn, Options.HealthToAdd);

            float effectDuration = Options.EffectDuration;
            if (effectDuration > 0)
            {
                pawn.HealthShotBoostExpirationTime = Server.CurrentTime + effectDuration;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flHealthShotBoostExpirationTime");
            }
        }
    }
}