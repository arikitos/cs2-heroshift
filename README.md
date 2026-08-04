# HeroShift

HeroShift is a Counter-Strike 2 plugin for CounterStrikeSharp that assigns each player one of 142 built-in skills. The refactored runtime uses stable skill IDs, typed skill options, explicit hook registration, immutable configuration snapshots, embedded English resources, and generated release packages.

## Runtime architecture

```text
Code
├── canonical global defaults
├── stable SkillId values
├── typed options and metadata for 142 skills
├── explicit SkillRegistry / SkillDispatcher hooks
└── embedded English translations

heroshift.json
└── server-specific overrides only

languages/<language>.json
└── optional translations or translation overrides
```

The runtime does not use reflection-based skill dispatch, `SkillsInfo`, nested `SkillConfig` classes, `config.json`, or `skillsInfo.json`.

## Requirements

- A CS2 dedicated server
- Metamod
- CounterStrikeSharp
- .NET 10-compatible CounterStrikeSharp runtime
- Network access while generating a release, or a local copy of the pinned RayTrace archives

The generated release bundles the pinned official RayTrace managed and Linux Metamod distributions. Runtime paths include:

```text
addons/counterstrikesharp/shared/RayTraceApi/RayTraceApi.dll
addons/counterstrikesharp/plugins/RayTraceImpl/RayTraceImpl.dll
addons/metamod/RayTrace.vdf
addons/RayTrace/gamedata.json
addons/RayTrace/bin/linuxsteamrt64/RayTrace.so
```

HeroShift preserves the capability key `raytrace:craytraceinterface` and the existing native trace behavior. Skills that require RayTrace degrade safely when the capability is unavailable.

## Installation

Download the generated `HeroShift-vX.Y.Z.zip` release archive and extract it into the CS2 game directory:

```text
game/csgo/
```

The archive contains the generated HeroShift files and the complete pinned RayTrace runtime tree. Core paths are:

```text
addons/counterstrikesharp/gamedata/HeroShift.gamedata.json
addons/counterstrikesharp/plugins/HeroShift/HeroShift.dll
addons/counterstrikesharp/plugins/HeroShift/Newtonsoft.Json.dll
addons/counterstrikesharp/plugins/HeroShift/WASDMenuAPI.dll
addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json
addons/counterstrikesharp/plugins/RayTraceImpl/
addons/counterstrikesharp/shared/RayTraceApi/
addons/metamod/RayTrace.vdf
addons/RayTrace/
package-manifest.json
THIRD_PARTY_NOTICES.md
licenses/RayTrace-GPL-3.0.txt
```

`package-manifest.json` records the size and SHA-256 hash of every packaged runtime file and the exact version and archive hashes of the bundled RayTrace release. PDB and XML documentation files are excluded.

For Docker deployments, persist the configuration and optional language files as volumes. Example:

```text
Host:      ./server-config/HeroShift/heroshift.json
Container: /server/game/csgo/addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json
```

Adjust the container prefix to match the server image. HeroShift resolves runtime files from its module directory rather than the process working directory.

## Configuration

HeroShift reads one override file relative to the CS2 game directory:

```text
addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json
```

The minimal valid file is:

```json
{
  "schemaVersion": 1
}
```

Code defaults apply to every omitted value. A server should only define values it intends to override.

Example:

```json
{
  "schemaVersion": 1,
  "general": {
    "gameMode": "NoRepeat",
    "enableBotSkills": true,
    "language": "en",
    "skillTimeBeforeStart": 7.0
  },
  "commands": {
    "reloadCommand": {
      "aliases": ["reload", "refresh"],
      "permission": "@HeroShift/admin"
    }
  },
  "skills": {
    "dash": {
      "enabled": true,
      "color": "#ff0000",
      "options": {
        "cooldown": 2.5
      }
    }
  }
}
```

Configuration is fully validated before it is published. Unknown sections, fields, skill IDs, option names, invalid types, duplicate aliases, and invalid ranges are rejected with their JSON path. An invalid hot reload leaves the previous valid immutable snapshot active.

The legacy `config.json` and `skillsInfo.json` formats are intentionally unsupported. Migrate server-specific values into `heroshift.json` and omit values that should use canonical defaults.

## Translations

English is embedded in `HeroShift.dll`; an external English file is not required. Optional language files are loaded from:

```text
addons/counterstrikesharp/plugins/HeroShift/languages/<language>.json
```

Lookup order is:

```text
selected external language → embedded English → translation key
```

External placeholder sets are validated against embedded English.

## Commands

Use `css_useSkill` to activate the current skill. A typical client bind is:

```text
bind x css_useSkill
```

The plugin retains the existing command aliases and permissions. Important defaults include:

| Command | Purpose | Default permission |
| --- | --- | --- |
| `!skills` | Show the available skills | `@HeroShift/admin` |
| `!setskill` | Assign a skill | `@HeroShift/admin` |
| `!setstaticskill` | Assign a persistent skill | `@HeroShift/admin` |
| `!reload` / `!refresh` | Atomically reload configuration and translations | `@HeroShift/admin` |
| `!hud` | Toggle the HUD | Public |
| `!console` | Execute a server command | `@HeroShift/owner` |

Aliases and permissions can be overridden through the typed `commands` section in `heroshift.json`.

## Development

```powershell
dotnet restore "HeroShift - SRC Files/HeroShift.sln"
dotnet test "HeroShift - SRC Files/HeroShift.sln" -c Release --no-restore
dotnet build "HeroShift - SRC Files/HeroShift.sln" -c Release --no-restore
./scripts/package.ps1 -Configuration Release -Version dev -NoBuild
```

The packaging script downloads RayTrace release `build-f483aba` from its official release server and verifies both archives before extraction. For offline packaging, place the two archives named in `scripts/package.ps1` in one directory and pass `-RayTraceAssetsDirectory <path>`.

The validation workflow runs tests, Debug and Release builds, architecture scans, package-inventory validation, and deterministic ZIP generation on Windows and Linux.

## Releases

Generate a local candidate without modifying Git history:

```powershell
./release.ps1 -Version 1.2.3
```

The script does not edit source files, commit, tag, or push. After reviewing the generated archive, create and push a `vX.Y.Z` tag. The Release workflow rebuilds, tests, packages, uploads the workflow artifact, and publishes the GitHub Release from that tag.

## Live-server validation

Before merging or promoting a production release, validate the generated ZIP on a live CS2 server:

- cold plugin load and unload
- hot reload and invalid-config rollback
- map changes and round lifecycle
- commands, permissions, voting, and HUD
- WASDMenu navigation and selection
- representative RayTrace skills
- bots and human bot takeover
- disconnect and reconnect cleanup
- representative skills from every hook family

Live-server validation is intentionally separate from the repository CI matrix.
