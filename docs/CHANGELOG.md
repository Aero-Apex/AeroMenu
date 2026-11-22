# Changelog

## v1.0.3 — 2026-11-22

### Added

- Added 50 new PRO features embedded in the existing tabs (no new tabs): FPS limiter, VSync, quality/AA/resolution controls, combat/meeting/host tools, chat utilities, anti-abuse logging, movement/identity tools, automation, calculator, notes pad and more.
- Added platform-aware menu hotkeys: `Insert` on Windows/Linux, `Command` or `` ` `` on macOS (including Wine/Crosswire). Both keys are rebindable.
- Added the prebuilt DLL compiled against Among Us 2026.9.29 / BepInEx IL2CPP.

### Changed

- No breaking changes.

## v1.0.2 — 2026-11-22

### Added

- Added a prebuilt `AeroMenu.dll` (compiled against Among Us 2026.9.29 / BepInEx IL2CPP) in the repository root.
- Added 60 new optional features in **General → EXTRAS**, grouped into 12 categories (visual overlays, info panels, local tweaks, host tools, player tools, utility, panels, meeting, lobby, cosmetics, diagnostics, automation). Every option is saved independently.
- Added an EXTRAS hub that isolates each feature group so one failing group cannot break the menu or the game.
- Added `.github/workflows/build-dll.yml` and `docs/BUILDING.md` explaining how to compile `AeroMenu.dll` (the game's interop assemblies are not committed).

### Fixed

- "Auto-Ban Broken FriendCode (Host)" now respects its saved setting instead of always banning (the guard option was a permanently-null `readonly` field).
- Whitelist-only lobbies no longer kick players whose friend code is whitelisted but whose PUID is not.
- Lobby teleport detection now actually runs; it was completely dead code.
- Position sanity patch now normalizes the authoritative network sync fields, not just the rendered transform.
- Fixed malformed-packet "ban sender" targeting client 0 (the host) when the sender dictionary is empty.
- Fixed SetColor/SetName RPC validation reading a nonexistent 4-byte netId prefix.
- Vote-kick bans are de-duplicated when both anti-cheat paths fire.
- Auto-host now honors the configured load-wait grace period and force-start bypass.
- Fixed swapped Engineer/Scientist role option names in forced-role logic.
- Auto-two-impostors now restores the previous role-forcing state when disabled.
- Guardian Angel auto-protect now applies the picked target instead of whichever player is nearest.
- Auto-kill no longer wastes attempts on dead players.
- Replay trails are cleared on round/map change.
- Bots no longer reuse the local player's color.
- Chat history size 5-20 is no longer silently overwritten with 80.
- Hotkeys no longer fire while typing in the status bar or accent hex fields.
- Custom accent color is now re-applied after config load/profile load.
- Menu scale is now loaded and applied instead of being silently ignored.
- Menu toggle key can now actually be cleared.
- Clicking an already-active sub-tab no longer resets the scroll position.
- Platform-spoof punishment state is now cleared like the other anti-cheat states.

## v1.4.5 — 2026-07-29

### Changed

- Extended host bot bans to detect `bot` or `бот` anywhere in raw platform names without case sensitivity.
- Removed the `Hidden` Friend Code mode and its temporary-account and serialization patches.
- Renamed `Public Friend Code` to `Spoof Friend Code` in the identity menu and registration notifications.

## v1.4.4 — 2026-07-25

### Added

- Added a saved `Realistic Mode` for the radar with detailed layouts for The Skeld, Mira HQ, Polus, Airship, and Fungle while keeping the original radar available.

## v1.3.9 — 2026-06-22

### Added

- Added separate options for unlocking regular cosmetics, unlocking Cosmicubes, and locally activating 100% completed Cosmicubes.
- Added selected-player actions: copy PUID, copy Friend Code, and report a player with a selectable reason.
- Added `See Phantoms` and support for revealing active Guardian Angel protection effects.
- Added tracer filters for all players, crewmates, impostors, dead players/ghosts, and dead bodies. Body tracers are yellow.
- Added `Block Innersloth Telemetry`, `No Disconnect Penalty`, and automatic room-code copying on disconnect.
- Added a detailed Unity log rate control: `0` disables ordinary logs, `1–19` limits lines per second, and `∞` removes the limit.

### Changed

- Reorganized Visuals into Visibility, ESP, Tracers, and Other sections.
- Renamed `Show Tracers` to `All Tracers`.
- Reworked remote kill-cooldown tracking using in-game timer state.
- Kill-cooldown labels now use the menu accent color, colored timer thresholds, and burgundy `READY` status.
- `No Disconnect Penalty` is enabled by default and appears below the telemetry option with a description.
- Improved `Kill Anyone` selection for other impostor-team roles; hosts can confirm these kills directly.
- Made Door Lockdown controls responsive at narrow menu widths.
- Updated log monitoring to ignore the first 40 startup lines and detect warning bursts from 7 events with throttling.
- Staggered screen notifications by 0.3 seconds to prevent simultaneous notification bursts.
- Reduced player-history polling frequency and cached unchanged ESP name and information text.
- Moved expensive tracer visibility and dead-body scans to a 0.3-second interval while keeping tracer positions smooth each frame.
- Moved bot detection to a 0.5-second interval without delaying regular autobans.
- Changed `No Task Mode` to apply zero task settings once when enabled and once after changing lobbies instead of every frame.
- Pre-indexed autoban Friend Codes in a case-insensitive `HashSet`, preserving immediate bans without per-player `Split`, `Trim`, and lowercase conversions.
- Reused notification `GUIStyle` instances instead of allocating new styles during every `OnGUI` pass.
- Changed `Level Spoof` to apply only when enabled, when its value changes, and when entering a different lobby instead of every frame.

### Fixed

- Fixed random fatal CLR crashes caused by repeatedly reading and mutating IL2CPP FriendCode strings from per-frame GUI and local spoof updates.
- Fixed Door Lockdown rows overflowing because of long room names and the scrollbar.
- Fixed an IL2CPP `ObjectCollectedException` caused by `GUILayout.MaxWidth`, which also unbalanced Unity's GUIClip stack.
- Fixed kill cooldown incorrectly showing `READY` at round start.
- Fixed repeated null-reference errors in player visibility handling.
- Improved fully vanished Phantom and protection-effect visibility restoration.
- Fixed high FPS loss caused by unrestricted `RpcFlag`, task, role, and other verbose Unity console messages.

## v1.3.7

- Fixed known issues.
- Improved UI.
- Updated configuration options.
- Added new features and stability improvements.
