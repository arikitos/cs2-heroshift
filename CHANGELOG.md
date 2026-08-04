# Changelog

All notable changes to this project are documented in this file.

## Unreleased

### Added

- Add stable typed skill IDs, canonical typed metadata, and typed option records for all 142 built-in skills.
- Add an explicit `SkillRegistry` and `SkillDispatcher` with typed lifecycle, player, damage, tick, entity, round, objective, grenade, transmit, and disconnect hooks.
- Add atomic `heroshift.json` configuration snapshots with deep override binding, unknown-field detection, typed skill-option validation, and invalid-reload rollback.
- Add embedded English resources with optional external-language overrides, English fallback, and translation-placeholder validation.
- Add explicit `IGameMenuService` and `ITraceService` runtime boundaries for WASDMenu and the external RayTrace capability.
- Add configuration, localization, registry, dispatcher, lifecycle, Boolean-hook, runtime-adapter, player-state, voting-policy, and complete baseline-equivalence tests.
- Add strict validation for global values, unified command aliases, skill option ranges, duplicate translation keys, malformed placeholders, unknown translation keys, and selected language paths.
- Add deterministic release packaging with an exact file inventory, per-file SHA-256 hashes, fixed ZIP timestamps, and the complete pinned RayTrace managed and Linux Metamod runtime.
- Add Windows and Linux CI that restores, runs all tests, builds Debug and Release, validates architecture invariants, generates each package twice, compares package hashes, and uploads the generated archive.

### Changed

- Make code the canonical source for global defaults, skill defaults, skill metadata, stable IDs, and built-in English resources.
- Use `heroshift.json` for server-specific overrides only.
- Replace reflection and method-name string dispatch with explicit typed delegates while preserving event order, damage ordering, tick ordering, freeze-time filtering, failure suppression, performance logging, history tracking, and curse ownership.
- Route all live runtime call sites through typed skill lifecycle and dispatcher APIs, including commands, voting, bots, bot takeover, WASDMenu targeting, skill copy/deactivation flows, rounds, entities, damage, tick, and disconnect cleanup.
- Replace the final runtime skill enum and legacy player-state records with stable `SkillId`, `PlayerRuntimeState`, and `PlayerStateStore` types.
- Load the optional language selected by `general.language` and publish configuration and localization together only after both validate.
- Apply each voting command's typed threshold, timing, cooldown, minimum-player, and alias settings when a vote is created.
- Stamp the plugin assembly and reported module version from the requested release version without editing source files.
- Isolate the bundled WASDMenu implementation without changing its input, selection, pause, hot-reload, or HTML behavior.
- Isolate RayTrace without changing `raytrace:craytraceinterface`, trace masks, native calls, ignored entities, hull behavior, debug beams, missing-module behavior, or server dependency paths.
- Generate release archives from build output, source-controlled packaging assets, and SHA-256-verified official RayTrace release archives instead of maintaining manually synchronized server binaries.
- Make `release.ps1` a non-mutating local packaging wrapper; it no longer edits source, commits, tags, or pushes.
- Build and publish tagged releases through the same tested deterministic packaging script used by CI.

### Removed

- Remove `SkillsInfo`, all nested legacy `SkillConfig` models, string-based option lookup, reflection member caches, assembly scanning, `MethodInfo.Invoke`, and `SkillAction`.
- Remove the legacy global `Config` facade and duplicate global-default models.
- Remove runtime support for `config.json` and `skillsInfo.json`.
- Remove the required external English file; English now works from the embedded resource.
- Remove checked-in generated DLLs, copied gamedata, the manually synchronized `HeroShift - Server Files` release source, post-build repository mutation, and one-off refactor migration workflows.

### Fixed

- Preserve cleanup for all 19 stateful skills on player disconnect.
- Preserve existing Boolean-hook aggregation and short-circuit rules through characterization tests.
- Preserve bot takeover ownership, active-skill ordering, round/map reset behavior, command aliases, permissions, vote counting and bot exclusion, HUD behavior, WASDMenu behavior, and RayTrace degradation behavior during the architectural cutover.
- Keep the previous valid immutable configuration snapshot active when a hot reload fails validation.
- Explicitly cancel owned vote, bot, draw, menu, and tick state and clear player, entity, skill, command, and runtime caches during unload and hot reload.
- Normalize package manifest paths across Windows and Linux and reject unexpected, missing, PDB, or XML files from release archives.
- Repair the captured global configuration baseline so it is valid deterministic JSON and enforce every canonical global, HUD, chat, command, and voting default against it.
- Characterize the 19 intentional `PlayerDisconnect` cleanup hooks separately while preserving strict baseline equality for every gameplay hook.
- Run the full refactor validation workflow for direct updates to both `main` and `development`.
