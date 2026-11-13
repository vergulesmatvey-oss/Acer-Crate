# Security and transparency

Acer Crate is an independent GPL-3.0-or-later fork of [OpenSense](https://github.com/archivesteak/opensense), based on OpenSense 0.3.6. It is not an Acer product and is not endorsed by Acer or the upstream OpenSense project.

## What installation does

The NSIS installer unpacks its bundled payload into a temporary directory and invokes two readable PowerShell scripts: `Install-AcerCrate-Package.ps1` and `Ensure-PowerPlans.ps1`.

The installation helper verifies bundled SHA-256 hashes, backs up an existing installation and settings, installs files into the selected folder (default `D:\Acer Crate`), and configures the `AcerCrate` Windows service to run the included `Service\AcerCrate.Service.exe`. The installer creates a Start menu shortcut and a Windows uninstall entry. The power-plan helper uses Windows `powercfg.exe` to create or update the application's power plans.

Administrator rights are required for installation and service configuration. The installed interface normally runs without elevation. Active settings use `%APPDATA%\AcerCrate` and `%PROGRAMDATA%\AcerCrate`. The installer backs up and migrates legacy locations and the old service only when tied to an existing Acer Crate installation; migration code retains the legacy identifiers for that purpose. Binary/service folders are protected from standard-user writes. Only the current user log subfolder grants that user write access. The application uses the inherited Acer firmware/WMI/HID interfaces for supported hardware controls. Existing PawnIO support is retained for supported CPU sensor access; the Acer Crate installer does not install a new `sysmondrv.sys` driver.

The offline installer does not need to download application runtimes. The application's optional update checks contact GitHub's release API; release downloads come from GitHub. Version 1.0.1 points these checks at `vergulesmatvey-oss/Acer-Crate`. Optional automatic artwork lookup also contacts store.acer.com and Acer-hosted image URLs using only the public model code, then caches successful images. It can be disabled in Settings; bundled images do not need the network. Application/service logs stay local unless the user exports them. These features are network activity and must not be described as having no outbound connections.

## Targeted review on 2026-11-12

Reports on Reddit mentioned `sysmondrv.sys`, `CFG.BAK`, VBS/JavaScript payloads, and a script making outbound connections. A targeted static inspection of the published initial release found:

- Reviewed installer SHA-256: `7ba61e178d2ecd9d4b8360a5104c4244d8dd55c71f1b36417a5003074eb9fea3`.
- Reviewed `Source.zip` SHA-256: `51f2b158c1322fa36b41f7ff01f14b8649e2a5ab6998c91b6194382cc9714833`.
- No archive entries named `sysmondrv.sys` or `CFG.BAK`, and no `.vbs`, `.js`, or `.sys` files in the 882 extracted installer entries.
- No hash mismatches against the bundled payload manifest. This establishes internal consistency, not the trustworthiness of the publisher.
- Both extracted installation scripts matched the separately published source files.
- The 387 files in the separately published source archive matched the source archive embedded in that installer.

The current release has its own SHA-256 checksums and validation notes. These are separate builds: do not apply the initial release's hash to later versions.

This was a targeted static inspection. It was not an independent security audit, an antivirus scan, a clean virtual-machine execution trace, or proof that arbitrary downloaded copies are malware-free. No blanket claim that the application is "virus-free" is made. Passing functional tests does not establish that claim either. The installer is currently not Authenticode-signed.

## Known dependency advisory

The build currently reports [GHSA-qhrr-8q5h-9q3h](https://github.com/advisories/GHSA-qhrr-8q5h-9q3h) for MessagePack 2.5.302. This known dependency issue is not evidence of a VBS/JS payload, but it still needs a separately tested compatibility update across the client and service. It is not presented as fixed in this release.

## Reporting a finding

Please identify the exact download URL, release tag, SHA-256, detected file's full path and hash, scanner name/version, detection name, and a sanitized process/network trace if available. Distinguish files extracted by the installer from files that already existed on the test system. Do not post passwords, tokens, or private user data in public reports. A matching file hash is necessary to compare findings between different copies.

Source code, tests, and installer scripts are available for inspection and local builds. Please report concrete evidence through the repository rather than treating either a claim of infection or a claim of safety as conclusive without that evidence.
