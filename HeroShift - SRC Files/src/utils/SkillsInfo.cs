using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using src.player;
using System.Collections.Concurrent;
using System.Reflection;
using static src.HeroShift;

namespace src.utils
{
    /*
     * SkillsInfo - THIS IS WHERE EVERY HERO'S TUNABLE VALUES COME FROM.
     *
     * If you want to change how much damage a hero does, how long an effect
     * lasts, a radius, a chance or a grenade count - this is the pipeline:
     *
     *   1. Each skill file declares a `SkillConfig` class whose constructor
     *      parameters ARE the tunables (e.g. KillerFlash: flashDuration,
     *      friendlyFire, grenadeLimit). Those parameter defaults are the
     *      shipped balance values.
     *
     *   2. SkillsInfoModel's constructor finds every SkillConfig class in the
     *      assembly by reflection and instantiates it with its defaults, so a
     *      newly added hero appears in the config automatically.
     *
     *   3. LoadSkillsInfo() reads configs/skillsInfo.json and uses
     *      JsonConvert.PopulateObject to overwrite those defaults per skill
     *      (matched on the "Name" field). Anything absent keeps its default.
     *
     *   4. At runtime a skill reads a value with
     *          SkillsInfo.GetValue<float>(skillName, "flashDuration")
     *      Lookup is case-insensitive and cached (_memberCache), so passing
     *      "FlashDuration" or "flashDuration" both work.
     *
     * So: to rebalance a hero, edit configs/skillsInfo.json on the server (no
     * recompile), or change the SkillConfig default to alter the shipped value.
     */
    public static class SkillsInfo
    {
        private static readonly string configsFolder = Path.Combine(Instance.ModuleDirectory, "configs");
        private static readonly string configPath = Path.Combine(configsFolder, "skillsInfo.json");
        private static readonly object fileLock = new();

        private static SkillsInfoModel config = LoadSkillsInfo();
        public static SkillsInfoModel LoadedConfig => config;

        private static SkillsInfoModel? _indexedConfig;
        private static ConcurrentDictionary<string, DefaultSkillInfo> _byName = new();
        private static readonly ConcurrentDictionary<(Type Type, string Key), MemberInfo?> _memberCache = new();

        public static SkillsInfoModel LoadSkillsInfo()
        {
            lock (fileLock)
            {
                var newConfig = new SkillsInfoModel();

                if (!File.Exists(configPath))
                {
                    Instance.Logger.LogInformation("Config file does not exist. Create a new skills info file...");
                    SaveConfig(newConfig);
                    return config = newConfig;
                }

                try
                {
                    string json;
                    using (var fs = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                        json = sr.ReadToEnd();

                    var root = JsonConvert.DeserializeObject<JArray>(json);
                    if (root != null)
                        foreach (var skillObj in root)
                        {
                            var name = skillObj["Name"]?.ToString();
                            if (string.IsNullOrEmpty(name)) continue;

                            var instance = newConfig.FirstOrDefault(x => x.Name == name.ToString());
                            if (instance != null) JsonConvert.PopulateObject(skillObj.ToString(), instance);
                        }
                }
                catch
                {
                    Instance.Logger.LogError("Error when loading the skills info file.");
                }

                return config = newConfig;
            }
        }

        public static void SaveConfig(SkillsInfoModel config)
        {
            lock (fileLock)
            {
                try
                {
                    Directory.CreateDirectory(configsFolder);
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);

                    string tempPath = $"{configPath}.temp";
                    File.WriteAllText(tempPath, json);

                    File.Copy(tempPath, configPath, overwrite: true);
                    File.Delete(tempPath);
                }
                catch
                {
                    Instance.Logger.LogError("Error when saving the skills info file.");
                }
            }
        }

    /*
         * Reads one tunable for one skill.
         *   skill - the Skills enum value (or anything whose ToString is the name)
         *   key   - the SkillConfig property name, case-insensitive
         * Returns default(T) if the skill or key is unknown, so a typo in the key
         * silently yields 0 / false / null rather than throwing. Worth knowing
         * when a value "does nothing" - check the spelling first.
         */
        public static T GetValue<T>(object skill, string key)
        {
            if (config == null) return default!;

            EnsureIndex();
            if (!_byName.TryGetValue(skill.ToString()!, out var skillConfig) || skillConfig == null)
                return default!;

            var member = _memberCache.GetOrAdd((skillConfig.GetType(), key), k =>
            {
                MemberInfo? m = k.Type.GetProperty(k.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                m ??= k.Type.GetField(k.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return m;
            });

            object? value = member switch
            {
                PropertyInfo p => p.GetValue(skillConfig),
                FieldInfo f => f.GetValue(skillConfig),
                _ => null
            };

            if (value == null) return default!;

            Type targetType = typeof(T);
            Type? underlyingType = Nullable.GetUnderlyingType(targetType);
            return (T)Convert.ChangeType(value, underlyingType ?? targetType);
        }

        private static void EnsureIndex()
        {
            if (ReferenceEquals(_indexedConfig, config)) return;

            var dict = new ConcurrentDictionary<string, DefaultSkillInfo>();
            foreach (var s in config)
                dict[s.Name] = s;

            _byName = dict;
            _indexedConfig = config;
            _memberCache.Clear();
        }

        public class SkillsInfoModel : ConcurrentBag<DefaultSkillInfo>
        {
            public string Name { get; set; } = "Default";
            public SkillsInfoModel()
            {
                foreach (var skill in
                    Assembly.GetExecutingAssembly().GetTypes()
                        .Where(t => typeof(DefaultSkillInfo).IsAssignableFrom(t) && t.Name == "SkillConfig")
                        .Select(t =>
                        {
                            var ctor = t.GetConstructors().FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));
                            if (ctor == null) return null;
                            var args = ctor.GetParameters().Select(p => Type.Missing).ToArray();
                            return ctor.Invoke(args) as DefaultSkillInfo;
                        })
                        .Where(instance => instance != null)
                        .Cast<DefaultSkillInfo>())
                    Add(skill);
            }
        }

    /*
         * The settings EVERY hero has. Each skill's own SkillConfig derives from
         * this and appends its hero-specific values on top.
         *   Active           - false removes the hero from the draw entirely
         *   Color            - HUD/chat colour (hex)
         *   OnlyTeam         - 0 both sides, otherwise a CsTeam value
         *   NeedsTeammates   - only drawn if the player has living teammates
         *   RequiredPermission - admin flag needed to receive it
         *   MaxPerServer     - simultaneous holders allowed (-1 = unlimited)
         *   Rarity           - draw weight bucket, see RarityManager
         */
        public class DefaultSkillInfo(Skills skill, bool active = true, string color = "#ffffff", CsTeam onlyTeam = CsTeam.None, bool disableOnFreezeTime = false, bool needsTeammates = false, string requiredPermission = "", float? hudDuration = null, float? descriptionHudDuration = null, int maxPerServer = -1, Rarity rarity = Rarity.Common)
        {
            public bool NeedsTeammates { get; set; } = needsTeammates;
            public bool DisableOnFreezeTime { get; set; } = disableOnFreezeTime;
            public int OnlyTeam { get; set; } = (int)onlyTeam;
            public string Color { get; set; } = color;
            public bool Active { get; set; } = active;
            public string Name { get; set; } = skill.ToString();
            public float? HudDuration { get; set; } = hudDuration;
            public float? DescriptionHudDuration { get; set; } = descriptionHudDuration;
            public string RequiredPermission { get; set; } = requiredPermission;
            public int MaxPerServer { get; set; } = maxPerServer;
            public string Rarity { get; set; } = rarity.ToString();
        }

    }
}