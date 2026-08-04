# HeroEditor (local-only tool)

A single-page, no-build UI for browsing and editing all 142 HeroShift skills — display name,
description, and per-skill tunable options — instead of hand-editing JSON files.

This is **not part of the release build** (the CI packaging script only zips compiled plugin
output, never `src/`), so it's safe to keep around for local use only.

## Usage

1. Open `index.html` in Chrome or Edge (needs the File System Access API).
2. Click **Open project folder…** and select the repository root (`cs2-heroshift`).
3. Edit any skill's display name, description, color, rarity, team restriction, or options.
   Fields matching the code default show no highlight; changed fields mark the card as
   "overridden" (blue border) and get written to `config/heroshift.json` on save.
4. Click **Save changes** to write both:
   - `src/HeroShift/Localization/Resources/en.json` (display name + description)
   - `config/heroshift.json` (per-skill overrides — metadata + options)
5. **Reset to defaults** on a card removes its override block entirely.

## Regenerating skill data

`skills.generated.json` holds each skill's id, class name, hooks, and code-default
metadata/options — the source of truth the editor diffs overrides against. It's built from:

- `tests/HeroShift.Tests/Fixtures/baseline.json` — reflection-derived skill metadata/options,
  kept accurate by the test suite.
- `src/HeroShift/Localization/Resources/en.json` — display names/descriptions.

Re-run after adding/removing a skill or changing a default:

```
python src/HeroEditor/regenerate.py
```
