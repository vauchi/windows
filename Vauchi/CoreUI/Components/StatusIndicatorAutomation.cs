// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi.CoreUI.Components;

/// <summary>
/// Helpers for formatting the accessibility name of a StatusIndicator component.
/// </summary>
public static class StatusIndicatorAutomation
{
    /// <summary>
    /// Returns the UIA Name for a StatusIndicator. Uses the core-resolved
    /// <c>status_label</c> when present; otherwise falls back to the raw
    /// <c>status</c> discriminant to keep old payloads readable.
    /// </summary>
    public static string FormatName(string title, string status, string statusLabel) =>
        !string.IsNullOrEmpty(statusLabel) ? $"{title}: {statusLabel}" : $"{title}: {status}";
}
