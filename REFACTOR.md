# HeroShift Full Refactor Plan

## Purpose

Refactor the HeroShift CS2 plugin end to end into a typed, scalable, maintainable architecture while preserving the plugin's observable runtime behavior.

The refactor must replace the current duplicated configuration and reflection-driven skill system with:

```text
Code:
  Canonical defaults
  Skill IDs
  Typed options
  Skill metadata
  Built-in English resources

heroshift.json:
  Server-specific overrides only

Optional language files:
  Additional translations only

Generated release artifact:
  DLLs, gamedata and default resource files
```

This document is intended to be executed by Claude Code inside VS Code against:

```text
Repository: arikitos/cs2-heroshift
Working branch: development
```

---

# 1. Mandatory Repository Rules

## Commits

Commits are explicitly authorized for this refactor.

Use English Conventional Commit messages with imperative subjects and no trailing period.

Recommended commit sequence is defined later in this document.

Before every commit:

1. Review `git diff --stat`.
2. Review `git diff`.
3. Confirm no unrelated files are included.
4. Run the relevant checks for that phase.
5. Update `CHANGELOG.md` under `Unreleased` when repository files change.

Do not push unless explicitly requested.

---

# 2. Non-Negotiable Compatibility Contract

The internal architecture may change substantially. Observable server behavior must remain compatible unless a change is explicitly documented and approved.

Preserve:

- All existing skills.
- All current gameplay formulas.
- All timers, probabilities, damage values, movement values, radii, durations and limits.
- All command aliases and default permissions.
- All voting behavior.
- All event ordering that can affect gameplay.
- All HUD behavior.
- WASDMenu behavior.
- RayTrace behavior.
- Bot behavior.
- Bot takeover behavior.
- Hot reload behavior.
- Map-change and round-reset behavior.
- Current DLL names.
- Current runtime dependency names.
- Current gamedata behavior.
- Current CounterStrikeSharp integration.
- Current server installation layout as far as required by runtime dependencies.

Do not use the refactor as an opportunity to rebalance, rename, optimize gameplay semantics, or fix unrelated bugs.

When a behavior appears questionable, preserve it and document it under `Risks / Notes`.

---

# 3. Existing External Contracts

## .NET and packages

Keep the repository's existing versions unchanged.

At the time this plan was prepared, the main plugin project uses:

```xml
<TargetFramework>net10.0</TargetFramework>
<PackageReference Include="CounterStrikeSharp.API" Version="1.0.371" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

The WASDMenu project currently references its existing CounterStrikeSharp version. Do not upgrade or reconcile package versions during this refactor.

## WASDMenu

Preserve:

- `WASDMenuAPI.dll`
- Existing WASDMenu project source.
- Existing activation and disconnect handlers.
- Existing `OnTick` behavior.
- W/S navigation.
- Use/E selection.
- Hot-reload player population.
- Center HTML rendering.
- Pause behavior.
- Menu cleanup semantics.

HeroShift may wrap WASDMenu behind an internal adapter, but must not replace or redesign the external WASDMenu implementation.

## RayTrace

Preserve:

```text
RayTraceApi.dll
RayTraceImpl.dll
RayTrace.vdf
Capability: raytrace:craytraceinterface
```

Preserve:

- Existing trace masks.
- Existing contents masks.
- Existing ignored entity behavior.
- Existing hull dimensions.
- Existing eye-position calculation.
- Existing native exception handling.
- Existing missing-module behavior.
- Existing hit helper semantics.
- Existing debug beam behavior.
- Existing gamedata and runtime paths.

RayTrace may be wrapped behind an internal interface, but the external API calls and behavior must remain equivalent.

---

# 4. Refactor Objectives

The completed implementation must achieve all of the following:

1. The code is the canonical source for:
   - Skill IDs.
   - Skill metadata.
   - Skill defaults.
   - Typed skill-specific options.
   - Built-in English translations.

2. `heroshift.json` contains only server-specific overrides.

3. External language files are optional overrides or additional languages.

4. `config.json` and `skillsInfo.json` are removed from the new architecture.

5. No legacy compatibility layer is required.

6. Skill configuration access is compile-time typed.

7. Skill dispatch no longer uses method names as strings.

8. Runtime Skill lookup no longer depends on:
   - Enum member name.
   - Class name.
   - File name.
   - Reflection-discovered nested `SkillConfig`.
   - Translation key naming coincidence.

9. The release package is generated from source and build output.

10. Windows and Linux builds are supported.

11. The plugin continues to behave the same inside a Linux CS2 dedicated-server Docker container.

---

# 5. Required Baseline Capture

Do not begin destructive architectural replacement before capturing the current system.

Create read-only baseline tooling or generated snapshots under a clearly named development-only location such as:

```text
tools/refactor-baseline/
artifacts/refactor-baseline/
```

Do not ship development-only tooling in the release package.

Capture:

## Skill inventory

For every skill:

- Current enum name.
- Current class name.
- Current `SkillConfig` type.
- Base metadata defaults.
- Skill-specific defaults.
- Implemented hook methods.
- Translation keys.
- Whether it uses:
  - RayTrace.
  - WASDMenu.
  - Tick.
  - Damage hooks.
  - Entity hooks.
  - Timers.
  - Per-player state.
  - Static global state.

## Global configuration

Capture all defaults from the current configuration model:

- General settings.
- HUD settings.
- Chat settings.
- Commands.
- Voting commands.
- Permissions.
- Debug and performance settings.

## Localization

Capture:

- All English keys.
- All placeholders for each key.
- Skill name keys.
- Skill description keys.
- Dynamic description keys.
- Command and voting messages.
- HUD messages.
- Error messages.

## Runtime and package inventory

Capture expected release contents:

- Plugin DLLs.
- WASDMenu DLL.
- Newtonsoft DLL.
- RayTrace assets.
- Gamedata.
- Configuration files.
- Language files.
- Any static resources.

## Baseline output format

Use deterministic JSON sorted by key.

Example:

```json
{
  "skills": {
    "Dash": {
      "hooks": ["LoadSkill", "EnableSkill", "DisableSkill", "OnTick", "NewRound"],
      "metadata": {},
      "options": {}
    }
  }
}
```

Commit the baseline tool and snapshot before replacing the old architecture.

---

# 6. Target Architecture

Do not split the repository into many projects during this refactor. Keep the existing solution and primary project unless there is a verified technical requirement.

Recommended source structure:

```text
HeroShift - SRC Files/src/
├── Plugin/
│   ├── HeroShiftPlugin.cs
│   ├── PluginRuntime.cs
│   ├── PluginBootstrap.cs
│   └── PluginServices.cs
│
├── Configuration/
│   ├── HeroShiftConfiguration.cs
│   ├── HeroShiftDefaults.cs
│   ├── ConfigurationLoader.cs
│   ├── ConfigurationSnapshot.cs
│   ├── ConfigurationValidator.cs
│   ├── JsonMerge.cs
│   └── Models/
│
├── Localization/
│   ├── ILocalizationService.cs
│   ├── LocalizationService.cs
│   ├── TranslationCatalog.cs
│   ├── TranslationValidator.cs
│   └── Resources/
│       └── en.json
│
├── Skills/
│   ├── Abstractions/
│   │   ├── SkillId.cs
│   │   ├── SkillDefinition.cs
│   │   ├── SkillMetadata.cs
│   │   ├── SkillRegistration.cs
│   │   ├── SkillHookSet.cs
│   │   └── SkillOptionSet.cs
│   ├── BuiltInSkillCatalog.cs
│   ├── SkillRegistry.cs
│   ├── SkillDispatcher.cs
│   └── SkillConfigurationResolver.cs
│
├── Players/
│   ├── PlayerRuntimeState.cs
│   └── PlayerStateStore.cs
│
├── Infrastructure/
│   ├── WasdMenu/
│   │   ├── IWasdMenuService.cs
│   │   └── WasdMenuService.cs
│   └── RayTrace/
│       ├── IRayTraceService.cs
│       └── RayTraceService.cs
│
├── command/
├── player/
└── utils/
```

Avoid mass-moving all skill files. Keep skill implementation files in their current directory initially to reduce rename noise.

---

# 7. Skill Identity

Replace the `Skills` enum as the canonical identity system.

Use a strongly typed value:

```csharp
public readonly record struct SkillId
{
    public string Value { get; }

    private SkillId(string value)
    {
        Value = value;
    }

    public static SkillId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Skill ID cannot be empty.", nameof(value));

        return new SkillId(value.Trim().ToLowerInvariant());
    }

    public override string ToString() => Value;
}
```

Requirements:

- IDs are lowercase invariant.
- IDs are stable.
- IDs do not depend on class names.
- IDs are unique.
- Parsing from command input is case-insensitive.
- Existing user-visible skill-name command behavior remains compatible.
- Built-in aliases may be required where current enum/class naming differs from natural lowercase conversion.

Example:

```csharp
public static class BuiltInSkillIds
{
    public static readonly SkillId Dash = SkillId.Create("dash");
    public static readonly SkillId AimLock = SkillId.Create("aimlock");
}
```

Do not create raw `new SkillId(...)` calls throughout the codebase. Centralize built-in IDs.

---

# 8. Skill Definition Model

Each skill must have one canonical definition.

Example:

```csharp
public sealed record SkillMetadata(
    bool Enabled,
    string Color,
    CsTeam OnlyTeam,
    bool DisableOnFreezeTime,
    bool NeedsTeammates,
    string RequiredPermission,
    float? HudDuration,
    float? DescriptionHudDuration,
    int MaxPerServer,
    SkillRarity Rarity);

public sealed record SkillDefinition<TOptions>(
    SkillId Id,
    SkillMetadata Metadata,
    TOptions DefaultOptions,
    SkillHookSet Hooks)
    where TOptions : class;
```

The definition must include:

- ID.
- Metadata.
- Typed defaults.
- Registered hooks.
- Localization keys or deterministic localization-key derivation.
- Optional validation delegate for skill-specific options.

The skill implementation must not duplicate metadata in multiple locations.

---

# 9. Typed Skill Options

Every skill-specific `SkillConfig` must become a dedicated typed options class or record.

Example:

```csharp
public sealed record DashOptions
{
    public float JumpVelocity { get; init; } = 150f;
    public float PushVelocity { get; init; } = 600f;
    public bool AnyDirection { get; init; } = true;
    public float CooldownSeconds { get; init; } = 2f;
}
```

The skill must receive or resolve its typed options once per effective configuration snapshot.

Remove all usages of:

```csharp
SkillsInfo.GetValue<T>(skill, "property")
```

Replace with:

```csharp
_options.CooldownSeconds
```

or an equivalent typed accessor:

```csharp
SkillOptions.Get<DashOptions>(BuiltInSkillIds.Dash)
```

Requirements:

- No reflection-based property access.
- No string property names.
- Unknown properties in JSON must be reported.
- Invalid types must be reported.
- Missing properties use code defaults.
- Invalid values must not silently become `default(T)`.

---

# 10. Skill Registration and Dispatch

Remove reflection-based method dispatch.

Remove the architecture where calls are made through:

```csharp
SkillAction(skillName, "EnableSkill", args)
```

Implement typed hook registration.

Possible model:

```csharp
public sealed class SkillHookSet
{
    public Action? OnLoad { get; init; }
    public Action<CCSPlayerController>? OnEnable { get; init; }
    public Action<CCSPlayerController>? OnDisable { get; init; }
    public Action<CCSPlayerController>? OnUse { get; init; }
    public Action<CCSPlayerController, string[]>? OnType { get; init; }
    public Action? OnTick { get; init; }

    // Continue for every existing hook.
}
```

Or capability interfaces where appropriate.

The dispatcher must preserve:

- Existing invocation order.
- Existing return aggregation for boolean hooks.
- Existing short-circuit behavior.
- Existing exception behavior unless current behavior can crash the server; any safety adjustment must be explicitly documented.
- Active-skill tracking.
- Curse-skill limits.
- Round and map tracking.
- Performance logging.

Create pre-indexed collections:

```text
All skills
Tick skills
Damage-pre skills
Damage-post skills
Entity-spawn skills
Transmit skills
Round-start skills
Round-end skills
...
```

Do not iterate over all skills for hooks only some skills implement.

---

# 11. Boolean Hook Semantics

Before replacement, document the exact semantics of every boolean hook.

Examples include:

- `PlayerHurtPre`
- `WeaponDrop`
- `OnWeaponCanAcquire`

For each:

- What does `true` mean?
- What does `false` mean?
- Is the result OR-combined, AND-combined, last-result-wins, or short-circuited?
- In what order are skills invoked?
- Can more than one active skill affect the event?
- Is the current player's skill called, all active skills called, or both attacker and victim skills called?

Write characterization tests for these rules before changing the dispatcher.

---

# 12. Player Runtime State

Replace `jSkill_PlayerInfo` with a clear runtime model.

Suggested name:

```csharp
PlayerRuntimeState
```

Include:

- Player identity.
- Bot flag.
- Current `SkillId`.
- Special `SkillId`.
- Rolled value/chance.
- Drawing state.
- HUD expiration.
- Description expiration.
- HUD suppression.
- Custom HTML.
- Hide-HUD flags.
- Skill-used state.
- Death-HUD state.

Preserve bot-takeover semantics exactly.

Do not confuse:

- Human controller.
- Bot controller.
- Controlled pawn.
- Event controller.
- Stored runtime owner.

Characterize and test the current `PlayerManager.GetPlayerEvent` and takeover behavior before changing model names.

---

# 13. Global Configuration

Create one typed effective configuration root.

Example:

```csharp
public sealed record HeroShiftConfiguration
{
    public int SchemaVersion { get; init; } = 1;
    public GeneralOptions General { get; init; } = new();
    public HudOptions Hud { get; init; } = new();
    public ChatOptions Chat { get; init; } = new();
    public CommandOptions Commands { get; init; } = new();
    public VotingOptions Voting { get; init; } = new();
    public IReadOnlyDictionary<SkillId, SkillOverride> Skills { get; init; }
        = new Dictionary<SkillId, SkillOverride>();
}
```

The code contains canonical defaults.

`heroshift.json` contains overrides only.

Example:

```json
{
  "schemaVersion": 1,
  "general": {
    "gameMode": "NoRepeat",
    "enableBotSkills": true
  },
  "skills": {
    "dash": {
      "enabled": true,
      "options": {
        "cooldownSeconds": 2.5
      }
    }
  }
}
```

Do not serialize every default into the generated file.

Provide a minimal default file with comments only if JSON-with-comments is intentionally supported. Otherwise use standard JSON and document fields in README.

Use the existing JSON library unless there is a compelling verified reason not to. Do not add dependencies.

---

# 14. Configuration Merge

Configuration resolution:

```text
Canonical code defaults
        ↓
Read heroshift.json overrides
        ↓
Validate schemaVersion
        ↓
Reject unknown root sections
        ↓
Resolve global options
        ↓
Resolve per-skill metadata overrides
        ↓
Resolve typed per-skill options
        ↓
Validate full effective configuration
        ↓
Create immutable snapshot
```

Requirements:

- Atomic snapshot replacement.
- No partially applied configuration.
- On reload failure, retain previous valid snapshot.
- On initial load failure, log actionable errors and fail safely.
- Include JSON path in every error.
- Report unknown skill IDs.
- Report unknown fields.
- Report type mismatches.
- Report range violations.
- Do not swallow exceptions without context.

Recommended error:

```text
[HeroShift] Invalid configuration at skills.dash.options.cooldownSeconds:
value must be greater than or equal to 0
```

---

# 15. Configuration Validation

Validate at minimum:

## Global

- `schemaVersion` supported.
- Durations valid.
- Probabilities in valid range.
- Player-count thresholds valid.
- Command alias lists non-empty where required.
- Command aliases normalized.
- Duplicate aliases detected.
- Permission strings accepted as strings, including empty public permission.
- Color and HUD fields are non-null where required.

## Skills

- Every registered skill has one definition.
- Every built-in ID is unique.
- Every definition has options of the expected type.
- Every override references a known skill.
- `MaxPerServer >= -1`.
- Durations are valid.
- Probabilities and multipliers follow existing semantics.
- Team restrictions are valid.
- Rarity values are valid.
- Skill-specific ranges are validated without altering existing accepted behavior.

Do not impose new restrictions that reject valid current configurations unless required for safety.

---

# 16. Localization

Move built-in English resources into the main DLL.

Suggested resource:

```text
HeroShift - SRC Files/src/Localization/Resources/en.json
```

Mark as an embedded resource in the project.

The English catalog is canonical.

External language files are optional:

```text
plugins/HeroShift/languages/he.json
plugins/HeroShift/languages/pl.json
```

Recommended fallback:

```text
External selected language
→ Embedded English
→ Translation key
```

Requirements:

- No external `en.json` required for basic operation.
- External English may optionally override built-in English if explicitly supported.
- Preserve current placeholder formatting behavior.
- Preserve chat color replacement.
- Preserve alternate skill-button replacement.
- Preserve illiterate-skill behavior.
- Preserve dynamic percentage formatting.
- Preserve `welcome_message` behavior.

Use deterministic keys such as:

```text
skills.dash.name
skills.dash.description
skills.dash.description.dynamic
```

A temporary translation-key mapping is acceptable during migration, but the final system must not depend on enum/class-name coincidence.

---

# 17. Translation Validation

Build-time or test-time validation must check:

- Every skill has an English name.
- Every skill has an English description.
- Dynamic descriptions exist where used.
- No duplicate keys.
- No malformed format strings.
- Placeholder sets match between base English and optional translations.
- Unknown external keys produce warnings or errors according to a documented rule.
- Missing external keys fall back to English.

Placeholder comparison must understand:

```text
{0}
{PLAYER}
{SERVER_NAME}
{VERSION}
{SKILLS_COUNT}
```

Do not treat literal braces as placeholders incorrectly.

---

# 18. WASDMenu Adapter

Add:

```csharp
public interface IWasdMenuService
{
    void Initialize(BasePlugin plugin, bool hotReload);
    void Open(...);
    void Close(...);
    void CloseAll();
}
```

Implementation delegates to the existing WASDMenu API.

Do not rewrite WASDMenu internals unless required to compile with the new architecture.

Preserve:

- Player dictionary lifecycle.
- Menu ownership.
- Tick registration.
- Input edge detection.
- Selection behavior.
- HTML output.
- Nullability behavior from the current code.

Add smoke or unit-level adapter tests where possible, but do not mock CS2 behavior inaccurately.

---

# 19. RayTrace Adapter

Add:

```csharp
public interface IRayTraceService
{
    bool IsAvailable { get; }

    CustomTraceResult? TraceShape(...);
    CustomTraceResult? EyeTrace(...);
    CustomTraceResult? TraceHullShape(...);
}
```

The implementation should move or wrap existing code with minimal logic changes.

Do not alter:

- Capability lookup string.
- Native API calls.
- Trace options.
- Masks.
- Default max distance behavior.
- Player exclusion.
- Beam drawing.
- Hit-result conversion.
- Error handling behavior.

Skill-specific max distance must come from typed skill options instead of string lookup.

---

# 20. Command System

Preserve current command behavior.

Canonical command defaults belong in code.

`heroshift.json` may override:

- Aliases.
- Permissions.
- Voting enablement.
- Voting timing.
- Voting percentages.
- Start parameters.
- Server start parameters.

Represent aliases as arrays in the new schema:

```json
{
  "commands": {
    "setSkill": {
      "aliases": ["setskill", "set_skill"],
      "permission": "@HeroShift/admin"
    }
  }
}
```

Do not retain comma-separated strings internally.

During loading:

- Trim aliases.
- Reject empty aliases.
- Normalize case according to CounterStrikeSharp behavior.
- Detect duplicates across commands.
- Preserve `css_` registration behavior.
- Preserve command removal and re-registration on reload.
- Preserve server-console behavior when `player == null`.

---

# 21. Voting System

Preserve:

- Admin bypass.
- Non-admin voting behavior.
- Voting availability.
- Minimum-player rules.
- Cooldowns.
- Percentage calculation.
- Vote creator counting as a vote.
- Bot exclusion from denominator.
- Timer cancellation.
- Console command execution path.
- Translation messages.

Add characterization tests for threshold calculation and cooldown selection.

---

# 22. Plugin Lifecycle

Preserve load order unless an explicit dependency requires adjustment.

Target load sequence:

```text
Set Instance
Load embedded English catalog
Load and validate configuration
Create configuration snapshot
Create skill catalog
Create skill registry
Initialize RayTrace adapter
Initialize WASDMenu adapter
Register debug/performance services
Register event handlers
Register tick handlers
Register commands
Load enabled skills
Synchronize existing players on hot reload
Register manifest resources
Print startup diagnostics
```

Unload sequence must explicitly clean:

- Registered commands.
- Event handlers.
- Tick listeners.
- Menus.
- Timers where owned.
- Runtime registries.
- Player state.
- Tracked entities.
- Static caches where necessary.

Avoid relying on process shutdown.

---

# 23. Skill Migration Procedure

Migrate all skills, but do it in reviewable batches.

For each skill:

1. Preserve the new documentation comments.
2. Declare stable `SkillId`.
3. Create typed options.
4. Move metadata defaults into the canonical definition.
5. Register implemented hooks explicitly.
6. Replace `SkillsInfo.GetValue`.
7. Replace enum comparisons with `SkillId`.
8. Preserve all static and per-player state.
9. Preserve timers and cleanup.
10. Preserve event signatures.
11. Preserve return behavior.
12. Add or update validation.
13. Run build and catalog validation.

Recommended batch categories:

## Batch A: Simple passive skills

Skills with:

- Minimal state.
- No RayTrace.
- No WASDMenu.
- No entity lifecycle.

## Batch B: Tick and movement skills

Examples:

- Dash.
- BunnyHop.
- Regeneration.
- Movement modifiers.

## Batch C: Damage pipeline skills

Skills using:

- Damage pre.
- Damage post.
- TakeHealth.
- Reflection or armor behavior.

## Batch D: Entity and grenade skills

Skills creating, tracking or removing entities.

## Batch E: RayTrace skills

Skills using aim traces, line of sight, hull traces or hit helpers.

## Batch F: Menu and targeted skills

Skills using WASDMenu, target selection or command arguments.

## Batch G: Remaining complex skills

Multi-stage, curse, bomb, smoke, decoy and lifecycle-heavy skills.

Each batch must be its own commit or small set of commits.

---

# 24. Remove Legacy Architecture

Only remove the old system after all consumers are migrated.

Remove:

```text
src/utils/SkillsInfo.cs
Legacy SkillConfig reflection discovery
SkillsInfo.GetValue
Reflection member cache
Reflection skill method cache
HeroShift.SkillAction
Skills enum as canonical identity
config.json
skillsInfo.json
External required en.json
```

Before deletion, search:

```bash
rg "SkillsInfo"
rg "SkillAction"
rg "SkillConfig"
rg "skillsInfo\.json"
rg "config\.json"
rg "src/lang/en\.json"
```

Expected final result: no runtime consumers.

If a term remains only in migration documentation or changelog, verify it is intentional.

---

# 25. Generated Release Artifact

The release package must be generated from:

- Build output.
- Source-controlled static assets.
- Minimal default `heroshift.json`.
- Gamedata.
- Optional empty languages directory or documented example translations.
- Required runtime dependencies already part of the repository's current delivery model.

Do not use manually synchronized copies as the canonical source.

Recommended generated layout:

```text
package/
└── addons/
    ├── counterstrikesharp/
    │   ├── plugins/
    │   │   └── HeroShift/
    │   │       ├── HeroShift.dll
    │   │       ├── WASDMenuAPI.dll
    │   │       ├── Newtonsoft.Json.dll
    │   │       ├── configs/
    │   │       │   └── heroshift.json
    │   │       └── languages/
    │   ├── shared/
    │   │   └── RayTraceApi/
    │   │       └── RayTraceApi.dll
    │   └── gamedata/
    │       └── HeroShift.gamedata.json
    └── metamod/
        └── RayTrace.vdf
```

Adjust paths only after verifying the repository's current server layout.

Preserve any additional required RayTrace implementation files in their current locations.

---

# 26. Packaging Script

Create a deterministic repository script.

Preferred options:

```text
scripts/package.ps1
scripts/package.sh
```

or a cross-platform .NET-based packaging tool already supported by the repository.

Do not add a new external dependency.

The packaging process must:

1. Clean only its own staging directory.
2. Build Release.
3. Create a fresh staging directory.
4. Copy exact required files.
5. Exclude PDBs from release ZIP unless intentionally included.
6. Validate required file presence.
7. Fail on missing dependencies.
8. Produce a deterministic inventory.
9. Create ZIP.
10. Never modify source files.
11. Never commit or tag automatically.

The existing release script may remain as a release orchestrator, but packaging and committing/tagging must be separable.

---

# 27. CI

Add or update CI without upgrading action versions unless required.

Required jobs:

## Windows build

```text
dotnet restore
dotnet build -c Release
```

## Linux build

Run on Ubuntu with .NET 10.

Validate that:

- Project paths work.
- MSBuild paths do not depend on Windows separators.
- Language and gamedata resources build correctly.
- Packaging script can stage artifacts.

## Static validation

Run:

- Skill catalog validation.
- Duplicate ID detection.
- Typed option binding tests.
- Localization validation.
- Command alias validation.
- Package inventory validation.

## Tests

If a test project does not exist, add one using existing repository-compatible tooling. Adding a test project is acceptable; introducing new third-party test dependencies requires approval if not already available through the SDK or repository.

Prefer the existing ecosystem and minimal additions.

---

# 28. Linux and Docker Portability

The target runtime is a Linux CS2 dedicated server inside a Docker container, often via Docker Desktop.

Requirements:

- Use `Path.Combine`.
- Use `Path.GetFullPath` carefully.
- Do not assume Windows path separators.
- Do not assume current working directory.
- Resolve paths from `Instance.ModuleDirectory` or explicit project locations.
- Treat filesystem casing as case-sensitive.
- Use atomic replacement within the same directory where possible.
- Do not rely on PowerShell-only build behavior for CI validation.
- Ensure all runtime file names use correct case.
- Ensure embedded-resource names are validated on Linux.

Document expected Docker volume mounts.

Example:

```text
Host:
  ./server-config/HeroShift/heroshift.json

Container:
  /path/to/cs2/game/csgo/addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json
```

Do not hardcode this example path in runtime code.

---

# 29. Testing Strategy

## Unit and static tests

Test:

- `SkillId` normalization.
- Duplicate IDs.
- Skill lookup.
- Typed option merging.
- Unknown option rejection.
- Invalid type rejection.
- Global configuration merging.
- Atomic snapshot behavior.
- Localization fallback.
- Placeholder validation.
- Command alias validation.
- Voting threshold calculations.
- Skill metadata equivalence to baseline.

## Characterization tests

Before replacing behavior, test existing semantics of:

- Skill dispatch.
- Damage hooks.
- Round reset.
- Map reset.
- Player death.
- Team switching.
- Bot takeover.
- Command reload.
- HUD expiration.
- Active skill tracking.
- Curse limits.
- Random skill selection.
- Rarity selection.
- Max-per-server rules.

## Build tests

Run:

```bash
dotnet restore "HeroShift - SRC Files/HeroShift.sln"
dotnet build "HeroShift - SRC Files/HeroShift.sln" -c Debug
dotnet build "HeroShift - SRC Files/HeroShift.sln" -c Release
```

Run on Windows and Linux where available.

## Package tests

Assert the ZIP contains every required file at the exact path.

## Runtime smoke test

Perform only after static and build checks pass.

Inside the CS2 Docker environment verify:

- Cold server start.
- Plugin load.
- No missing dependency errors.
- Correct skill count.
- Correct enabled-skill count.
- `heroshift.json` loaded.
- Embedded English works without an external `en.json`.
- Optional language override works.
- Hot reload.
- Map change.
- Round start/end.
- Skill drawing.
- Skill activation.
- Skill disable/reset.
- Commands.
- Voting.
- WASDMenu navigation and selection.
- RayTrace-dependent skill.
- RayTrace missing-module fallback if testable.
- Bot join.
- Bot skill.
- Human bot takeover.
- Disconnect/reconnect.
- Server shutdown/unload.

---

# 30. Baseline Equivalence Checks

Create an automated comparison between old baseline and new catalog.

Compare:

- Skill count.
- Skill IDs.
- Metadata defaults.
- Skill-specific defaults.
- Hook membership.
- Translation presence.
- Command defaults.
- Voting defaults.

Differences must be explicitly reviewed.

The comparison may allow intentional schema naming changes while requiring semantic equality.

Example mapping:

```text
Old: Cooldown = 2.0
New: CooldownSeconds = 2.0
Result: equivalent
```

No unexplained difference may remain.

---

# 31. Observability and Startup Diagnostics

Improve diagnostics only where it does not change gameplay.

Startup output should report:

- Plugin version.
- Configuration schema version.
- Effective config path.
- Skill count.
- Enabled skill count.
- Translation source.
- WASDMenu status.
- RayTrace capability status.
- Gamedata status.
- Validation warnings.

Do not perform blocking network version checks on the game thread.

Preserve existing version-check behavior unless separately approved. If retained, isolate it and fail silently as today.

---

# 32. Performance Requirements

The refactor must not introduce per-tick reflection or repeated deserialization.

Requirements:

- Immutable configuration snapshot.
- Pre-indexed hook lists.
- Cached typed options.
- No string property lookup in hot paths.
- No assembly scanning after startup.
- Avoid locks in high-frequency event paths where current concurrent structures suffice.
- Preserve or improve existing performance logging.
- Avoid allocating temporary collections every tick where practical.

Performance optimization must not change semantics.

---

# 33. Changelog

After every repository change, maintain `CHANGELOG.md`.

Under:

```markdown
## Unreleased
```

Use relevant sections:

```markdown
### Added
### Changed
### Fixed
### Removed
### Security
### Deprecated
```

The final refactor should document:

- New typed skill catalog.
- New `heroshift.json`.
- Embedded English resources.
- Optional external languages.
- Removed legacy configuration files.
- Generated packaging.
- Linux build support.

Do not claim runtime verification until performed.

---

# 34. Recommended Commit Plan

Use these as logical checkpoints. Adjust only when a different split is clearly safer.

## Commit 1

```text
test: capture HeroShift refactor baseline
```

Include:

- Baseline extractor.
- Current defaults snapshot.
- Skill-hook snapshot.
- Translation-key snapshot.
- Package inventory snapshot.
- Initial tests.

## Commit 2

```text
refactor: add typed skill identity and definitions
```

Include:

- `SkillId`.
- Metadata model.
- Definition model.
- Registry skeleton.
- Validation.
- No gameplay migration yet.

## Commit 3

```text
refactor: add typed configuration snapshots
```

Include:

- Global configuration models.
- Canonical defaults.
- Override loader.
- Validation.
- Atomic snapshot.
- Tests.
- Temporary coexistence with old config allowed.

## Commit 4

```text
refactor: embed English localization resources
```

Include:

- Embedded English.
- Localization service.
- External language fallback.
- Placeholder validation.
- Compatibility mapping for current keys if required.

## Commit 5

```text
refactor: replace reflection skill dispatch
```

Include:

- Hook registration model.
- Dispatcher.
- Boolean-hook characterization.
- Temporary bridge if required.
- Do not remove old dispatch until all skills migrate.

## Commit 6

```text
refactor: migrate passive skills to typed options
```

## Commit 7

```text
refactor: migrate tick and movement skills
```

## Commit 8

```text
refactor: migrate damage pipeline skills
```

## Commit 9

```text
refactor: migrate entity and grenade skills
```

## Commit 10

```text
refactor: migrate RayTrace skills
```

## Commit 11

```text
refactor: migrate menu and targeted skills
```

## Commit 12

```text
refactor: migrate remaining complex skills
```

## Commit 13

```text
refactor: integrate typed runtime state and commands
```

Include:

- Player runtime state.
- Command config.
- Voting config.
- Reload flow.
- Bot takeover compatibility.

## Commit 14

```text
refactor: remove legacy configuration and dispatch
```

Include removal of:

- `SkillsInfo`.
- Reflection dispatch.
- Legacy config files.
- Required external English file.
- Old enum identity where no longer needed.

## Commit 15

```text
build: generate HeroShift release artifacts
```

Include:

- Packaging script.
- Generated default configuration.
- Package inventory validation.
- Release workflow integration.

## Commit 16

```text
ci: validate HeroShift on Windows and Linux
```

Include:

- Windows build.
- Linux build.
- Tests.
- Catalog validation.
- Package validation.

## Commit 17

```text
docs: document HeroShift configuration and deployment
```

Include:

- README update.
- `heroshift.json` documentation.
- Language override documentation.
- Docker deployment.
- Upgrade notes.
- Final changelog cleanup.

Every commit should be revertable without corrupting unrelated history.

---

# 35. Stop Conditions

Stop and report before proceeding when:

- `development` unexpectedly diverges.
- Uncommitted user changes overlap refactor files.
- A dependency version change appears necessary.
- A required external binary is missing.
- Baseline extraction finds inconsistent Skill definitions.
- Current code has ambiguous boolean-hook semantics.
- A build cannot be performed because the required .NET SDK is unavailable.
- Linux build requires a package upgrade.
- A skill's documented defaults disagree with its code.
- A runtime path cannot be determined safely.
- WASDMenu or RayTrace behavior cannot be preserved with confidence.
- A generated package would omit an existing required file.

Do not guess through high-impact ambiguity.

---

# 36. Final Acceptance Criteria

The refactor is complete only when all of the following are true:

## Architecture

- Code is the canonical source for defaults.
- All skills use stable `SkillId`.
- All skill options are typed.
- All skill metadata is canonical.
- No reflection-based skill method dispatch remains.
- No reflection-based skill option access remains.
- No `SkillsInfo.GetValue` remains.
- No runtime dependency on `skillsInfo.json`.
- No runtime dependency on legacy `config.json`.
- English works from embedded resources.
- Optional language files work.

## Behavior

- Every prior skill still exists.
- Every skill is registered exactly once.
- Every prior hook is represented.
- All baseline defaults match semantically.
- Commands and permissions match.
- Voting behavior matches.
- WASDMenu behavior matches.
- RayTrace behavior matches.
- Bot takeover behavior matches.
- Hot reload works.
- Round and map resets work.

## Build and package

- Debug build passes.
- Release build passes.
- Windows build passes.
- Linux build passes.
- Tests pass.
- Package validation passes.
- ZIP contains exact required files.
- Release artifact is generated, not manually synchronized.

## Repository

- Only `development` changed.
- `main` unchanged.
- `CHANGELOG.md` updated.
- Documentation updated.
- No dependency upgrades.
- No AI attribution trailers.
- Commit history is small, clear and revertable.

---

# 37. Final Implementation Report Format

At the end, report exactly:

## Status

State whether the full refactor is complete, partial, blocked, or complete pending runtime validation.

## Summary

Concise description of architectural and behavior-preservation work.

## Files Changed

List exact paths grouped by area.

## Changelog

State the exact `Unreleased` entries added.

## Verification

List every command executed and its result.

Separate:

- Static validation.
- Unit tests.
- Debug build.
- Release build.
- Windows CI.
- Linux CI.
- Package validation.
- Docker runtime smoke test.

Never claim a check passed without evidence.

## Risks / Notes

Include:

- Anything not runtime-tested.
- Skills requiring focused manual review.
- External dependency assumptions.
- Remaining behavior uncertainty.
- Known intentional compatibility changes.

## Commits

List each commit SHA and subject in order.

## Commit Message

If additional uncommitted work remains, provide a copy-ready English Conventional Commit message.

---

# 38. Execution Instruction

Execute the plan incrementally using the commit boundaries above.

Do not implement the entire refactor as one giant unreviewable commit.

At each phase:

1. Inspect.
2. Implement.
3. Review the diff.
4. Run relevant checks.
5. Update changelog.
6. Commit.
7. Verify branch state.
8. Continue.

The objective is a complete end-to-end refactor with preserved runtime behavior, not merely a new configuration loader or partial abstraction layer.
