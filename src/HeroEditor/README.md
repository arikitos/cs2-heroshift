# HeroEditor

HeroEditor is a local tool for browsing and editing HeroShift skill names, descriptions, metadata, and tunable options.

## Recommended startup

Run the launcher from PowerShell.

```powershell
./src/HeroEditor/start.ps1
```

The launcher starts a local server, opens the browser, and loads the repository root automatically. This mode enables all editor actions, including local release packaging.

Opening `index.html` directly remains supported in Chrome and Edge. The first direct opening requires selecting the repository root because browsers cannot grant filesystem access automatically. The selected directory handle is stored in IndexedDB and is restored automatically on later openings when permission remains granted.

## Header actions

The header contains search, Reset to Default, Save Changes, and Publish Local Zip.

Reset to Default clears every skill override and restores names, descriptions, metadata, and options from `skills.generated.json` and `description.bindings.json`.

Save Changes writes these files.

```text
src/HeroShift/Localization/Resources/en.json
config/heroshift.json
src/HeroEditor/description.bindings.json
```

Publish Local Zip first saves the current editor state, calculates the next patch version from the project version, local release archives, and local Git tags, then executes the root release script with `NoPublish`. It creates a local archive only. It never creates or pushes a Git tag and never creates a GitHub release.

## Dynamic descriptions

A description can reference skill options with tokens.

```text
{{optionName}}
{{optionName|percent}}
{{optionName|seconds}}
{{optionName|multiplier}}
{{optionName|currency}}
```

The editor shows the rendered description below the editable template. Option changes update the rendered description immediately. Saved localization contains the rendered text, so the game receives a normal string without editor tokens.

`description.bindings.json` contains templates for every skill that has player facing numeric or Boolean options. Percentages, timing, damage, health, ranges, movement values, limits, chances, multipliers, and similar gameplay quantities are resolved from the effective option values instead of being duplicated as hardcoded text.

Descriptions for fixed behavior that is not exposed as an option avoid numeric claims that could drift. For example, Chicken now describes faster movement and reduced health without duplicating its internal constants, and Behind describes turning the enemy to face the opposite direction instead of hardcoding an angle.

The HeroEditor test suite validates that every gameplay numeric or Boolean option is represented by a description token, every token points to an existing option, and every numeric default description has a binding. These tests run in CI on Windows and Linux.

## Regenerating skill data

`skills.generated.json` is generated from the reflection baseline and English localization.

```powershell
python src/HeroEditor/regenerate.py
```

Run regeneration after adding or removing a skill or changing code defaults.
