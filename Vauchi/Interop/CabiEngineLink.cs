// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Vauchi.CoreUI;

namespace Vauchi.Interop;

/// <summary>
/// The display shell's engine over Core's C ABI: the one place the app
/// hands the display shell a way to reach Core (ADR-078 D-13).
/// </summary>
public sealed class CabiEngineLink : IEngineLink
{
    private readonly IntPtr _appHandle;

    public CabiEngineLink(IntPtr appHandle)
    {
        if (appHandle == IntPtr.Zero)
            throw new ArgumentException("The Core application handle is null.", nameof(appHandle));
        _appHandle = appHandle;
    }

    public string? InitialCommands() => VauchiNative.AppInitialCommands(_appHandle);

    public string? Dispatch(string eventJson) => VauchiNative.AppDispatch(_appHandle, eventJson);
}
