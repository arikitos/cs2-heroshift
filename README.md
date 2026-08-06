# HeroShift

HeroShift is a Counter-Strike 2 plugin for CounterStrikeSharp that gives every player a random (or admin-assigned) hero skill each round, similar to a "wanted"/"hero" gamemode. It ships **146 built-in skills**, a WASD in-game skill menu, a configurable HUD, multi-language chat, and an admin/voting command system for match control.

The runtime uses stable skill identifiers, typed options, explicit hook registration, immutable configuration snapshots, embedded English resources, and verified release packages — no reflection-based dispatch and no legacy `SkillsInfo`/`config.json`/`skillsInfo.json` files.

## Features

* 146 built-in skills, each with its own enable/disable, use, tick, damage, round, and cleanup hooks
* Multiple game modes: `Normal`, `TeamSkills`, `SameSkills`, `NoRepeat` (default), `FullRandom`, `Debug`
* Per-round random skill assignment, plus admin-assigned skills that can be one-round (`setskill`) or persistent across rounds (`setstaticskill`)
* In-game WASD menu for browsing the full skill list
* Configurable on-screen HUD (colors, sizes, duration) showing the player's current skill and description
* Chat announcements for your own skill, the killer's skill, teammates' skills, and an end-of-round summary
* Embedded English localization with optional external per-language override files
* Bot support — skills can be enabled for bots, with a debug bot-kick option
* "Curse" skill limiting, so punishing skills can be capped per player
* Admin voting system for map change, match start, team swap/shuffle, pause, and score changes
* Live config reload (`reload`) without restarting the server
* Raycast-based line-of-sight support for skills that need it, via a bundled RayTrace Metamod plugin

## Requirements

* A Counter-Strike 2 dedicated server
* Metamod
* CounterStrikeSharp
* A .NET 10 compatible CounterStrikeSharp runtime
* PowerShell 7, Git, and GitHub CLI for publishing releases

The release archive includes the pinned RayTrace managed and Linux Metamod runtime.

## Installation

Download the generated release archive and extract it into the Counter-Strike 2 game directory, normally `game/csgo`.

The archive includes these core files.

```text
addons/counterstrikesharp/gamedata/HeroShift.gamedata.json
addons/counterstrikesharp/plugins/HeroShift/HeroShift.dll
addons/counterstrikesharp/plugins/HeroShift/Newtonsoft.Json.dll
addons/counterstrikesharp/plugins/HeroShift/WASDMenuAPI.dll
addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json
addons/counterstrikesharp/plugins/RayTraceImpl
addons/counterstrikesharp/shared/RayTraceApi
addons/metamod/RayTrace.vdf
addons/RayTrace
package-manifest.json
```

## Commands

Every command is registered as `css_<alias>` for each configured alias, so admins can rename commands in `heroshift.json` without touching code. An empty permission means no permission is required.

### Player and admin commands

| Aliases | Description | Default permission |
| --- | --- | --- |
| `t`, `useSkill` | Use your current skill (no arguments) or type/target it (with arguments) | `@HeroShift/admin` |
| `skills` | Open the WASD menu listing all loaded skills | `@HeroShift/admin` |
| `setskill`, `set_skill` | Assign a skill to a target player for the current round | `@HeroShift/admin` |
| `setstaticskill`, `set_static_skill` | Assign a skill to a target player that persists across rounds | `@HeroShift/admin` |
| `next_skill` | Step a target player through the sorted skill list (testing aid) | `@HeroShift/admin` |
| `heal` | Heal yourself by 100 HP | `@HeroShift/admin` |
| `sethealth`, `set_health`, `health` | Set your own HP to an absolute value | `@HeroShift/admin` |
| `hud`, `hood` | Toggle your own skill HUD | *(none)* |
| `reload`, `refresh` | Live-reload `heroshift.json` and the active language file | `@HeroShift/admin` |
| `plantedbomb`, `planted_bomb`, `bomb` | Spawn an already-planted, ticking C4 at your feet (test helper) | `@HeroShift/admin` |
| `botplace`, `bot_place` | Teleport a bot to your position, optionally with godmode (test helper) | `@HeroShift/admin` |
| `ent`, `entity`, `checkentity`, `check_entity`, `checkent`, `check_ent` | Check whether an entity index/handle is still alive (debug) | `@HeroShift/owner` |
| `console`, `sv` | Run a raw server console command | `@HeroShift/owner` |

### Voting commands

Admins run these directly; other players trigger a player vote instead, when voting is enabled for that command.

| Aliases | Description | Default permission | Vote time | Success threshold |
| --- | --- | --- | --- | --- |
| `map`, `changemap` | Change map (numeric argument = Workshop ID, otherwise a map name) | `@HeroShift/admin` | 25s | 90% |
| `start`, `go` | Start or restart the match | `@HeroShift/admin` | 15s | 60% |
| `swap` | Swap CT/T teams | `@HeroShift/admin` | 15s | 90% |
| `shuffle` | Randomly redistribute players across teams | `@HeroShift/admin` | 15s | 90% |
| `pause`, `unpause` | Toggle match pause | `@HeroShift/admin` | 15s | 60% |
| `setscore` | Set CT/T scores: `<command> <ct> <t>` | `@HeroShift/owner` | 15s | 90% |

## Permissions

HeroShift checks CounterStrikeSharp admin flags/groups configured per command:

* `@HeroShift/admin` — general admin actions (assigning skills, match control, reload, etc.)
* `@HeroShift/owner` — higher-trust actions (raw console commands, entity debugging, setting scores)
* `@HeroShift/death` — controls whether a player's HUD is hidden after death

Any command's permission can be set to an empty string in the config to make it available to all players.

## Configuration

The deployed plugin reads `addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json`. The source-controlled default is `config/heroshift.json`.

The minimal valid configuration is shown below.

```json
{
  "schemaVersion": 1
}
```

Code defaults apply to omitted values. Unknown fields, invalid skill identifiers, invalid option names, duplicate aliases, and invalid ranges are rejected before a new configuration snapshot is published — an invalid reload keeps the previous snapshot active.

Configuration is grouped into: general options (game mode, chat/HUD toggles, bot support, curse-skill limits), HUD appearance, chat appearance, command aliases/permissions, voting timing/thresholds, and per-skill overrides (enable/disable, color, team restriction, required permission, HUD duration, rarity, and skill-specific options).

English is embedded in the plugin. Optional language files can be placed at `addons/counterstrikesharp/plugins/HeroShift/languages/<language>.json`.

## Project structure

```text
HeroShift.sln
src/HeroShift
src/WASDMenuAPI
tests/HeroShift.Tests
config/heroshift.json
release.ps1
```

The repository no longer keeps copied server binaries, packaging staging folders, migration tools, or separate packaging scripts.

## Build and test

```powershell
dotnet restore HeroShift.sln
dotnet test HeroShift.sln -c Release --no-restore
dotnet build HeroShift.sln -c Release --no-restore
```

## Releases

Authenticate GitHub CLI, ensure local main matches remote main, then run the root release script.

```powershell
gh auth login
./release.ps1 -Version 1.2.3
```

The script restores, builds, tests, creates `HeroShift-v1.2.3.zip` in the repository root, creates and pushes the matching Git tag, and publishes the GitHub release with the archive attached. Temporary staging stays outside the repository and is removed after packaging.

Use `-NoPublish` to create and validate the root archive without creating a tag or GitHub release. Use `-RayTraceAssetsDirectory` to provide the two pinned RayTrace archives for offline packaging.

## Credits

* **D3X** — original plugin author
* **Juzlus** — modifier
* **ByDexterTR** — contributor
