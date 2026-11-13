# Changelog

## 1.1.0 — 2026-11-13

### Modes and hardware

- Refine the OSD as a compact frameless pill, with an accent icon badge, progress/confirmation indicator and a subtle outline. Remove the native gray frame and render the popup with per-pixel alpha and Direct2D/DirectWrite antialiasing, without a hard window-region clip.
- Show the requested mode immediately on Ctrl+Alt+F while the main window is hidden or minimized. The pill shows Applying until fresh telemetry confirms the change; timeout or failure does not claim success.
- Advance rapid presses from the requested mode, coalesce queued changes, and retry registration when another application releases the shortcut.
- Let older firmware without native operating modes cycle the existing safe software profiles. Unavailable firmware commands are not sent.
- Add a Graphics page for firmware-supported Hybrid, Discrete and Automatic GPU routing, confirmation/restart handling, detected adapters and Windows per-app GPU preferences. Hybrid is not integrated-only mode.
- Use capability detection for Nitro, Nitro V and Predator generations from 2021 onward. Universal hardware compatibility and MUX availability are not claimed; physical validation remains limited to AN515-58.

### Interface and library

- Add a game library: executable/shortcut entries, automatic Steam/Epic detection at startup with a toggle, manual scanning, search, favorites, recent launch history and bounded page rendering.
- Add application and service logs with size/retention limits, filters, refresh and export.
- Update page headers and cards outside Overview; retain the Overview layout and remove its card outlines.
- Default new settings to English while retaining a saved language choice.
- Bundle 38 official Acer model photographs (about 5 MB), match model codes including suffixes, label series illustrations when an exact photograph is unavailable, and add an offline photo selector in Settings. Retain optional online lookup/cache and manual images. Artwork coverage does not certify hardware support.

### Acer Crate identity and upgrade

- Rename projects, namespaces, assemblies, IPC, Windows service, singleton events, autostart entries, icons and active settings folders to AcerCrate.
- Migrate the legacy service/settings only when they belong to the existing Acer Crate installation. Preserve settings/presets and back them up before upgrading.
- Retain GPL notices, copyright attribution and legacy migration identifiers.
- Keep installers, self-contained runtimes, editable source and build caches on the selected development/install drive.

## 1.0.1 — 2026-11-12

Acer Crate is an independent fork of [OpenSense](https://github.com/archivesteak/opensense), based on OpenSense 0.3.6. Upstream copyright notices and the GPL-3.0-or-later license are retained.

### Interface

- Added a black theme with a separate sidebar, a thin divider, and an accent highlight for the selected page.
- Added custom accent, text, and sidebar background colors, including palette and HEX input. The sidebar, selected performance mode, progress indicators, sliders, and mode notification use one accent color.
- Updated Overview with live CPU/GPU readings, fan speeds, memory usage, individual disk usage, and CPU temperature/load history charts.
- Kept Overview cards black, with subtle outlines. Keyboard and cooling pages use slightly lighter card backgrounds.
- Enlarged the keyboard preview to use the available page width while preserving its proportions and zone selection.
- Added the version label and an uninstall button in Settings. Uninstalling preserves user settings.
- Improved foreground activation when opening the application, reopening it, or restoring it from the tray.

### Performance modes and reliability

- Added Ctrl+Alt+F to cycle through available Quiet, Balanced, and Performance modes, subject to firmware and power-source limits.
- Added a rounded, passive notification with an icon and mode name after a confirmed mode change while the main window is hidden or minimized. It is suppressed while the main window is visible.
- Fixed Overview fan readings to use the latest telemetry for each fan. Missing or stale readings are cleared rather than displayed indefinitely.
- Retained personal presets, supported cooling/lighting controls, close-to-tray behavior, and temperature protection.

### Efficiency

- Stop page refresh timers and rendering while the window is hidden; retain bounded sensor history for reopening.
- Cache parsed interface colors and reuse the system animation-settings object.
- Reuse disk rows, avoid resolving unchanged artwork repeatedly, and remove duplicate refreshes on page load.
- Read the displayed Windows power-scheme status from service telemetry instead of making a second interface-side system query.
- Keep the hardware control loop and temperature protection independent of interface refresh settings.

### Packaging and source transparency

- Align application and installer version labels as 1.0.1. The earlier public release was labeled 1.0.0 while its assembly metadata retained the upstream 0.3.6 version.
- Default installation to `D:\Acer Crate`, with a directory selection page, installation backups, integrity checks, and rollback handling.
- Include the .NET and Windows App SDK runtimes for offline installation.
- Publish the complete editable project, build scripts, license, a separate source archive, and SHA-256 checksums.
- Point release update checks at the Acer Crate repository instead of the upstream OpenSense repository.
- Document installer behavior and the scope of the targeted security review in `SECURITY.md`. Functional tests and a static review do not certify the absence of malware.

### Validation and limits

- 690 core tests passed during release validation, with no failures or skipped tests.
- Hardware interaction has been checked on an Acer Nitro AN515-58. Other models are not claimed to be verified.
- Fn plus the letter F was indistinguishable from plain F on the tested keyboard. Ctrl+Alt+F is the supported shortcut for this build.
- The installer is not Authenticode-signed. A dependency advisory for MessagePack 2.5.302 remains documented in `SECURITY.md`.

## 1.0.0 — 2026-11-11

- Initial public Acer Crate release based on OpenSense, with a redesigned interface for monitoring, cooling, performance modes, keyboard lighting, and personal presets.
