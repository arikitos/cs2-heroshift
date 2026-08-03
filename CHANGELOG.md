# Changelog

All notable changes to this project are documented in this file.

## Unreleased

### Added

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
