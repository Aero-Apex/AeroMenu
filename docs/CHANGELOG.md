# Changelog

## v1.0.9 — 2026-11-24

### Fixed

- Host role forcing now broadcasts the final role to every client; the old path only assigned locally, so other players kept their original roles.
- In-game role changes apply locally and over RPC; the Roles tab "Set" button now uses the same path instead of changing only the host's local role.
- SelectRoles failures are logged instead of silently falling back to vanilla assignment.
- Task speed no longer accelerates the current minigame twice per tick.
- Auto-ban records a newly seen friend code or product user ID when the other identifier is already banned.
- AllowImpostor is restored to its original value when tasks-as-impostor is off, instead of staying true forever.

## v1.0.8 — 2026-11-24

### Fixed

- Auto-updater no longer silently swallows a failed startup install: the status and log now say whether the staged DLL was installed or needs Restart & Update.
- "Restart & Update" is now one click: if nothing is staged it downloads first, then restarts automatically once the DLL is ready.
- A manual check no longer downgrades a local build that is newer than the published release.
- Restart helper gets a longer hand-off window before the game is killed, avoiding races on slower machines.
- Keep-whisper target is pinned by product user ID, so a reconnect cannot route private messages to a different player who reused the slot.
- Whisper commands typed in portable chat are no longer broadcast to the whole lobby; they use the private send path and echo locally.

## v1.0.7 — 2026-11-24

### Fixed

- Host chat moderation no longer flags the ordinary word "voffset"; only the `<voffset` rich-text tag is dangerous.
- Friend-code validation accepts `_` and `-` in the name segment, stopping false "Broken FriendCode" host bans.
- Auto-skip-vote now reads the real `VotedForId`/`DidVote` fields, so it actually skips.
- Revive/Revive All preserve impostor-aligned roles (Shapeshifter, Phantom, Viper) and no longer misclassify Guardian Angel.
- SnapTo teleports now send the same sequence ID the local transform committed, removing client desync.
- Typing in portable chat or the custom Fake RPC field no longer fires keyboard binds.
- Lobby cards now render room code, platform, capacity and age in the info line.
- "Game preset" toggle is now required before Apply Preset runs.
- Random Nickname is regenerated on config load instead of coming back inert.
- Always Red is re-applied if another color change wins the race.
- Loading a config actively undoes previously applied display tweaks hide-HUD/no-vignette/FOV.
- ESP ignore list now hides name tags as well as tracers.
- Active tab/button text picks black or white from accent luminance, so dark accents stay readable.
- Status bar draws with a neutral GUI.color and restores the previous value.
- Local-outfit RPC trust window now only covers the cosmetic RPCs actually sent, instead of bypassing validation for every RPC from that player.
- Immortality vent state can be exited again; the guard no longer blocks the exit operation.

## v1.0.6 — 2026-11-23

### Added

- Restart & Update button in MENU / General / AUTO UPDATE: stages the update, saves settings, restarts the game, applies the DLL and relaunches automatically.
- Auto-updater retries failed checks, reports status/errors in the panel and writes [UPDATE] lines to BepInEx/LogOutput.log.

### Changed

- Rejoin Until Impostor waits in the lobby until the match starts and the role screen appears instead of leaving on a timer; late-join fallback added.
- Rejoin hopper retries a lobby twice before moving on, accepts 9+ player rooms (fullest first), queues up to 8 candidates.

### Fixed

- Vanilla RPC IDs 66/67 were falsely reported as mod RPCs.
- RPC sniffer ignores your own client and whitelisted players, and labels modded RPCs as [MOD RPC].
- Fixed the double-v in the updater restart status.

## v1.0.5 — 2026-11-23

### Added

- **Rejoin Until Impostor** (Self → Roles → Role Tools). Start it in a lobby: it copies that lobby's settings (max players, impostors, mode, map), then uses the Find Game list to join nearly full public lobbies with the same settings (at least max − 2 players by default, e.g. 13+ for a 15-player lobby). If your role isn't impostor when the match starts, it leaves and joins a different lobby; lobbies that don't start in time are skipped. Options: plain Impostor only, same map only, min players, lobby wait, max lobbies, leave delay. Your Find Game impostor/map filters are set to the target while it searches and restored afterwards (also after a restart).

### Fixed

- Scrolling the menu no longer drops FPS: sections scrolled out of view are skipped, and extra scroll events in a frame are merged. Measured menu cost went from ~6.8 ms to ~1.7–2.2 ms per frame.
- Guarded `SpiritGuideRole.FixedUpdate`, which could throw a NullReferenceException every physics tick (thousands per match) and spiral the frame rate down.
- Removed a startup warning from Il2CppInterop about an unsupported method parameter.

## v1.0.4 — 2026-11-22

### Added

- Auto-updater (MENU / General tab): checks GitHub releases on launch, downloads the newest `AeroMenu.dll` in the background and installs it on the next game start (with a backup of the previous DLL).
- In-game screenshots in the README.

### Fixed

- Fixed the sidebar/content overlap: `GUILayout.BeginArea(Rect)` ignores the rect position in this IL2CPP build, so the menu body is now pinned with `GUI.BeginGroup` and the sidebar uses fixed rects.
- Fixed 45 EXTRAS features never showing: `GUILayout.TextField`/`TextArea` throw "Method unstripping failed" in this build, which aborted the EXTRAS tab after section C. All 15 text boxes now use a built-in replacement text field (typing, backspace, Ctrl/Cmd+C/V/X, Enter/Esc), and each EXTRAS section is isolated so one failure can no longer hide the rest.
- Fixed the close button: it was drawn on the wrong side on some frames and showed an empty square (the game font has no `✕` glyph).
- Fixed EXTRAS and PRO cards running past the right edge of the menu.
- Fixed sliders with no visible track (crosshair size, chat font, graphics, minimap zoom, push/pull, spin, scale, color ID).
- Fixed the "Force Vote" toggle running off the Visuals tab, truncated labels in Search, the content area hiding under the status bar, and overlapping overlay panels / list overflow labels.
- Fixed text: "vv1.0.4" on the Welcome page, the "sendQUICK CHAT1" button label, Russian-only Anti-Cheat mode descriptions and the "нет" RPC label in Player History.

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
