# Acer Crate 1.1.0

A Windows application for cooling, performance modes, monitoring, keyboard lighting, and a personal game library, developed and checked on an Acer Nitro AN515-58. This is an independent project, not an official Acer product.

## Download and install

Download `AcerCrate-1.1.0-Setup-x64.exe` from [Releases](https://github.com/vergulesmatvey-oss/Acer-Crate/releases). The installer defaults to `D:\Acer Crate`, allows another folder, and includes the .NET and Windows App SDK runtimes. Administrator approval is needed to install the control service; the installed interface normally runs without elevation.

Avoid running another program that simultaneously controls the same fans or firmware settings. Existing user settings and presets are retained when updating. Uninstalling is available in Windows settings and inside Acer Crate.

This build targets Windows x64 with Windows build 19041 or later. Hardware behavior has been checked on AN515-58. Nitro, Nitro V and Predator models from 2021 onward use capability detection; this is not a claim that every configuration has been verified. Unavailable firmware features stay disabled.

## Features

- Live CPU/GPU readings, fan speeds, RAM and disk usage, and sensor history.
- Quiet, Balanced, Performance, and personal mode controls, subject to hardware and power-source limits.
- Supported automatic/manual cooling, keyboard lighting, zone colors, and presets.
- A black interface with configurable accent/text/sidebar colors and a wide keyboard preview.
- Ctrl+Alt+F mode cycling with an immediate requested-mode notification while the main window is hidden, followed by hardware confirmation.
- Close-to-tray behavior and reduced interface work while hidden, with hardware protection kept active.

- Game library with manual entries, automatic Steam/Epic detection, favorites and recent play history.
- Bounded application/service logs with filtering and export.
- GPU/MUX controls when supported by firmware, plus Windows per-application GPU preferences.
- English on first launch, 38 offline model photographs with labelled series fallback and optional artwork lookup on Acer, and AcerCrate service/settings names.

See [CHANGELOG.md](CHANGELOG.md) for the complete release changes and [SECURITY.md](SECURITY.md) for installer behavior, network activity, review scope, and known limitations.

## Source and local builds

The complete project is available directly in this repository: `src`, `tests`, `installer`, project files, and the original license. Each release also includes a separate source archive and `SHA256SUMS.txt`. The installer embeds its corresponding source archive in the installed `Source` directory.

Use Windows x64 and the .NET 10 SDK selected by `global.json`. From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer/Build-AcerCrate.ps1
```

The build script keeps SDK state, NuGet caches, temporary files, and outputs under `.acercrate-dev` next to the project. If the SDK is not already installed, install the required SDK first. The script downloads a hash-checked NSIS compiler when one is not available. Build outputs are written to `.acercrate-dev/outputs`. That development directory is excluded from the source distribution.

The GitHub Actions workflow builds, tests, and packages the project for review; it does not automatically publish a release.

## Security claims

A targeted static inspection did not find the specifically reported VBS/JS files or `sysmondrv.sys` / `CFG.BAK` in the reviewed initial installer. That is not an independent malware-free certification. This release is unsigned, and a known MessagePack dependency advisory remains documented. Please use the exact SHA-256 and sanitized reports when discussing a detection.

## Credits and license

The hardware-control foundation and substantial parts of this application come from [OpenSense by archivesteak and its contributors](https://github.com/archivesteak/opensense). Acer Crate adds its own interface, settings, packaging, and integration changes. The [original upstream README](UPSTREAM.md) is retained, and the [GNU GPL v3 license](LICENSE) and upstream copyright notices are included.

Some Acer Crate changes were developed with assistance from ChatGPT/Codex. That does not replace code review, hardware validation, or security testing.

## Screenshots

![Acer Crate Interface](https://i.postimg.cc/jSbqy4Kh/1000002108.png)
