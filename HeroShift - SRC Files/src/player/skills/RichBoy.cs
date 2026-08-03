using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using src.utils;
using static src.HeroShift;

namespace src.player.skills
{
    /*
     * RichBoy - You start the round with a large random amount of money.
     *
     * LOGIC
     *   EnableSkill: rolls money between minMoney and maxMoney and sets your
     *     account.
     *
     * TUNABLE VALUES  (edit configs/skillsInfo.json, or the defaults in the
     * SkillConfig constructor at the bottom of this file)
     *   minMoney = 5000
     *                -> lowest starting money that can be rolled
     *   maxMoney = 15000
     *                -> highest starting money that can be rolled
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
    public class RichBoy : ISkill
    {
        private const Skills skillName = Skills.RichBoy;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, SkillsInfo.GetValue<string>(skillName, "color"));
        }

        private static int GetMaxMoney() => ConVar.Find("mp_maxmoney")?.GetPrimitiveValue<int>() ?? 16000;

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;
            int moneyBonus = Instance.Random.Next(SkillsInfo.GetValue<int>(skillName, "minMoney"), SkillsInfo.GetValue<int>(skillName, "maxMoney"));

            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null) return;

            moneyBonus = Math.Min(moneyBonus, GetMaxMoney() - moneyServices.Account);

            playerInfo.SkillChance = moneyBonus;
            AddMoney(player, moneyBonus);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = PlayerManager.GetPlayerByIndex(player!.Index);
            if (playerInfo == null) return;

            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null) return;

            int money = Math.Abs((int)playerInfo.SkillChance! - moneyServices.CashSpentThisRound);
            AddMoney(player, -money, 3000);
        }

        private static void AddMoney(CCSPlayerController player, int money, int minimum = 0)
        {
            if (player == null || !player.IsValid) return;
            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null) return;

            moneyServices.Account = Math.Clamp(moneyServices.Account + money, minimum, GetMaxMoney());
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#D4AF37", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common, int minMoney = 5000, int maxMoney = 15000) : SkillsInfo.DefaultSkillInfo(skill, active, color, onlyTeam, disableOnFreezeTime, needsTeammates, requiredPermission, hudDuration, descriptionHudDuration, maxPerServer, rarity)
        {
            public int MinMoney { get; set; } = minMoney;
            public int MaxMoney { get; set; } = maxMoney;
        }
    }
}