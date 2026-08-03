# HeroShift

A CounterStrike 2 plugin that hands every player a random skill each round — from invisibility and explosive shots to camera manipulation and full-blown chaos.

## Contents

- [About](#-about)
- [Project Structure](#-project-structure)
- [Current Skills](#-current-skills-141)
- [Installation](#-installation)
- [Server Commands](#-server-commands)
- [Configuration](#-configuration)

## 💡 About

HeroShift brings chaos and fun to CS2 gameplay. Each round, every player is randomly assigned one of over a hundred unique skills, ranging from invisibility and explosive shots to camera manipulation and full map control. Surprise your opponents, seize control of the match, and discover dozens of creative abilities that reshape the way the game is played.

## 📁 Project Structure

```
cs2-hero-shift/
├── HeroShift - SRC Files/          # Plugin source code (C# / CounterStrikeSharp)
│   └── src/
│       ├── HeroShift.cs            # Main plugin entry point (BasePlugin, lifecycle hooks)
│       ├── command/                # Chat/console commands + skill voting system
│       ├── gamedata/               # HeroShift.gamedata.json (CS2 signatures/offsets)
│       ├── lang/                   # en.json localization source strings
│       ├── menu/                   # Builds the in-game WASD skill-selection menu
│       ├── player/                 # Per-player logic: events, bot handling, perf/debug logging
│       │   └── skills/             # One class per skill (Aimbot.cs, Wallhack.cs, GodMode.cs, ...)
│       └── utils/                  # Config loader, ray-tracing, entity/player managers, rarity system
├── HeroShift - Server Files/        # Release payload — drop straight into a CS2 game server
│   ├── gamedata/
│   └── plugins/HeroShift/
│       ├── HeroShift.dll / WASDMenuAPI.dll / Newtonsoft.Json.dll
│       ├── configs/
│       │   ├── config.json         # Runtime settings (game mode, HUD, permissions, ...)
│       │   └── skillsInfo.json     # Per-skill metadata (rarity, color, active flag, ...)
│       └── languages/
│           └── en.json
├── WASDMenuAPI - SRC Files/          # Standalone WASD-style in-game menu library (HeroShift dependency)
│   ├── Classes/                    # WasdManager, WasdMenu, WasdMenuOption, WasdMenuPlayer
│   └── Interfaces/                 # IWasdMenu, IWasdMenuManager, IWasdMenuOption contracts
├── .github/workflows/release.yml    # CI: builds and publishes the GitHub Release zip
├── release.ps1                      # Local release script (bump version, build, package, tag, push)
└── README.md
```

`HeroShift - Server Files/` **is** the release archive — both `release.ps1` and the CI workflow zip that directory as-is, so anything placed there ships in every release.

## ✨ Current Skills (141)

<details>
<summary>The table below lists all available skills in the game, along with their descriptions.</summary>

| Name               | Description                                                                                         | Cooldown / Range |
| ------------------ | ---------------------------------------------------------------------------------------------------- | ----------------- |
| Absorbing Man      | You have a random damage taken multiplier                                                            | (0.65 - 0.85)x    |
| Ant-Man            | Random character size at the start of the round                                                      | (60 - 95)%        |
| Anti-Venom         | Shooting teammates heals them                                                                         | -                 |
| Arcade             | Your HE grenade only explodes when there is an enemy nearby                                          | -                 |
| Armor              | Armor absorbs the first damage taken                                                                  | 15 s              |
| Arsenal            | You receive infinite ammo for all your weapons                                                       | -                 |
| Azazel             | You teleport behind the back of a hit enemy                                                          | -                 |
| Banshee            | Every now and then, you hear player screams                                                          | 2 s               |
| Basilisk           | You deal damage to every enemy you are looking at                                                    | 2 s               |
| Bishop             | Your opponent will receive a portion of the damage that they inflicted on you                        | -                 |
| Black Cat          | Choose the player who will lose all their money                                                      | -                 |
| Black Panther      | Perform a second jump to dash                                                                        | -                 |
| Black Widow        | Standing still increases your invisibility by 33%, crouching by 33%, and holding a knife by 33%       | -                 |
| Blade              | Click [css_useSkill] to throw a knife. But watch out for others                                      | -                 |
| Blink              | The first hit on an enemy sends them back to their spawn                                              | -                 |
| Bullseye           | Every bullet you hit counts as a headshot                                                             | -                 |
| Cable              | Random chance to fire an explosive bullet while shooting                                              | (15 - 30)%        |
| Captain America    | Your decoy bounces off walls and instantly kills an enemy on impact                                   | -                 |
| Carnage            | Killing restores ammo and a portion of health                                                         | -                 |
| Cloak              | Applies a darkness effect to a chosen enemy                                                           | -                 |
| Colossus           | You take no damage from headshots                                                                    | -                 |
| Crossbones         | You can plant the bomb anywhere, with a detonation time of 60 seconds                                | -                 |
| Cyclops            | Firing while airborne pushes you backwards                                                            | -                 |
| Dagger             | Click [css_useSkill] to turn the flashlight on or off. Its light can blind enemies                    | 2 s               |
| Daredevil          | You can see enemies through walls                                                                     | -                 |
| Dazzler            | You are immune to flashbangs, and your flashbangs last 7 seconds                                      | -                 |
| Deadpool           | Click [css_useSkill] to reload the weapon you are currently holding                                   | -                 |
| Death              | Hitting an enemy instantly kills them                                                                 | -                 |
| Deathlok           | All bullets are fired very quickly                                                                    | -                 |
| Doctor Strange     | Click [css_useSkill] to rewind a few seconds back in time                                             | 15 s              |
| Domino             | Select a skill from the list provided                                                                 | -                 |
| Dust               | Your smoke grenades never run out                                                                     | -                 |
| Echo               | Choose a player to mute all sounds for                                                                | -                 |
| Elektra            | You deal increased damage to enemies from behind                                                      | -                 |
| Elixir             | Your smoke grenades heal                                                                              | -                 |
| Explodey Boy       | You explode upon death, killing nearby players                                                        | -                 |
| Eye-Boy            | Click [css_useSkill] to activate third-person view                                                    | 0 s               |
| Falcon             | Enemies are visible on the radar                                                                       | -                 |
| Fantomex           | Click [css_useSkill] to take the active weapon's magazine from a random enemy                         | -                 |
| Firestar           | As long as you are alive, the bomb deals damage to its carrier                                        | -                 |
| Fixer              | You can plant and defuse bombs faster                                                                 | -                 |
| Forge              | Choose a player who cannot use rifles                                                                 | -                 |
| Gambit             | You have infinite HE grenades                                                                         | -                 |
| Ghost              | You are invisible while holding the bomb                                                              | -                 |
| Ghost Rider        | Select a player who takes damage for every missed shot                                                | -                 |
| Grandmaster        | Click [css_useSkill] to spectate a random enemy                                                       | 0 s               |
| Graviton           | Your decoy attracts nearby players towards itself                                                     | -                 |
| Gravity            | You receive a random gravity value at the start of the round                                          | (0.1 - 0.7)x      |
| Green Goblin       | Your HE grenades deal double damage and have double range                                             | -                 |
| Groot              | Click [css_useSkill] to create a destructible barricade                                               | 2 s               |
| Hawkeye            | Click [css_useSkill] to swap your current weapon for an AWP                                           | 0 s               |
| Hazmat             | Your smoke grenades deal damage                                                                       | -                 |
| Heimdall           | You can see through smoke grenades                                                                    | -                 |
| Hela               | A primary knife attack deals damage regardless of distance                                            | -                 |
| Howard the Duck    | You get a chicken model + 10% faster movement - 50 HP                                                 | -                 |
| Hulk               | You have a random chance to launch an enemy upwards                                                   | (20 - 40)%        |
| Human Torch        | Throw a decoy to call down a rain of Molotovs                                                          | -                 |
| Iceman             | Your decoy freezes all nearby players                                                                 | -                 |
| Invisible Woman    | You are completely invisible                                                                          | -                 |
| Iron Man           | Fly for a limited time. Hold [USE - E] to fly                                                         | -                 |
| Jack of Hearts     | Choose a player to swap health with                                                                   | -                 |
| Jean Grey          | Choose a player who will have trouble throwing grenades                                               | -                 |
| Joaquin Torres     | Your chickens heal you while you are nearby                                                            | 1 s = 5 HP        |
| Jubilee            | Anyone fully blinded by your flashbang dies (including you)                                            | -                 |
| Juggernaut         | You have a random chance to push an enemy back when hitting them                                      | 100%              |
| Justin Hammer      | A chosen enemy has to pay for every shot                                                               | -                 |
| Kang the Conqueror | Every grenade throw alters the round time                                                             | -                 |
| Kingpin            | Choose a player to swap money with                                                                    | -                 |
| Kraven the Hunter  | Choose a player who will leave a trail behind them                                                    | -                 |
| Lockjaw            | Click [css_useSkill] to return to spawn                                                               | 15 s              |
| Loki               | Click [css_useSkill] to swap places with a random enemy                                               | 30 s              |
| M.O.D.O.K.         | You only take damage to the head                                                                     | -                 |
| Magik              | Press [css_useSkill] to teleport to the teammate you're looking at.                                   | 15 s              |
| Magma              | Molotov restores health                                                                                | -                 |
| Magneto            | You have a random chance to make an enemy drop their weapon on hit                                    | (20 - 35)%        |
| Mastermind         | Forces the enemy's screen to zoom in, reducing their field of view                                    | -                 |
| Mister Immortal    | After death, you respawn with the same amount of health                                               | -                 |
| Mister Sinister    | Click [css_useSkill] to create a replica that deals damage on hit                                     | 15 s              |
| Mockingbird        | Click [css_useSkill] to string a wire between two walls. Enemies who touch it appear on your radar    | 20 s              |
| Moon Knight        | In jester mode, you cannot get or take any damage. This mode changes every few seconds                | (10 - 25) s       |
| Moonstone          | Your grenades are not affected by gravity and fly faster                                              | -                 |
| Morbius            | Hitting an enemy restores health equal to a percentage of the damage dealt                             | -                 |
| Morph              | Choose a player to copy their skill                                                                   | -                 |
| Multiple Man       | Click [css_useSkill] to deploy a replica that walks straight ahead                                    | 30 s              |
| Mysterio           | Click [css_useSkill] to control your hologram for a few seconds                                       | 30 s              |
| Mystique           | You start the round with an enemy player model                                                        | -                 |
| Nebula             | Arms and legs are bulletproof                                                                          | -                 |
| Nick Fury          | Click [css_useSkill] to create/switch to a camera                                                     | 30 s              |
| Night Nurse        | Click [css_useSkill] to use a healing charge that restores 50 health                                  | 1 s               |
| Nightcrawler       | Click [css_useSkill] to teleport to the enemy spawn                                                   | 15 s              |
| Nightmare          | Force a chosen enemy to experience a terrifying vision                                                | -                 |
| Nitro              | Shooting the bomb damages it                                                                          | -                 |
| Penance            | After you die, you deal damage to the enemy who killed you                                            | 30 HP             |
| Phoenix            | You have a random chance to respawn after death                                                       | (20 - 40)%        |
| Polaris            | All enemy grenades are repelled away from you                                                         | -                 |
| Professor X        | When you are near the bomb, you start defusing it                                                     | 10 s              |
| Prowler            | Dealing damage to an enemy steals their money                                                         | -                 |
| Punisher           | Press Attack2 with the MP5 to fire an HE grenade                                                      | 10 s              |
| Puppet Master      | A chosen enemy jumps whenever one of their teammates jumps                                            | -                 |
| Purple Man         | As long as you are alive, your enemies cannot read                                                    | -                 |
| Quicksilver        | Random player speed at the beginning of the round                                                     | (1.2 - 3.0)x      |
| Rangefinder        | You can see the distance to the nearest enemy                                                         | -                 |
| Red Hulk           | You receive a random amount of health at the start of the round                                       | +(50 - 501) HP    |
| Rhino              | Select a player to jetkick                                                                            | -                 |
| Rocket Raccoon     | Click [css_useSkill] to place a barrel that explodes when shot                                        | 20 s              |
| Rogue              | Choose a player whose skill you want to disable                                                       | -                 |
| Sabretooth         | You deal more damage and move faster as your health gets lower                                        | -                 |
| Sandman            | Choose a player who cannot jump                                                                       | -                 |
| Scarlet Witch      | You can choose a bomb site to deactivate                                                               | -                 |
| Scorpion           | Choose a player who will take damage every few seconds                                                | -                 |
| Sentry             | Click [css_useSkill] to become immortal for a short time                                              | 30 s              |
| Shadowcat          | Your footsteps and jumps are silent to other players                                                  | -                 |
| Shuri              | Choose a player to disable their crosshair                                                            | -                 |
| Silver Samurai     | While holding a knife, you have a high chance to deflect a shot                                       | -                 |
| Speed Demon        | The bomb explodes much faster                                                                         | -                 |
| Spider-Man         | Your grenades stick to walls                                                                          | -                 |
| Spot               | You swap places with the hit enemy                                                                    | -                 |
| Squirrel Girl      | You get an extra jump                                                                                 | -                 |
| Star-Lord          | Click [css_useSkill] to receive a random weapon                                                       | 15 s              |
| Storm              | Zeus x27 instantly recharges                                                                          | -                 |
| Sunspot            | You receive a random amount of money at the start of the round                                        | (5000 - 15000)$   |
| Super-Adaptoid     | You can steal a skill from a chosen player                                                            | -                 |
| Taskmaster         | No recoil while shooting                                                                               | -                 |
| Tempo              | Planting the bomb takes significantly longer                                                          | -                 |
| The Thing          | Grenades deal no damage to you                                                                        | -                 |
| The Watcher        | Click [css_useSkill] to activate a bird's-eye view camera                                              | -                 |
| Thor               | Zeus deals damage regardless of distance                                                              | -                 |
| Throg              | You get auto "BunnyHop"                                                                               | -                 |
| Tinkerer           | Click [css_useSkill] to swap weapons with a random enemy                                              | 30 s              |
| Toad               | Jumping restores health                                                                               | -                 |
| Trapster           | Your bullets significantly slow down players                                                          | -                 |
| U.S. Agent         | You have a random damage multiplier                                                                   | (1.15 - 1.35)x    |
| Ultron             | Disables the radar for a chosen enemy                                                                 | -                 |
| Vision             | Click [css_useSkill] to enable noclip for a short time                                                | 30 s              |
| War Machine        | Click [css_useSkill] to lock your aim on the nearest enemy                                             | 20 s              |
| Wasp               | Enlarge an enemy of your choice                                                                       | (110 - 140)%      |
| Whirlwind          | You have a random chance to turn an enemy 180° when hitting them                                      | (20 - 40)%        |
| Wolverine          | You restore health every few seconds                                                                  | -                 |
| X-23               | Instant kill with a knife                                                                             | -                 |
| Yondu              | Your grenades (except smokes) are attracted to enemies                                                | -                 |
| Zombie             | After death, you respawn as a zombie with increased health and no weapons                             | -                 |

</details>

## 💻 Installation

1. Install / buy a **CS2 server**.
   - Good tutorial on how to create your own CS2 server [[Video]](https://www.youtube.com/watch?v=1ZrEn0CiMi4&ab_channel=TroubleChute), [[Website]](https://hub.tcno.co/games/cs2/dedicated_server/).
2. Install **Metamod**.
   - Download [Metamod:Source 2.x](https://www.sourcemm.net/downloads.php/?branch=master)
   - Extract it to the `C2Server/game/csgo/` folder.
   - Edit the `gameinfo.gi` file by adding a new line

     ```json
         Game_LowViolence csgo_lv // Perfect World content override
         Game csgo/addons/metamod // <-- Line to add

         Game csgo
     ```

3. Install **CounterStrikeSharp**.
   - Download [CounterStrikeSharp-With-Runtime](https://github.com/roflmuffin/CounterStrikeSharp/releases).
   - Extract it to the `C2Server/game/csgo/` folder.
4. Install **Ray-Trace**
   - Download [RayTrace-CSS-API](https://github.com/FUNPLAY-pro-CS2/Ray-Trace/releases)
   - Extract folder `conterstrikesharp` to the `CS2Server/game/csgo/addons/` folder.
   - Download [RayTrace-MM](https://github.com/FUNPLAY-pro-CS2/Ray-Trace/releases)
   - Extract it to the `CS2Server/game/csgo/addons/` folder.
5. Install **HeroShift**
   - Download [HeroShift](https://github.com/arikitos/cs2-hero-shift/releases)

## 🖥️ Server Commands

> [!TIP]
> **Bind to use skills:** `bind x css_useSkill`

<details>
<summary>The table below lists all available commands in the game, along with their descriptions.</summary>

| Command                                        | Example                         | Description                                                                                            | Permissions        |
| ---------------------------------------------- | ------------------------------- | ------------------------------------------------------------------------------------------------------ | ------------------ |
| `!setskill <playerName/steamID> <skill>`       | `!setskill Juzlus Aimbot`       | Giving skill to a player                                                                               | `@HeroShift/admin` |
| `!skills`                                      | `!skills`                       | List of skills                                                                                         | -                  |
| `!map <mapName>`                               | `!map de_nuke`                  | Change map                                                                                             | `@HeroShift/admin` |
| `!map <mapWorkshopId>`                         | `!map 3332005394`               | Change map from workshop                                                                               | `@HeroShift/admin` |
| `!start`                                       | `!start`                        | Start game with conditions: `mp_forcecamera 0, mp_freezetime 15, mp_overtime_enable 1, sv_cheats 0`    | `@HeroShift/admin` |
| `!start sv`                                    | `!start sv`                     | Start the game with conditions: `mp_forcecamera 0, mp_freezetime 0, mp_overtime_enable 1, sv_cheats 1` | `@HeroShift/admin` |
| `!console <command>`                           | `!console sv_cheats 1`          | Run a command on the server                                                                            | `@HeroShift/owner` |
| `!swap`                                        | `!swap`                         | Switch sides                                                                                           | `@HeroShift/admin` |
| `!shuffle`                                     | `!shuffle`                      | Randomly assign players to teams                                                                       | `@HeroShift/admin` |
| `!pause`                                       | `!pause`                        | Pause the game                                                                                         | `@HeroShift/admin` |
| `!heal`                                        | `!heal`                         | Restore 100 health points                                                                              | `@HeroShift/admin` |
| `!hud`                                         | `!hud`                          | Enable/Disable HUD                                                                                     | -                  |
| `!entity <index/handle>`                       | `!entity 429`                   | Checking whether a given entity exists                                                                 | `@HeroShift/owner` |
| `!setscore <CT> <TT>`                          | `!setscore 10 7`                | Set the game score                                                                                     | `@HeroShift/owner` |
| `!setstaticskill <playerName/steamID> <skill>` | `!setstaticskill Juzlus Aimbot` | Giving a player a permanent skill                                                                      | `@HeroShift/admin` |
| `!setstaticskill <playerName/steamID> None`    | `!setstaticskill Juzlus None`   | Back to normal                                                                                         | `@HeroShift/admin` |
| `!botplace [slot] [godmode]`                   | `!botplace 2 1`                 | Teleport a bot to your location                                                                        | `@HeroShift/admin` |
| `!next_skill <name/steamID> [idx]`             | `!next_skill Juzlus`            | Switch skill for a player (next, previous -1, or specific idx)                                         | `@HeroShift/admin` |
| `!plantedbomb [time]`                          | `!plantedbomb 35`               | Spawn a planted C4 bomb at your position with custom or default (40s) detonation time                  | `@HeroShift/admin` |
| `!sethealth <amount>`                          | `!sethealth 150`                | Set a specific health amount for yourself                                                              | `@HeroShift/admin` |
| `!reload`                                      | `!reload`                       | Reload config, skill data, and translations                                                            | -                  |

_Most commands require permissions, which must be set in the file: `game/csgo/addons/counterstrikesharp/configs/admins.json`_

</details>

## ⚙️ Configuration

All skills can be customized in the **`config.json`** / **`skillsInfo.json`** files located in the **`game/csgo/addons/counterstrikesharp/plugins/HeroShift/configs/`** folder.

- ##### config.json

_Excerpt — see the file itself for the full option list._

```json
{
    "Settings": {
        "GameMode": 3,                   // Game mode:
                                         // 0 - Random skills for each player (It can't be the same twice in a row)
                                         // 1 - Same skills for the whole team
                                         // 2 - Same skills for all players
                                         // 3 - Random skills for each player (It can't be the same until the map changes)
                                         // 4 - Random skills for each player (Full random)
                                         // 5 - Debug: Skills are assigned in turn
        "YourSkillChatInfo": true,       // Show your skill in chat
        "KillerSkillChatInfo": true,     // Show killer's skill in chat
        "TeamMateSkillChatInfo": true,   // Show allies' skills in chat
        "SummaryAfterTheRound": true,    // Show summary of the last round
        "EnableBotSkills": true,         // Enable skills for bots
        "EnableBotKickDebug": false,     // Kick a random bot every 45s (for debug/testing)
        "DebugMode": false,              // Save debug logs (player events and plugin activity) to the Debug folder
        "PerfMode": false,               // Save performance measurements to the logs folder
        "AlternativeSkillButton": null,  // Possible buttons:
                                         // null, "Attack", "Jump", "Duck", "Forward", "Back",
                                         // "Use", "Cancel", "Left", "Right", "Moveleft",
                                         // "Moveright", "Attack2", "Run", "Reload", "Alt1",
                                         // "Alt2", "Speed", "Walk", "Zoom", "Weapon1",
                                         // "Weapon2", "Bullrush", "Grenade1", "Grenade2",
                                         // "Attack3", "Scoreboard", "Inspect"
        "SkillTimeBeforeStart": 7.0,     // How many seconds before freeze time ends should skills
                                         // drawing be completed? (freezetime - SkillTimeBeforeStart)
        "SkillHudDuration": -1.0,       // How long should the HUD be visible for?
        "SkillDescriptionDuration": 7.0, // How long should the skill description be visible for?
        "DisplayAlwaysDescription":false,// Always display skill description (SkillDescriptionDuration = 9999)
        "DisableSpectateHUD": false,     // Disable HTML HUD when spectating
        "HideHudForOtherPlugins": true,  // Automatically hides HUD when another plugin uses it
        "EnableFlashingHtmlHudFix": false,// Enable FlashingHtmlHudFix
        "TraceRayBeam": false,           // Enable trail visibility for 'Long Knife', 'Long Zeus'
        "DisableHUDOnDeathPermission": "@HeroShift/death",  // Disable the HUD after death for players with this permission
        "DisableSkillsOnRoundEnd": false,// Disable all skills at the end of the round (when the summary is visible)
        "CurseSkillPerPlayer": null,     // Maximum number of effects per player

        "HtmlHudCustomisation": {        // Settings for changing colours and font sizes
            ...                          // xxxl: 64px, xxl: 40px, xl: 32px
        }                                // l: 24px, ml: 20px, m: 18px
        ...                              // sm: 16px, s: 12px, xs: 8px
    },
```

- ##### skillsInfo.json

_Excerpt — one entry per skill; see the file itself for the full list._

```json
[
    {
        "NeedsTeammates": false,      // Requires other players on the team
        "DisableOnFreezeTime": false, // Disable the skill during freeze time
        "OnlyTeam": 0,                // Skill availability:
                                      // 0 - Everyone
                                      // 2 - Terrorist
                                      // 3 - CounterTerrorist
        "Color": "#ff0000",         // Skill color
        "Active": true,               // Enabled on startup
        "Name": "Aimbot",             // Skill name
        "HudDuration": null,          // Overrides the global SkillHudDuration for this skill.
                                      // null = use SkillHudDuration from config.json,
                                      // -1 = never hide the HUD,
                                      // >= 0 = display duration in seconds.
        "DescriptionHudDuration":null,// Overrides the global DescriptionHudDuration for this skill.
                                      // null = use DescriptionHudDuration from config.json,
                                      // -1 = never hide the description,
                                      // >= 0 = display duration in seconds.
        "RequiredPermission": "",     // Required permission
        "MaxPerServer": -1,           // Maximum number of players allowed to have
                                      // this skill on the server (-1 for unlimited)
        "Rarity": "Common"            // Rarity tier of the skill:
                                      // Common (70x), Uncommon (14x),
                                      // Rare (10x), Epic (5x), Legendary (1x)
    },
    ...
]
```
