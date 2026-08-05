# HeroShift

HeroShift is a Counter Strike 2 plugin for CounterStrikeSharp that assigns each player one of 142 built in skills. The runtime uses stable skill identifiers, typed options, explicit hook registration, immutable configuration snapshots, embedded English resources, and verified release packages.

## Requirements

* A Counter Strike 2 dedicated server
* Metamod
* CounterStrikeSharp
* A .NET 10 compatible CounterStrikeSharp runtime
* PowerShell 7, Git, and GitHub CLI for publishing releases

The release archive includes the pinned RayTrace managed and Linux Metamod runtime.

## Project structure

```text
HeroShift.sln
src/HeroShift
src/WASDMenuAPI
tests/HeroShift.Tests
config/heroshift.json
docs/dispatch-semantics.md
release.ps1
```

The repository no longer keeps copied server binaries, packaging staging folders, migration tools, or separate packaging scripts.

## Build and test

```powershell
dotnet restore HeroShift.sln
dotnet test HeroShift.sln -c Release --no-restore
dotnet build HeroShift.sln -c Release --no-restore
```

## Installation

Download the generated release archive and extract it into the Counter Strike 2 game directory, normally `game/csgo`.

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

## Configuration

The deployed plugin reads `addons/counterstrikesharp/plugins/HeroShift/configs/heroshift.json`. The source controlled default is `config/heroshift.json`.

The minimal valid configuration is shown below.

```json
{
  "schemaVersion": 1
}
```

Code defaults apply to omitted values. Unknown fields, invalid skill identifiers, invalid option names, duplicate aliases, and invalid ranges are rejected before a new configuration snapshot is published.

English is embedded in the plugin. Optional language files can be placed at `addons/counterstrikesharp/plugins/HeroShift/languages/<language>.json`.

## Releases

Authenticate GitHub CLI, ensure local main matches remote main, then run the root release script.

```powershell
gh auth login
./release.ps1 -Version 1.2.3
```

The script restores, builds, tests, creates `HeroShift-v1.2.3.zip` in the repository root, creates and pushes the matching Git tag, and publishes the GitHub release with the archive attached. Temporary staging stays outside the repository and is removed after packaging.

Use `-NoPublish` to create and validate the root archive without creating a tag or GitHub release. Use `-RayTraceAssetsDirectory` to provide the two pinned RayTrace archives for offline packaging.

## Runtime behavior

The runtime does not use reflection based skill dispatch, `SkillsInfo`, nested legacy `SkillConfig` classes, `config.json`, or `skillsInfo.json`. Dispatch ordering and Boolean hook behavior are documented in `docs/dispatch-semantics.md` and protected by tests.
