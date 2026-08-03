using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using src.player;
using src.player.skills;

namespace src.utils
{
    public static class Localization
    {
        private static readonly string languagesFolderPath = Path.Combine(HeroShift.Instance.ModuleDirectory, "languages");
        private static readonly ConcurrentDictionary<string, string> _translations = [];

        private static readonly Dictionary<Skills, string> skillKeys = BuildSkillKeys("");
        private static readonly Dictionary<Skills, string> skillDescKeys = BuildSkillKeys("_desc");
        private static readonly Dictionary<Skills, string> skillDesc2Keys = BuildSkillKeys("_desc2");

        private static Dictionary<Skills, string> BuildSkillKeys(string suffix)
        {
            var map = new Dictionary<Skills, string>();
            foreach (Skills skill in Enum.GetValues<Skills>())
                map[skill] = skill.ToString().ToLowerInvariant() + suffix;
            return map;
        }

        private static string SkillKey(Skills skill) => skillKeys.TryGetValue(skill, out var k) ? k : skill.ToString().ToLowerInvariant();
        private static string SkillDescKey(Skills skill) => skillDescKeys.TryGetValue(skill, out var k) ? k : skill.ToString().ToLowerInvariant() + "_desc";
        private static string SkillDesc2Key(Skills skill) => skillDesc2Keys.TryGetValue(skill, out var k) ? k : skill.ToString().ToLowerInvariant() + "_desc2";

        private static readonly ConcurrentDictionary<(Skills Skill, double Chance), string> _skillNameCache = [];
        private static readonly ConcurrentDictionary<(Skills Skill, double Chance), string> _skillDescCache = [];

        private const double NoChance = double.NegativeInfinity;

        public static void Load()
        {
            _translations.Clear();
            _skillNameCache.Clear();
            _skillDescCache.Clear();
            LoadLanguage();
        }

        private static void LoadLanguage()
        {
            string langPath = Path.Combine(languagesFolderPath, "en.json");
            if (!File.Exists(langPath))
                return;

            var jsonText = File.ReadAllText(langPath);
            var translations = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(jsonText);
            if (translations == null)
                return;

            string redColor = ChatColors.Red.ToString();
            string? altButton = Config.LoadedConfig.AlternativeSkillButton;
            foreach (var tkey in translations.Keys)
            {
                var val = translations[tkey].Replace("CHATCOLORS.RED", redColor);
                if (!string.IsNullOrEmpty(altButton))
                    val = val.Replace("css_useSkill", $"css_useSkill/{altButton}");
                _translations[tkey] = val;
            }
        }

        public static string GetSkillName(this CCSPlayerController player, Skills skill, float? chance = null)
        {
            bool cacheable = !Illiterate.CheckIlliterateSkill(player);
            var cacheKey = (skill, chance == null ? NoChance : Math.Round((double)chance, 2));

            if (cacheable && _skillNameCache.TryGetValue(cacheKey, out var cached))
                return cached;

            string result = BuildSkillName(player, skill, chance);

            if (cacheable)
                _skillNameCache[cacheKey] = result;

            return result;
        }

        private static string BuildSkillName(CCSPlayerController player, Skills skill, float? chance)
        {
            if (chance == null)
            {
                var translation = GetTranslation(SkillKey(skill));
                if (!translation.Contains("{0}"))
                    return translation;

                if (!translation.Contains(' '))
                    return translation.Replace("{0}", "").Trim();

                var parts = translation.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var filtered = parts.Where(p => !p.Contains("{0}"));
                return string.Join(' ', filtered);
            }

            var value = Math.Round((double)(chance ?? 1), 2);
            var skillNameText = GetTranslation(SkillKey(skill), player, value);
            if (skillNameText.Contains('%')) skillNameText = skillNameText.Replace(value.ToString(), Math.Round(value * 100, 0).ToString());
            return skillNameText;
        }

        public static string GetSkillDescription(this CCSPlayerController player, Skills skill, float? chance = null)
        {
            bool cacheable = !Illiterate.CheckIlliterateSkill(player);
            var cacheKey = (skill, chance == null ? NoChance : Math.Round((double)chance, 2));

            if (cacheable && _skillDescCache.TryGetValue(cacheKey, out var cached))
                return cached;

            string result = BuildSkillDescription(player, skill, chance);

            if (cacheable)
                _skillDescCache[cacheKey] = result;

            return result;
        }

        private static string BuildSkillDescription(CCSPlayerController player, Skills skill, float? chance)
        {
            if (chance == null)
            {
                var translation = GetTranslation(SkillDescKey(skill));
                if (!translation.Contains("{0}"))
                    return translation;

                if (!translation.Contains(' '))
                    return translation.Replace("{0}", "").Trim();

                var parts = translation.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var filtered = parts.Where(p => !p.Contains("{0}"));
                return string.Join(' ', filtered);
            }

            var skillName = SkillDesc2Key(skill);
            var value = Math.Round((double)(chance ?? 1), 2);
            var desc2 = GetTranslation(skillName, player, value);

            var skilLDescription = desc2 == skillName
                ? player.GetTranslation(SkillDescKey(skill))
                : desc2.Contains('%') ? desc2.Replace(value.ToString(), Math.Round(value * 100, 0).ToString()) : desc2;
            return skilLDescription;
        }

        public static void PrintTranslationToChatAll(string message, string[]? key, params object[][]? args)
        {
            foreach (var player in Utilities.GetPlayers().Where(p => !p.IsBot))
            {
                if (key == null)
                {
                    player.PrintToChat(message);
                    continue;
                }

                List<string> translations = [];
                for (int i = 0; i < key.Length; i++)
                {
                    object[] currentArgs = args != null && i < args.Length ? args[i] : [];
                    translations.Add(GetTranslation(key[i], null, currentArgs));
                }
                player.PrintToChat(string.Format(message, [.. translations]));
            }
        }

        public static string GetTranslation(this CCSPlayerController player, string key, params object[] args)
        {
            return GetTranslation(key, player, args);
        }

        public static string GetTranslation(string key, CCSPlayerController? player = null, params object[] args)
        {
            if (_translations.TryGetValue(key, out var translation))
            {
                if (args.Length != 0 && args[0].ToString() == "welcome")
                    return translation;

                string output = args.Length == 0 ? translation : string.Format(translation, args);

                if (Illiterate.CheckIlliterateSkill(player))
                    return Illiterate.GetRandomText(output)!;
                return output;
            }

            return key;
        }
    }
}
