# Acer Crate 1.1.0 validation

- 714 automated core tests passed; 0 failed, 0 skipped (2026-11-13).
- The WinUI application and Windows service published successfully as self-contained Windows x64 binaries.
- Native preview smoke checks opened Game library, Logs, Graphics and Fans successfully after registering the new pages with WinUI. First-launch English, the automatic Steam/Epic toggle and the black Overview layout without card outlines were observed.
- Active projects, namespaces, assemblies, service, IPC and settings use AcerCrate. Legacy names are restricted to migration and upstream/legal context.
- Installer PowerShell syntax parsed without errors. The package excludes user settings, logs, development caches and old branded runtime files.
- The corresponding source archive includes the editable projects, tests, build/installer scripts and GPL notices.
- The current dependency build still warns about MessagePack 2.5.302 (GHSA-qhrr-8q5h-9q3h). No malware-free certification or universal hardware compatibility is claimed.

Physical MUX switching and reboot were not performed. This validation is separate from the earlier AN515-58 hardware checks and the targeted inspection of the initial public installer. UI smoke checks and installation/migration checks are recorded separately after packaging.
- Artwork tests validate exact/series selection, suffix normalization and the signatures, size and unique model codes of all 38 bundled image records.

- The OSD preview rendered a 350 x 76 premultiplied-alpha surface with 232 partially transparent edge pixels and zero alpha at the corner; no hard region clipping is used.
