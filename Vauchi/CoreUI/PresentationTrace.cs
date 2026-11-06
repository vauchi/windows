// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Diagnostics;
using System.IO;

namespace Vauchi.CoreUI;

/// <summary>
/// Records how far a presentation update got, so a hang or a crash with no
/// managed stack can be placed in Core or in XAML (private#1099). DEBUG only,
/// and stage names only: event payloads can hold what a user typed.
/// </summary>
internal static class PresentationTrace
{
    [Conditional("DEBUG")]
    public static void Mark(string stage) =>
        File.AppendAllText(
            Path.Combine(AppContext.BaseDirectory, "presentation-trace.log"),
            $"{Environment.ProcessId} {DateTime.UtcNow:HH:mm:ss.fff} {stage}{Environment.NewLine}");
}
