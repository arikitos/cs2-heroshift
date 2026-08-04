// ClassifyBatches - development-only helper (not part of Program.cs's Main).
// Reads the baseline snapshot plus a text scan of each skill file to bucket
// every skill into one of REFACTOR.md section 23's migration batches (A-G).
// Run manually via `dotnet run -- classify <repoRoot>` while migrating skills;
// never shipped, not part of the normal baseline-extraction Main entry point.

using System.Text.RegularExpressions;

namespace BaselineExtractor;

public static class ClassifyBatches
{
    public static void Run(string repoRoot)
    {
        string skillsDir = Path.Combine(repoRoot, "HeroShift - SRC Files", "src", "player", "skills");
        var results = new List<(string Name, string Batch, string[] Signals)>();

        foreach (var file in Directory.GetFiles(skillsDir, "*.cs").OrderBy(f => f, StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string text = File.ReadAllText(file);
            string stripped = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);

            if (!Regex.IsMatch(stripped, $@"class\s+{Regex.Escape(name)}\s*:\s*ISkill")) continue;

            var signals = new List<string>();
            bool usesRayTrace = stripped.Contains("RayTrace") || stripped.Contains("CustomTraceResult");
            bool usesMenu = stripped.Contains("WasdMenu") || stripped.Contains("IWasdMenu") || stripped.Contains("MenuManager");
            bool usesEntitySpawn = stripped.Contains("OnEntitySpawned") || stripped.Contains("CreateEntity") || stripped.Contains("SpawnEntity") || stripped.Contains("AddTimer") && stripped.Contains("Entity");
            bool usesDamage = stripped.Contains("OnTakeDamage") || stripped.Contains("PlayerHurtPre") || stripped.Contains("TakeHealth");
            bool usesTick = stripped.Contains("public static void OnTick(");
            bool usesTypeSkillOrTarget = stripped.Contains("public static void TypeSkill(") || stripped.Contains("Findtarget") || stripped.Contains("FindTarget");
            bool hasSubstantialState = Regex.Matches(stripped, @"ConcurrentDictionary|ConcurrentBag|static readonly Dictionary").Count > 1;

            string batch;
            if (usesRayTrace) { batch = "E-RayTrace"; signals.Add("RayTrace"); }
            else if (usesMenu) { batch = "F-Menu"; signals.Add("WasdMenu"); }
            else if (usesEntitySpawn) { batch = "D-EntityGrenade"; signals.Add("EntitySpawn/Timer"); }
            else if (usesDamage) { batch = "C-DamagePipeline"; signals.Add("Damage"); }
            else if (usesTick) { batch = "B-TickMovement"; signals.Add("OnTick"); }
            else if (usesTypeSkillOrTarget || hasSubstantialState) { batch = "G-ComplexRemaining"; signals.Add(usesTypeSkillOrTarget ? "TypeSkill/Target" : "SubstantialState"); }
            else { batch = "A-Passive"; }

            results.Add((name, batch, signals.ToArray()));
        }

        foreach (var group in results.GroupBy(r => r.Batch).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"== {group.Key} ({group.Count()}) ==");
            foreach (var r in group.OrderBy(x => x.Name, StringComparer.Ordinal))
                Console.WriteLine($"  {r.Name}" + (r.Signals.Length > 0 ? $"  [{string.Join(", ", r.Signals)}]" : ""));
        }
    }
}
