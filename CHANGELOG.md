# Changelog

All notable changes to this project are documented in this file.

## Unreleased

### Changed

- Isolate the bundled WASDMenu implementation and external RayTrace capability behind `IGameMenuService` and `ITraceService` adapters, preserving the current manager behavior, capability key, native trace semantics, warning behavior and server dependency paths.

- Remove the final `Config` compatibility facade; all runtime consumers now read the canonical immutable typed configuration snapshot directly, including typed command aliases, permissions and game modes.

- Remove the `SkillsInfo` compatibility facade and all 142 nested `SkillConfig` duplicates; skill selection now reads the immutable effective skill snapshot directly.

- Remove the final generic `SkillAction` compatibility dispatcher and migrate commands, bots, menu targeting and skill-copy/deactivation flows to explicit typed lifecycle coordinator methods.

- Move skill load/enable/disable/use/type/reset/round-end lifecycle into explicit typed coordinator methods that preserve PerfLog, active/map history and curse ownership side effects; migrate RoundEvents and remove the generic event string fan-out helpers.

- Route the sorted per-frame skill loop through a non-swallowing typed tick invocation while preserving freeze-time filtering, AreaReaper/ChillOut ordering and one-log-per-skill-per-round failure suppression.

- Route the ordered damage pre/post pipeline through typed dispatcher calls while preserving deferred revive-skill ordering and per-skill debug damage snapshots.

- Route player, weapon, grenade, hurt-suppression, death, disconnect and skill-use callbacks through typed dispatcher methods while retaining the specialized tick and damage-order paths for separate migration.

- Route entity, objective, grenade, trigger, weapon-acquisition and transmit callbacks through the typed dispatcher while preserving active-skill ordering and Fortnite wall damage handling.

- Replace assembly scanning and `MethodInfo.Invoke` in the compatibility skill dispatcher with explicit typed delegates resolved from `SkillRegistry`.

- Cut runtime configuration over to atomic `heroshift.json` snapshots with canonical typed global defaults, per-skill metadata/options binding, explicit legacy-ID mapping, and embedded-English localization fallback.

- Completed canonical typed definitions, metadata, explicit hook registrations, and typed option records for all 142 built-in skills; live runtime cutover remains a separate checkpoint.

- Migrated the remaining complex / target-selection skills batch to canonical typed skill definitions and options while retaining the legacy runtime dispatcher until the final cutover.

- Migrated the raytrace skills batch to canonical typed skill definitions and options while retaining the legacy runtime dispatcher until the final cutover.

- Migrated the entity and grenade skills batch to canonical typed skill definitions and options while retaining the legacy runtime dispatcher until the final cutover.

- Migrated the damage pipeline skills batch to canonical typed skill definitions and options while retaining the legacy runtime dispatcher until the final cutover.

- Migrated the tick and movement skills batch to canonical typed skill definitions and options while retaining the legacy runtime dispatcher until the final cutover.

- Migrated the passive skills batch to canonical typed skill definitions and options while retaining the legacy runtime dispatcher until the final cutover.

### Removed

- Remove obsolete split `config.json` / `skillsInfo.json` server resources and duplicate external English copies; `heroshift.json` plus embedded English are the packaged defaults, while an operator-provided language file remains an optional override.

- Stop reading `config.json` and `skillsInfo.json` at runtime; the files remain only until the dedicated legacy-cleanup checkpoint removes obsolete artifacts and nested compatibility models.

### Fixed

- Preserve player-disconnect cleanup for all 19 stateful skills by registering and invoking the typed `PlayerDisconnect` hook.
- Repair runtime-cutover compilation by importing the legacy Skill identity into the compatibility facade and defining the typed max-distance contract used by RayTrace skills.
- Generate runtime-cutover payloads from the staged index so new configuration, compatibility, test, and server-resource files are included alongside tracked modifications.
- Decouple runtime-cutover code application from changelog context and update `Unreleased` idempotently after the reviewed patch applies.
- Make runtime-cutover invariant checks deterministic and diagnostic, reporting the exact missing file, initialization, or remaining legacy dependency.
- Restrict typed option discovery to the nested `SkillConfig` body, prefer its constructor parameter types, and normalize fully qualified enum defaults so generated options match the effective configuration contract.

- Make typed skill identity available to every migrated gameplay implementation through a core global using, preventing generated option accessors from failing compilation when a source file did not already import the abstractions namespace.
- Initialize typed skill option snapshots lazily from the active legacy configuration during migration, automatically rebuilding once after a successful config reload so partially migrated skills remain runtime-safe before the final `heroshift.json` cutover.
- Invalidate cached registry hook indexes whenever a new definition is registered, and register all typed definitions already present on the branch.

### Added

- Stage an owner-gated final lifecycle cleanup that removes `SkillAction` only after zero-callsite checks, tests and Release build pass.
- Add typed dispatcher APIs for skill lifecycle, disconnect, entity, team, bomb, decoy, smoke and trigger hooks before migrating live event call sites.
- Staged an owner-gated typed-dispatch bridge that must pass invariants, tests, and Release build before replacing reflection-based skill invocation.
- Staged the reviewed compressed runtime-configuration patch for the owner-gated cutover workflow; payload files are removed automatically by the validated cutover commit.
- Added an owner-gated cutover workflow that applies the reviewed runtime-configuration patch, validates it on Linux, and commits only after tests and Release build succeed.

- `tools/refactor-migration/` and `.github/workflows/refactor-migration.yml`: deterministic, owner-gated generation of typed skill migration batches from the committed baseline, with diff validation, tests and Release build required before each checkpoint commit.
- `.github/workflows/refactor-validation.yml`: Linux restore, test and Release-build validation for the `refactor` branch, plus a source snapshot artifact used to continue and verify the end-to-end refactor outside GitHub's file-by-file API.
- `HeroShift - SRC Files/tools/refactor-baseline/`: a development-only baseline extractor
  used by the HeroShift architecture refactor (see `REFACTOR.md`). It parses the current
  skill sources and produces a deterministic JSON snapshot of every skill's ID, implemented
  hooks, and metadata/option defaults, plus the current localization key set and release
  package inventory. Never shipped in the release package.
- `src/Skills/` (namespace `src.SkillsCore`): typed skill identity and definition model
  (`SkillId`, `BuiltInSkillIds`, `SkillMetadata`, `SkillHookSet`, `SkillDefinition<TOptions>`,
  `SkillRegistry`) that will replace the reflection-driven `Skills` enum and `SkillsInfo`
  system. Additive only in this commit — not yet wired into plugin load or the event
  pipeline; the existing dispatch keeps running unchanged.
- `HeroShift - SRC Files/tests/HeroShift.Tests/`: new xUnit test project with initial
  coverage for `SkillId` normalization/parsing and `SkillRegistry` registration, duplicate
  detection, and hook indexing.
- `src/Configuration/`: typed `HeroShiftConfiguration` root (schema version, general/HUD/chat/
  command/voting options, per-skill overrides) with canonical code defaults transcribed from
  the legacy `Config.SettingsModel`. `ConfigurationLoader` reads a `heroshift.json`-shaped
  override JSON, rejects unknown root sections and unknown fields, validates schema version,
  durations, percentages, alias lists (including cross-command duplicate detection) and skill
  IDs, and produces an immutable `ConfigurationSnapshot` — or throws
  `ConfigurationValidationException` with one message per problem, each carrying its JSON path.
  Additive only: not yet wired into plugin load, which keeps reading the legacy `Config.cs`.
- `src/Localization/` (namespace `src.LocalizationCore`): embeds `src/lang/en.json` as a DLL
  resource (`Resources/en.json`) so English works without any external file.
  `LocalizationService` resolves translations through an external-language → embedded-English
  → raw-key fallback chain, preserving the legacy `CHATCOLORS.RED`/`css_useSkill` load-time
  substitutions, the `"welcome"` unformatted-sentinel behavior, and illiterate-player text
  scrambling. `TranslationValidator` checks an external catalog's keys and placeholder sets
  against the embedded English baseline. Additive only: plugin load still reads the legacy
  `src/utils/Localization.cs`.
- `src/Skills/SkillDispatcher.cs`: typed hook dispatcher replacing reflection-based
  `HeroShift.SkillAction`/`DispatchToActiveSkills`. `src/Skills/BOOLEAN_HOOK_SEMANTICS.md`
  characterizes the exact legacy fan-out and short-circuit rules for `PlayerHurtPre` (victim's
  skill asked first, attacker's only if the victim didn't suppress and holds a different
  skill), `OnWeaponCanAcquire` (every distinct active skill asked, first `true` wins — not
  just the acquiring player's own skill), and `WeaponDrop` (declared and implemed by
  `Iana`, but never dispatched anywhere in the legacy codebase — a pre-existing dead hook,
  preserved as-is rather than "fixed"), plus the `OnTick` skill-order and
  `OnTakeDamage`/`OnTakeDamagePost` late-damage-skill ordering rules. `SkillDispatcherTests`
  pins every one of these rules with fakes. Deliberately decoupled from player runtime state
  (callers pass the active `SkillId` list) since that migrates in a later commit — not yet
  wired into the live event pipeline.
