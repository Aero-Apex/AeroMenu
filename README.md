<div align="center">

<img src="docs/logo.png" width="180" alt="AeroMenu logo" />

# Aero Menu

**Among Us mod menu with local visual, host, lobby and moderation tools.**

<p>
  <img src="https://img.shields.io/badge/Among%20Us-Mod%20Menu-4b5563?style=flat-square" alt="Among Us Mod Menu">
  <img src="https://img.shields.io/badge/IL2CPP-BepInEx%206-374151?style=flat-square" alt="BepInEx IL2CPP">
  <img src="https://img.shields.io/badge/C%23-.NET%206-512BD4?style=flat-square&logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/Version-v1.0.1-a855f7?style=flat-square" alt="Version v1.0.1">
  <img src="https://img.shields.io/github/downloads/Aero-Apex/AeroMenu/total?style=flat-square&label=Downloads&color=2563eb" alt="Downloads">
</p>

<p>
  <a href="https://github.com/Aero-Apex/AeroMenu/releases/latest">
    <img src="https://img.shields.io/badge/⬇%20Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github&logoColor=white" alt="Download">
  </a>
</p>

---

[Features](#-features) · [Installation](#-installation) · [Customization](#-customization) · [Config](#-config-files) · [Build](#-build) · [Compatibility](#-compatibility) · [Troubleshooting](#-troubleshooting)

---

</div>

## 📖 About

Aero Menu is a BepInEx IL2CPP mod menu for Among Us.

It uses a custom IMGUI interface and includes local visual tools, player utilities, lobby controls, host features, cosmetics, moderation tools and other experimental options — all searchable from an in-game search tab and rebindable to any key.

Some features are completely local, while others interact with the current lobby and may require host permissions.

## ✨ Features

| Category | Features |
| :--- | :--- |
| **Visuals** | ESP, roles, ghosts, vents, tracers, boxes, Full Bright, freecam, zoom, meeting information |
| **Player** | Player info, teleport, follow, morph, revive, kill, clones with 11 formations, Flood Lobby, Bots |
| **Movement** | Speed controls, no-clip and cursor teleport |
| **Outfits** | Random Outfit, favorite outfits, copy outfit, free colors and cosmetic controls |
| **Host** | Lobby management, role manager, task settings, forced roles, start/end controls |
| **Anti-Cheat** | RPC checks, flood protection, bot detection, identity checks, kick/ban actions |
| **Chat** | Extended chat, history, clipboard, whispers, filters, colors, spam/flood and logging |
| **Sabotages** | Trigger/repair sabotages, vents, doors and room controls |
| **Menu** | Themes, backgrounds, RGB accents, notifications, profiles, keybinds, scaling and deep UI customization |

### 🔎 Search everything

Type any feature name in the built-in **SEARCH** tab to find it instantly. Filter by category (Visuals, ESP, Radar, Chat, Lobby, Sabotage, Combat, Info, Meeting, Menu) or by On/Off state. Button-only features appear too and jump straight to their tab.

### 💾 Saving & autosave

Your settings are safe:

- Saved on every change, on menu close, and on game exit (**Alt+F4** included)
- Emergency flush on process exit + a full autosave cycle every 30 seconds
- The menu's status bar flashes **Saved ✓** after every save

## 🚀 Installation

> **One-click:** download the release package and run **`Install-AeroMenu.bat`** — it auto-detects your Among Us install (Store / Steam / Epic) or asks for the path, installs BepInEx if missing, and drops in the mod.

<details>
<summary><strong>Manual installation</strong></summary>

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

Download `AeroMenu.dll` from the [Latest Release](https://github.com/Aero-Apex/AeroMenu/releases/latest) and place it here:

```text
Among Us/BepInEx/plugins/AeroMenu.dll
```

### 4. Open the menu

Launch Among Us and press **Insert** (or `Fn + Insert` on some keyboards). The key can be changed later in the menu.

</details>

## 🎨 Customization

Everything is customizable from the in-game **MENU** tab:

| Setting | Options |
| :--- | :--- |
| **Theme** | Light / dark, 82 accent presets, RGB rainbow mode, custom hex accent color |
| **Layout** | Window size & position, scale, corner roundness, sidebar / compact / micro layouts |
| **Look** | Custom backgrounds, menu character, opacity, font size offset, tab animations on/off |
| **Status bar** | Show/hide, custom left text, live "Saved ✓" indicator |
| **Behavior** | Default tab on open, autosave interval, notifications, watermark |
| **Keybinds** | Every action and toggle rebindable, including the menu toggle key |
| **Profiles** | 5 config profiles capturing your whole setup |

## 🗂️ Config files

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

## 🧩 Cosmetics

Aero Menu includes local cosmetic options such as:

- Unlock All except Cosmicubes
- Unlock Cosmicubes
- Activate completed Cosmicubes
- Random Outfit
- Free Color
- Favorite outfits

These options do **not** give permanent server-side purchases, currency or account ownership.

## 🐧 Linux / Steam Deck

Install BepInEx and Aero Menu normally, then add this to Steam launch options:

```text
WINEDLLOVERRIDES="winhttp.dll=n,b" %command%
```

## 🔄 Updating

Replace the old `BepInEx/plugins/AeroMenu.dll` with the DLL from the newest release. Your configuration is stored separately and remains unchanged.

## 🛠️ Build

The project targets **.NET 6**.

```powershell
dotnet build .\AeroMenu.slnx -c Release
```

Local Among Us and BepInEx assemblies are resolved through `AmongUsDir`.

## 🖥️ Compatibility

| Platform                             |           Status          |
| :----------------------------------- | :-----------------------: |
| Steam  / itch.io                     |        ✅ Supported        |
| Epic Games/xbox pc/Microsoft store   |        ✅ Supported        |
| Linux / Steam Deck (Proton)          |        ✅ Supported        |
| Cracked versions                     |        ✅ Supported        |
| iOS / iPadOS / Android               |      ❌ Not supported      |
| PlayStation / Xbox / Nintendo Switch |      ❌ Not supported      |

## ⚠️ Usage

Please use Aero Menu responsibly.

* Use Aero Menu only in private lobbies with players who are aware that mods are being used.
* Some features are local, while others may affect the current lobby or require host permissions.
* If you accidentally join a public lobby, disable or unload the mod before continuing to play.

## 📄 Disclaimer

Aero Menu is an unofficial third-party modification for Among Us.

The project is not affiliated with, endorsed by, sponsored by or approved by Innersloth LLC. Among Us and its related trademarks and assets belong to their respective owners.

Aero Menu is intended for use in **private lobbies only**.

The software is provided **as-is**, without any warranty.

By installing or using Aero Menu, you accept full responsibility for how you use it and for any consequences that may result, including account restrictions, bans, kicks, crashes, lost progress, corrupted files, game instability or incompatibility with future updates.

The developer is **not responsible for any consequences, damage or misuse resulting from the use of this software**.

Support is not provided for harassment, disruption of public games, unauthorized access, moderation evasion or other malicious activity.

## 📜 License

Licensed under the [GNU General Public License v3.0](LICENSE).
