<!-- SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

> **Mirror:** This repo is a read-only mirror of [gitlab.com/vauchi/windows](https://gitlab.com/vauchi/windows). Please open issues and merge requests there.

[![Pipeline](https://vauchi.gitlab.io/windows/badges/pipeline.svg)](https://gitlab.com/vauchi/windows/-/pipelines)
[![REUSE](https://api.reuse.software/badge/gitlab.com/vauchi/windows)](https://api.reuse.software/info/gitlab.com/vauchi/windows)

> [!WARNING]
> **Pre-Alpha Software** - This project is under heavy development and not ready for production use.
> APIs may change without notice. Use at your own risk.

# Vauchi Windows

Native Windows desktop app for Vauchi — privacy-focused contact card exchange.

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

## Architecture

This app implements the core-driven UI contract:

- **ScreenRenderer** renders `ScreenModel` from core (JSON via C ABI)
- **14 component UserControls** map to core's `Component` enum variants
- **ActionHandler** maps user input to `UserAction` JSON
- **VauchiNative.cs** wraps C ABI via `LibraryImport` + `System.Text.Json`
- **Platform chrome**: taskbar, notifications, MSIX packaging

All business logic lives in `vauchi-core` (Rust). This repo is a pure rendering layer.

## License

GPL-3.0-or-later
