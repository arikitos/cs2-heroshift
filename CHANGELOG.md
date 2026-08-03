# Changelog

All notable changes to this project are documented in this file.

## Unreleased

### Added

- `HeroShift - SRC Files/tools/refactor-baseline/`: a development-only baseline extractor
  used by the HeroShift architecture refactor (see `REFACTOR.md`). It parses the current
  skill sources and produces a deterministic JSON snapshot of every skill's ID, implemented
  hooks, and metadata/option defaults, plus the current localization key set and release
  package inventory. Never shipped in the release package.
