using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using src.player;
using src.player.skills;

namespace src.utils
{
    /*
     * Localization - every user-visible string in the plugin.
     *
     * Backing file is languages/en.json, a flat key -> string JSON map, loaded into
     * _translations by Load(). Despite the name there is currently only ONE language:
     * LoadLanguage() hardcodes "en.json" and there is no per-player language
     * selection.
     *
     * How a hero uses it:
     *   player.GetTranslation("key", args...)   - a line of text for one player
     *   Localization.GetTranslation("key")      - same, without a player (console)
     *   player.GetSkillName(skill)              - the hero's display name
     *   player.GetSkillDescription(skill)       - the hero's description
     *   Localization.PrintTranslationToChatAll(...) - broadcast to all humans
     *
     * Adding a new string: put the key in languages/en.json and call
     * GetTranslation("your_key"). An UNKNOWN KEY IS NOT AN ERROR - GetTranslation
     * returns the key itself, so a missing translation shows up as raw "your_key"
     * text in chat rather than an exception. Arguments are substituted with
     * string.Format, so use {0}, {1} placeholders.
     *
     * Skill key convention (built once into the lookup maps at startup): the enum
     * name lower-cased is the name key, plus "_desc" for the description and
     * "_desc2" for the variant used when a percentage/chance must be shown. So the
     * Aimbot hero reads keys "aimbot", "aimbot_desc" and "aimbot_desc2". A hero that
     * has no _desc2 key falls back to _desc - that is what the desc2 == skillName
     * comparison in BuildSkillDescription detects, exploiting the fact that a missing
     * key returns the key itself.
     *
     * Two substitutions are applied to every value at load time:
     *   "CHATCOLORS.RED"  -> the actual red chat colour character, so the JSON can
     *                        stay plain ASCII.
     *   "css_useSkill"    -> "css_useSkill/<AlternativeSkillButton>" when that
     *                        config option is set, so help text names both binds.
     *
     * Chance-formatted names/descriptions: when a value contains a '%', the raw
     * fraction (e.g. 0.35) is replaced with its whole-percent form (35), so en.json
     * can be written with a plain "{0}%".
     *
     * Caching: GetSkillName/GetSkillDescription memoise per (skill, chance). The
     * Illiterate hero deliberately scrambles text per call, so results are NOT cached
     * for a player under that effect - hence the CheckIlliterateSkill guard on every
     * cache read and write. Load() clears all caches, which is why !reload picks up
     * edited strings.
     */
    public static class Localization
    {
        private static readonly string languagesFolderPath = Path.Combine(HeroShift.Instance.ModuleDirectory, "languages");
        private static readonly ConcurrentDictionary<string, string> _translations = [];

        private static readonly Dictionary<Skills, string> skillKeys = BuildSkillKeys("");
        private static readonly Dictionary<Skills, string> skillDescKeys = BuildSkillKeys("_desc");
        private static readonly Dictionary<Skills, string> skillDesc2Keys = BuildSkillKeys("_desc2");

        // Precomputes "<lowercased enum name><suffix>" for every hero once at startup, so
        // the per-call path never allocates a key string.
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

        // Keyed on (skill, chance) because the same hero renders differently depending on
        // the rolled chance value.
        private static readonly ConcurrentDictionary<(Skills Skill, double Chance), string> _skillNameCache = [];
        private static readonly ConcurrentDictionary<(Skills Skill, double Chance), string> _skillDescCache = [];

        // Stand-in cache key for "no chance given", since null cannot be a double key and 0
        // is a legitimate chance value.
        private const double NoChance = double.NegativeInfinity;

        // Re-reads the language file and drops all caches. Called on plugin load and on
        // !reload, which is what makes edited strings take effect without a restart.
        public static void Load()
        {
            _translations.Clear();
            _skillNameCache.Clear();
            _skillDescCache.Clear();
            LoadLanguage();
        }

        // Reads languages/en.json and applies the load-time text substitutions.
        // A missing or unparseable file is not fatal: _translations stays empty and every
        // lookup then returns its own key, so the plugin still runs with raw key names.
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
                // Chat colours are control characters that cannot be typed into JSON, so the
                // literal token "CHATCOLORS.RED" is swapped for the real character here.
                var val = translations[tkey].Replace("CHATCOLORS.RED", redColor);
                // When an alternative bind is configured, help text mentioning the skill
                // command advertises both forms.
                if (!string.IsNullOrEmpty(altButton))
                    val = val.Replace("css_useSkill", $"css_useSkill/{altButton}");
                _translations[tkey] = val;
            }
        }

        // A hero's display name. Pass chance to include its rolled percentage in the name.
        // Results are cached unless the viewing player is under the Illiterate effect,
        // which must be re-scrambled on every call.
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
            // No chance supplied, but the string may still contain a {0} placeholder for one.
            // Rather than formatting it with a value, the whole placeholder-bearing WORD is
            // dropped, so "Headshot {0}% chance" becomes "Headshot chance" instead of leaving
            // a stray "{0}" or an empty gap. Single-word strings just lose the placeholder.
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
            // Chances are stored as fractions (0.35). If the rendered text contains a '%',
            // the fraction is rewritten as whole percent (35) so en.json can just say "{0}%".
            if (skillNameText.Contains('%')) skillNameText = skillNameText.Replace(value.ToString(), Math.Round(value * 100, 0).ToString());
            return skillNameText;
        }

        // A hero's description. Same caching and chance handling as GetSkillName, but reads
        // the "_desc"/"_desc2" keys.
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

            // "_desc2" is the chance-aware description. Because a missing key returns the key
            // itself, desc2 == the key name means the hero has no _desc2 entry, in which case
            // the plain "_desc" text is used instead.
            var skillName = SkillDesc2Key(skill);
            var value = Math.Round((double)(chance ?? 1), 2);
            var desc2 = GetTranslation(skillName, player, value);

            var skilLDescription = desc2 == skillName
                ? player.GetTranslation(SkillDescKey(skill))
                : desc2.Contains('%') ? desc2.Replace(value.ToString(), Math.Round(value * 100, 0).ToString()) : desc2;
            return skilLDescription;
        }

        // Broadcasts to every human (bots are skipped). `message` is a format string whose
        // {0}, {1}, ... are filled with the TRANSLATIONS of the keys in `key` - so it composes
        // several translated fragments into one line. `args` is parallel to `key`: args[i]
        // holds the format arguments for key[i]. Passing key == null sends `message` verbatim.
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

        // Extension-method form, so heroes can write player.GetTranslation("key", arg).
        public static string GetTranslation(this CCSPlayerController player, string key, params object[] args)
        {
            return GetTranslation(key, player, args);
        }

        // The single lookup all the helpers above funnel into.
        // Pass player when the text is for one person (needed for the Illiterate effect);
        // omit it for console output. An unknown key is returned AS the key - that is the
        // intended fallback and is why a typo shows up as visible raw text.
        public static string GetTranslation(string key, CCSPlayerController? player = null, params object[] args)
        {
            if (_translations.TryGetValue(key, out var translation))
            {
                // Sentinel, not a real argument: the caller passes the literal string
                // "welcome" (see the welcome_message lookup in PlayerEvents) to get the text
                // back UNFORMATTED. That text contains its own {PLAYER} placeholder, which
                // the caller substitutes itself and which string.Format would throw on.
                if (args.Length != 0 && args[0].ToString() == "welcome")
                    return translation;

                string output = args.Length == 0 ? translation : string.Format(translation, args);

                // Applied last, so the scramble covers the fully formatted text.
                if (Illiterate.CheckIlliterateSkill(player))
                    return Illiterate.GetRandomText(output)!;
                return output;
            }

            return key;
        }
    }
}
