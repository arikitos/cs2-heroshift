using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// BaselineExtractor - development-only tool (REFACTOR.md section 5).
// Parses the current reflection-driven skill source files as plain text (no
// Roslyn / new package dependency) and produces a deterministic JSON snapshot
// of every skill's identity, base metadata defaults, skill-specific
// SkillConfig defaults, and which ISkill hooks it implements. This snapshot is
// the equivalence baseline the new typed architecture must match (REFACTOR.md
// section 30). Never shipped in the release package.

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: BaselineExtractor <repoRoot> [outputPath]");
    Console.Error.WriteLine("       BaselineExtractor classify <repoRoot>   (buckets skills into REFACTOR.md migration batches)");
    return 1;
}

if (string.Equals(args[0], "classify", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: BaselineExtractor classify <repoRoot>");
        return 1;
    }

    BaselineExtractor.ClassifyBatches.Run(Path.GetFullPath(args[1]));
    return 0;
}

string repoRoot = Path.GetFullPath(args[0]);
string srcFilesDir = Path.Combine(repoRoot, "HeroShift - SRC Files");
string skillsDir = Path.Combine(srcFilesDir, "src", "player", "skills");
string outputPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.Combine(srcFilesDir, "tools", "refactor-baseline", "snapshot", "baseline.json");

string[] hookNames =
[
    "LoadSkill", "EnableSkill", "DisableSkill", "UseSkill", "TypeSkill",
    "OnTakeDamage", "OnTakeDamagePost", "OnEntitySpawned", "OnTick", "CheckTransmit",
    "NewRound", "RoundEnd", "PlayerMakeSound", "PlayerBlind", "PlayerHurt", "PlayerHurtPre",
    "PlayerDeath", "PlayerJump", "SwitchTeam", "BotTakeover",
    "WeaponFire", "WeaponEquip", "WeaponPickup", "WeaponReload", "WeaponDrop",
    "GrenadeThrown", "BulletImpact",
    "BombBeginplant", "BombAbortplant", "BombPlanted", "BombBegindefuse",
    "DecoyStarted", "DecoyDetonate", "SmokegrenadeDetonate", "SmokegrenadeExpired",
    "OnTriggerEnter", "OnTriggerExit", "OnWeaponCanAcquire",
];

// Shared DefaultSkillInfo parameter names, in the fixed order every SkillConfig
// constructor begins with (see src/utils/SkillsInfo.cs DefaultSkillInfo and any skill file).
string[] baseParamNames =
[
    "skill", "active", "color", "onlyTeam", "disableOnFreezeTime", "needsTeammates",
    "requiredPermission", "hudDuration", "descriptionHudDuration", "maxPerServer", "rarity",
];

var skillFiles = Directory.GetFiles(skillsDir, "*.cs")
    .OrderBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.Ordinal)
    .ToList();

var skills = new List<Dictionary<string, object?>>();
var warnings = new List<string>();

foreach (var file in skillFiles)
{
    string name = Path.GetFileNameWithoutExtension(file);
    string text = File.ReadAllText(file);

    // Fully commented-out files (e.g. Mute.cs) are dead code, not real skills -
    // skip them, but flag for human review since REFACTOR.md forbids silent drops.
    string stripped = StripBlockComments(text);
    if (!Regex.IsMatch(stripped, $@"class\s+{Regex.Escape(name)}\s*:\s*ISkill"))
    {
        warnings.Add($"{name}: no 'class {name} : ISkill' found (likely dead/commented-out file) - excluded from baseline");
        continue;
    }

    var hooks = hookNames.Where(h => Regex.IsMatch(stripped, $@"public\s+static\s+(void|bool)\s+{h}\s*\(")).ToList();

    var (baseDefaults, specificDefaults, ctorWarnings) = ExtractSkillConfig(stripped, name, baseParamNames);
    warnings.AddRange(ctorWarnings);

    skills.Add(new Dictionary<string, object?>
    {
        ["name"] = name,
        ["hooks"] = hooks,
        ["metadata"] = baseDefaults,
        ["options"] = specificDefaults,
    });
}

var localization = ExtractLocalization(srcFilesDir);
var package = ExtractPackageInventory(repoRoot);

var baseline = new Dictionary<string, object?>
{
    ["skillCount"] = skills.Count,
    ["skills"] = skills.OrderBy(s => (string)s["name"]!, StringComparer.Ordinal).ToList(),
    ["localization"] = localization,
    ["package"] = package,
    ["warnings"] = warnings.OrderBy(w => w, StringComparer.Ordinal).ToList(),
};

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(outputPath, JsonSerializer.Serialize(baseline, jsonOptions), new UTF8Encoding(false));

Console.WriteLine($"Extracted {skills.Count} skills ({warnings.Count} warnings) -> {outputPath}");
foreach (var w in warnings)
    Console.WriteLine($"  WARN: {w}");

return 0;

static string StripBlockComments(string text) => Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);

static (Dictionary<string, object?> baseDefaults, Dictionary<string, object?> specificDefaults, List<string> warnings) ExtractSkillConfig(string text, string skillName, string[] baseParamNames)
{
    var warnings = new List<string>();

    // Matches either:
    //   public class SkillConfig(<params>) : SkillsInfo.DefaultSkillInfo(...)
    //   public SkillConfig(<params>) : base(...)   (inside "public class SkillConfig : SkillsInfo.DefaultSkillInfo { ... }")
    var ctorMatch = Regex.Match(text, @"(?:public\s+class\s+SkillConfig\s*\(|public\s+SkillConfig\s*\()(?<params>.*?)\)\s*:\s*(?:SkillsInfo\.DefaultSkillInfo\(.*?\)|base\s*\(.*?\))", RegexOptions.Singleline);
    if (!ctorMatch.Success)
    {
        warnings.Add($"{skillName}: could not locate SkillConfig constructor parameter list");
        return (new(), new(), warnings);
    }

    var paramList = SplitTopLevel(ctorMatch.Groups["params"].Value);

    var baseDefaults = new Dictionary<string, object?>();
    var specificDefaults = new Dictionary<string, object?>();

    foreach (var rawParam in paramList)
    {
        var (paramName, paramValue) = ParseParam(rawParam);
        if (paramName == null) continue;

        if (baseParamNames.Contains(paramName, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.Equals(paramName, "skill", StringComparison.OrdinalIgnoreCase))
                baseDefaults[paramName] = paramValue;
        }
        else
        {
            specificDefaults[paramName] = paramValue;
        }
    }

    return (baseDefaults, specificDefaults, warnings);
}

// Splits a parameter list on top-level commas only (ignores commas inside <>, (), "").
static List<string> SplitTopLevel(string paramList)
{
    var result = new List<string>();
    int depth = 0;
    bool inString = false;
    var current = new StringBuilder();

    foreach (char c in paramList)
    {
        if (c == '"') inString = !inString;
        if (!inString)
        {
            if (c is '(' or '<' or '[') depth++;
            else if (c is ')' or '>' or ']') depth--;
        }

        if (c == ',' && depth == 0 && !inString)
        {
            result.Add(current.ToString().Trim());
            current.Clear();
        }
        else
        {
            current.Append(c);
        }
    }
    if (current.Length > 0) result.Add(current.ToString().Trim());
    return result.Where(p => p.Length > 0).ToList();
}

// A parameter looks like: "float jumpVelocity = 150f" or "Skills skill = skillName".
// Returns (name, defaultValueLiteral) - the literal is kept as source text (e.g. "150f",
// "Rarity.Common", "CsTeam.None") since this baseline compares semantics, not CLR types.
static (string? name, string? value) ParseParam(string param)
{
    var eqIdx = param.IndexOf('=');
    if (eqIdx < 0) return (null, null);

    string beforeEq = param[..eqIdx].Trim();
    string afterEq = param[(eqIdx + 1)..].Trim();

    var nameMatch = Regex.Match(beforeEq, @"(\w+)$");
    if (!nameMatch.Success) return (null, null);

    return (nameMatch.Groups[1].Value, afterEq);
}

// Reads src/lang/en.json (the current single built-in language file) and records every
// key plus its placeholder set ({0}, {1}, ...), so the new embedded-resource localization
// system can be checked for exact key/placeholder equivalence (REFACTOR.md sections 16-17).
static Dictionary<string, object?> ExtractLocalization(string srcFilesDir)
{
    string langPath = Path.Combine(srcFilesDir, "src", "lang", "en.json");
    var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(langPath))
        ?? new Dictionary<string, string>();

    var placeholderRegex = new Regex(@"\{(\d+)\}");
    var keys = raw.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new Dictionary<string, object?>
    {
        ["key"] = kv.Key,
        ["placeholders"] = placeholderRegex.Matches(kv.Value).Select(m => int.Parse(m.Groups[1].Value)).Distinct().OrderBy(i => i).ToList(),
    }).ToList();

    return new Dictionary<string, object?>
    {
        ["sourceFile"] = "src/lang/en.json",
        ["keyCount"] = raw.Count,
        ["keys"] = keys,
    };
}

// Records the file set that ships in the current server/release payload
// ("HeroShift - Server Files/"), which is the manually-synchronized layout the
// generated packaging script (REFACTOR.md section 25-26) must reproduce exactly.
static Dictionary<string, object?> ExtractPackageInventory(string repoRoot)
{
    string serverFilesDir = Path.Combine(repoRoot, "HeroShift - Server Files");
    var files = Directory.Exists(serverFilesDir)
        ? Directory.GetFiles(serverFilesDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(serverFilesDir, f).Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList()
        : new List<string>();

    return new Dictionary<string, object?>
    {
        ["root"] = "HeroShift - Server Files",
        ["fileCount"] = files.Count,
        ["files"] = files,
    };
}
