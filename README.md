<div align="center">

<img src="./.github/assets/fasterLegit.png" alt="fasterLegit" width="760" />

# fasterLegit

**A focused, open-source Windows utility for cleaning up the system, managing
startup items, and applying transparent, reversible optimizations.**

[![Release](https://img.shields.io/github/v/release/HIT-fasterLegit/fasterLegit?style=flat-square&color=fed114)](https://github.com/HIT-fasterLegit/fasterLegit/releases/latest)
[![CI](https://github.com/HIT-fasterLegit/fasterLegit/actions/workflows/ci.yml/badge.svg)](https://github.com/HIT-fasterLegit/fasterLegit/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-GPL--3.0-red?style=flat-square)](./LICENSE)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078d4?style=flat-square)](https://www.microsoft.com/windows)

[Download](https://github.com/HIT-fasterLegit/fasterLegit/releases/latest)
· [Report a bug](https://github.com/HIT-fasterLegit/fasterLegit/issues)
· [Contribute](./CONTRIBUTING.md)

</div>

## About

fasterLegit brings a small set of useful Windows maintenance tools into one
Fluent-style desktop app. It is intentionally conservative: the app shows what
an action changes, checks whether an optimization is supported, records its
result, and keeps revert data when the operation can be undone.

The application runs locally, contains no telemetry, and does not apply tweaks
automatically. You decide which actions to run.

> [!IMPORTANT]
> fasterLegit changes Windows settings and therefore requests Administrator
> privileges. Review each action and create a restore point before changing a
> system you depend on.

## Current features

| Area | What is available |
| --- | --- |
| **Dashboard** | CPU, memory, GPU, storage, Windows, firmware, security, active power plan, and uptime information |
| **Optimize** | Disable advertising and tailored suggestions, reduce visual effects, and hide the Windows 11 Start menu's Recommended section |
| **Bloatware** | Find removable AppX packages, review their risk labels, and remove selected packages |
| **Startup Manager** | Inspect startup applications and startup-related scheduled tasks, then enable or disable them |
| **Disk Cleanup** | Scan selected cleanup locations before removing temporary files, update leftovers, thumbnails, Recycle Bin contents, crash reports, and old Windows installations |
| **Settings** | Control appearance and application behavior, view legal information, and manage local preferences |

The Optimize page currently contains three deliberately scoped optimizations.
This README does not advertise experimental or hidden modules that are not
available from the main navigation.

## Safety and transparency

- **Explicit actions:** nothing is changed until you select and run an action.
- **Compatibility checks:** unsupported optimizations are identified before
  they can be applied.
- **Risk and status details:** optimization cards expose risk, tags, state, and
  recorded operation details.
- **Revert support:** successful reversible optimization steps are persisted
  locally and can be undone from the app.
- **Honest outcomes:** skipped, refused, irreversible, and failed steps are
  recorded separately instead of being reported as successful changes.
- **Local-only data:** settings, logs, history, and revert data stay under
  `%LocalAppData%\fasterLegit\`.

Some operations, such as removing an application or permanently deleting files,
are inherently irreversible. The app asks for confirmation where appropriate,
but a Windows restore point and a backup remain the safest fallback.

## Requirements

- Windows 10 or Windows 11
- x64 or ARM64 processor for published releases
- Administrator privileges

Published builds are self-contained, so users do not need to install the .NET
runtime separately.

## Getting started

1. Download the latest executable from
   [GitHub Releases](https://github.com/HIT-fasterLegit/fasterLegit/releases/latest).
2. Choose the build for your processor: `x64` or `ARM64`.
3. Run `fasterLegit.exe` and approve the Administrator prompt.
4. Review an action's description and risk before applying it.
5. Restart Windows when an action indicates that a restart is required.

No installer is required. Windows SmartScreen may warn about an unsigned build;
you can inspect the source and build the application yourself before running it.

## Build from source

Development is Windows-only and requires the .NET 10 SDK.

```powershell
git clone https://github.com/HIT-fasterLegit/fasterLegit.git
cd fasterLegit
dotnet restore fasterLegit.slnx
dotnet build fasterLegit.slnx --configuration Release --no-restore
```

Run the app from an elevated terminal:

```powershell
dotnet run --project fasterLegit/fasterLegit.csproj
```

Run the test suite:

```powershell
dotnet run --project fasterLegit.Test/fasterLegit.Test.csproj --configuration Release -- -longRunning 30
```

Create a self-contained release build:

```powershell
publish.bat single x64 --no-pause
```

Use `arm64` instead of `x64` for an ARM64 build. `publish.bat portable` creates
a multi-file self-contained package, while `publish.bat single` creates one
executable.

## Project structure

```text
fasterLegit/
├── fasterLegit/       WPF application
├── fasterLegit.Test/  xUnit integration tests
├── fasterLegit.slnx   .NET solution
└── publish.bat        Windows publishing helper
```

The app targets `net10.0-windows` and uses WPF, WPF UI, CommunityToolkit.Mvvm,
Microsoft.Extensions.Hosting, Serilog, and Newtonsoft.Json. System-changing
work is routed through one operation runner, while Windows registry, service,
scheduled-task, shell, and power operations remain in their dedicated
providers.

Optimization and tool state is stored locally:

```text
%LocalAppData%\fasterLegit\
├── History\
├── Revert\
├── Resources\
├── Crashes\
├── appsettings.json
└── fasterLegit.log
```

The selectable application locale is currently Vietnamese; neutral English
resources remain available as the resource fallback.

## Contributing

Bug reports and focused pull requests are welcome. Before opening a pull
request, read [CONTRIBUTING.md](./CONTRIBUTING.md), then run the same core checks
used by CI:

```powershell
dotnet restore fasterLegit.slnx
dotnet build fasterLegit.slnx --configuration Release --no-restore
dotnet run --project fasterLegit.Test/fasterLegit.Test.csproj --configuration Release --no-build -- -longRunning 30
csharpier check .
```

## Privacy and legal

- [Privacy Policy](./PRIVACY.md)
- [Terms of Service](./TERMS.md)
- [Disclaimer](./DISCLAIMER.md)
- [Third-party notices](./THIRD-PARTY-NOTICES.md)

fasterLegit is provided without warranty. Back up important data and create a
Windows restore point before applying system changes.

## License

Licensed under the [GNU General Public License v3.0](./LICENSE).
