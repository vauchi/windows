<!-- SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

> **Mirror:** This repo is a read-only mirror of [gitlab.com/vauchi/windows-app](https://gitlab.com/vauchi/windows-app). Please open issues and merge requests there.

[![Pipeline](https://img.shields.io/endpoint?url=https://vauchi.gitlab.io/windows-app/badges/pipeline.json&label=pipeline)](https://gitlab.com/vauchi/windows-app)
[![REUSE](https://api.reuse.software/badge/gitlab.com/vauchi/windows-app)](https://api.reuse.software/info/gitlab.com/vauchi/windows-app)

> [!NOTE]
> **You're early — and that's the point.** Vauchi is pre-alpha and
> under heavy development: not yet ready for production, and APIs may
> change without notice. If you're here now, you can help shape it —
> try it, break it, and tell us what's missing.

# Vauchi Windows

Native Windows app for Vauchi — living contact cards, exchanged in person.

Built with WinUI 3 + C# (.NET 8). Uses `vauchi-cabi` C ABI bindings via P/Invoke.

## Prerequisites

- Windows 10 21H2+
- .NET 8 SDK
- Visual Studio 2022 with WinUI 3 workload

## Build

```bash
dotnet build Vauchi.sln
dotnet test Vauchi.Tests
```

### Screen catalog stills

`Vauchi.exe --render-catalog <catalog.json> <out-dir>` replays Core's
`screen_catalog_v1.json` through the real renderer and writes
`<code_id>.png` and `<code_id>.dark.png` per screen at 1440x900, without
starting the Core engine. CI runs it as `test:screen-catalog`.

## Architecture

The app assembles Core, the display shell and the hardware shell
(ADR-078): Core emits generic, fully prepared commands, and the shells
report opaque events back.

- **The display shell** is
  [windows-display](https://gitlab.com/vauchi/windows-display), pinned
  as the `vauchi-windows-display` package: it renders Core's
  presentation commands with WinUI controls and reports user input as
  opaque event JSON, reaching the engine through `IEngineLink`
- **The hardware shell** is
  [windows-hw](https://gitlab.com/vauchi/windows-hw), pinned as
  `vauchi-windows-hw` and `vauchi-windows-hw-native`
- **VauchiNative.cs** wraps C ABI via `LibraryImport` + `System.Text.Json`;
  `CabiEngineLink` hands it to the display shell
- **Platform chrome**: taskbar, notifications, MSIX packaging

All business logic lives in `vauchi-core` (Rust). This repo holds none.

## License

GPL-3.0-or-later
