// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace Vauchi.Services;

// Marks the main app window as opted-out of screen capture so
// screen-recording tools (Snipping Tool, ShareX, OBS, Win+G Game
// Bar, screen-sharing apps in Teams/Slack/Zoom, GraphicsCapture
// API consumers) capture only a black rectangle. Mirrors iOS
// (UIScreen.isCaptured overlay) and Android (FLAG_SECURE).
//
// `WDA_EXCLUDEFROMCAPTURE` (0x11) requires Windows 10 build 2004
// (May 2020 Update) or newer. On older builds the flag is silently
// ignored — `SetWindowDisplayAffinity` returns FALSE but the
// window remains visible to capture tools. We do not fall back to
// `WDA_MONITOR` (which makes the window content render as black
// in capture *and* on remote-desktop sessions): legitimate remote-
// desktop work would lose the entire UI. `WDA_EXCLUDEFROMCAPTURE`
// strips capture only, leaving local rendering intact.
//
// Disabled in DEBUG builds — UI test automation needs to read
// window contents.
internal static class ScreenCaptureProtection
{
    private const uint WDA_NONE = 0x0;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    public static void Enable(Window window)
    {
#if DEBUG
        return;
#else
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (hwnd == IntPtr.Zero) return;
        SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
#endif
    }
}
