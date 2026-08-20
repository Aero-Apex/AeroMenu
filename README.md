<div align="center">

# Aero Menu - Among Us

**Among Us mod menu with local visual, host, lobby and moderation tools.**

<p>
  <img src="https://img.shields.io/badge/Among%20Us-Mod%20Menu-4b5563?style=flat-square" alt="Among Us Mod Menu">
  <img src="https://img.shields.io/badge/IL2CPP-BepInEx-374151?style=flat-square" alt="BepInEx IL2CPP">
  <img src="https://img.shields.io/badge/C%23-.NET%206-512BD4?style=flat-square&logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/Version-v1-a855f7?style=flat-square" alt="Version v1">
  <img src="https://img.shields.io/github/downloads/Aero-Apex/AeroMenu/total?style=flat-square&label=Downloads&color=2563eb" alt="Downloads">
</p>

<p>
  <a href="https://github.com/Aero-Apex/AeroMenu/releases/latest">
    <img src="https://img.shields.io/badge/Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github&logoColor=white" alt="Download">
  </a>
</p>

</div>

## About

Aero Menu is a BepInEx IL2CPP mod menu for Among Us.

It uses a simple IMGUI interface and includes local visual tools, player utilities, lobby controls, host features, cosmetics, moderation tools and other experimental options.

Some features are completely local, while others interact with the current lobby and may require host permissions.

## Features

| Category | Features |
| :--- | :--- |
| **Visuals** | ESP, roles, ghosts, vents, tracers, boxes, Full Bright, freecam, zoom, meeting information |
| **Player** | Player info, teleport, follow, morph, revive, kill and other player actions |
| **Movement** | Speed controls, no-clip and cursor teleport |
| **Outfits** | Random Outfit, favorite outfits, copy outfit, free colors and cosmetic controls |
| **Host** | Lobby management, role manager, task settings, forced roles, start/end controls |
| **Anti-Cheat** | RPC checks, flood protection, bot detection, identity checks, kick/ban actions |
| **Chat** | Extended chat, history, clipboard, whispers, filters, colors and logging |
| **Sabotages** | Trigger/repair sabotages, vents, doors and room controls |
| **Menu** | Themes, backgrounds, RGB accents, notifications, profiles, keybinds and scaling |

## Local vs lobby actions

Not every feature works the same way.

| Type | Effect |
| :--- | :--- |
| **Local** | Visible only on your client |
| **Profile** | Changes local Aero Menu or Among Us settings |
| **RPC** | Sends an action through the game and may be visible to other players |
| **Host** | Requires lobby host and may affect other players |

Available actions also depend on whether you are in the lobby, match, meeting or post-game screen.

## Installation

### 1. Install BepInEx IL2CPP

Download BepInEx:

- [Stable releases](https://github.com/BepInEx/BepInEx/releases)
- [Bleeding Edge](https://builds.bepinex.dev/projects/bepinex_be)

Use the IL2CPP version matching your game architecture.

### 2. Install BepInEx

Extract it into the Among Us directory containing:

```text
Among Us.exe
GameAssembly.dll
```

Example:

```text
Among Us/
├─ Among Us.exe
├─ GameAssembly.dll
├─ winhttp.dll
├─ dotnet/
└─ BepInEx/
```

Launch the game once and close it after reaching the main menu.

### 3. Install Aero Menu

Download `AeroMenu.dll` from:

[Latest Release](https://github.com/Aero-Apex/AeroMenu/releases/latest)

Place it here:

```text
Among Us/BepInEx/plugins/AeroMenu.dll
```

### 4. Open the menu

Launch Among Us and press:

```text
Insert
```

On some keyboards:

```text
Fn + Insert
```

The key can be changed later in the menu.

## Linux / Steam Deck

Install BepInEx and Aero Menu normally, then add this to Steam launch options:

```text
WINEDLLOVERRIDES="winhttp.dll=n,b" %command%
```

## Updating

Replace the old:

```text
BepInEx/plugins/AeroMenu.dll
```

with the DLL from the newest release.

Your Aero Menu configuration is stored separately and normally remains unchanged.

## Config files

```text
Among Us/AeroMenu/AeroMenu.cfg
Among Us/AeroMenu/AeroMenuBanList.txt
Among Us/AeroMenu/AeroBotBanList.txt
Among Us/AeroMenu/AeroPlatformBanList.txt
Among Us/AeroMenu/AeroFriendEspIgnore.txt
Among Us/AeroMenu/AeroPlayerHistory.txt
Among Us/AeroMenu/AeroWhiteList.txt
Among Us/AeroMenu/ChatLog.txt
```

## Cosmetics

Aero Menu includes local cosmetic options such as:

- Unlock All except Cosmicubes
- Unlock Cosmicubes
- Activate completed Cosmicubes
- Random Outfit
- Free Color
- Favorite outfits

These options do **not** give permanent server-side purchases, currency or account ownership.

## Screenshots

<details>
<summary><strong>Show screenshots</strong></summary>

### Menu customization

<img width="960" alt="Aero Menu customization" src="docs/screenshots/menu-customization.png" />

### Visuals / ESP

<img width="960" alt="Aero Menu visuals and ESP" src="docs/screenshots/visuals-esp.png" />

</details>

## Troubleshooting

<details>
<summary><strong>Menu does not open</strong></summary>

- Make sure you installed **BepInEx IL2CPP**, not Mono.
- Check that `AeroMenu.dll` is directly inside `BepInEx/plugins/`.
- Check the BepInEx console/log for errors.
- Try `Insert` or `Fn + Insert`.
- Make sure another overlay is not using the same key.

</details>

<details>
<summary><strong>Game stopped working after an update</strong></summary>

Among Us updates can break BepInEx interop or change game APIs.

Check the newest Aero Menu release and update BepInEx if necessary.

</details>

<details>
<summary><strong>Host feature is unavailable</strong></summary>

Make sure you are the current lobby host.

Some features are available only during a specific game state.

</details>

## Bug reports

Report bugs here:

[GitHub Issues](https://github.com/Aero-Apex/AeroMenu/issues)

## Build

The project targets **.NET 6**.

```powershell
dotnet build .\AeroMenu.slnx -c Release
```

Local Among Us and BepInEx assemblies are resolved through `AmongUsDir`.

## Compatibility

| Platform                             |           Status          |
| :----------------------------------- | :-----------------------: |
| Steam  / itch.io                     |        ✅ Supported        |
| Epic Games/xbox pc/Microsoft store   |        ✅ Supported        |
| Linux / Steam Deck (Proton)          |        ✅ Supported        |
| Cracked versions                     |        ✅ Supported        |
| iOS / iPadOS / Android               |      ❌ Not supported      |
| PlayStation / Xbox / Nintendo Switch |      ❌ Not supported      |

## Usage

Please use Aero Menu responsibly.

* Use Aero Menu only in private lobbies with players who are aware that mods are being used.
* Some features are local, while others may affect the current lobby or require host permissions.
* If you accidentally join a public lobby, disable or unload the mod before continuing to play.

## Disclaimer

Aero Menu is an unofficial third-party modification for Among Us.

The project is not affiliated with, endorsed by, sponsored by or approved by Innersloth LLC. Among Us and its related trademarks and assets belong to their respective owners.

Aero Menu is intended for use in **private lobbies only**.

The software is provided **as-is**, without any warranty.

By installing or using Aero Menu, you accept full responsibility for how you use it and for any consequences that may result, including account restrictions, bans, kicks, crashes, lost progress, corrupted files, game instability or incompatibility with future updates.

The developer is **not responsible for any consequences, damage or misuse resulting from the use of this software**.

Support is not provided for harassment, disruption of public games, unauthorized access, moderation evasion or other malicious activity.

## License

Licensed under the [GNU General Public License v3.0](LICENSE).

## Inspiration

- [EHR](https://github.com/Gurge44/EndlessHostRoles) - [Gurge44](https://github.com/Gurge44)
- [MalumMenu](https://github.com/scp222thj/MalumMenu) - [scp222thj](https://github.com/scp222thj)
- [SickoMenu](https://github.com/g0aty/SickoMenu) - [g0aty](https://github.com/g0aty)
- [AUM](https://github.com/BitCrackers/AmongUsMenu) - [BitCrackers](https://github.com/BitCrackers)