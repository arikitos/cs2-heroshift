# refactor-baseline

Development-only tool used by the HeroShift architecture refactor (see `REFACTOR.md` at the
repository root, section 5 and section 30). It is **not** part of `HeroShift.sln` and is
never shipped in the release package (`HeroShift.csproj` excludes `tools\**` from compilation).

## What it does

Parses the current reflection-driven skill sources (`src/player/skills/*.cs`) as plain text
and produces a deterministic JSON snapshot of:

- Every skill's ID, implemented `ISkill` hooks, base `DefaultSkillInfo` metadata defaults, and
  skill-specific `SkillConfig` defaults.
- The full `src/lang/en.json` localization key list with placeholder (`{0}`, `{1}`, ...) sets.
- The current `HeroShift - Server Files/` release payload file inventory.

This snapshot (`snapshot/baseline.json`) is the equivalence baseline the new typed
architecture must match. `snapshot/global-config-baseline.json` is a hand-transcribed
(not regex-extracted, since `Config.SettingsModel`'s constructor is not a regular
primary-constructor parameter list) snapshot of the global `configs/config.json` defaults,
cross-checked against `src/utils/Config.cs`.

## Regenerating

```bash
dotnet build "HeroShift - SRC Files/tools/refactor-baseline/BaselineExtractor.csproj" -c Debug
dotnet "HeroShift - SRC Files/tools/refactor-baseline/bin/Debug/net10.0/BaselineExtractor.dll" "<repo root>"
```

Output defaults to `snapshot/baseline.json`; pass a second argument to redirect it.

## Known limitations

- Parses `SkillConfig` constructors via regex, not a real C# parser. It handles both the
  primary-constructor form (`public class SkillConfig(...) : SkillsInfo.DefaultSkillInfo(...)`)
  and the classic form (`public SkillConfig(...) : base(...)`) used across all 142 active
  skill files as of this refactor.
- `Mute.cs` is entirely commented out (dead code, not registered in the `Skills` enum) and is
  intentionally excluded — flagged in `baseline.json`'s `warnings` array rather than silently
  dropped.
