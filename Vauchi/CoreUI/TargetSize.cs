// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>
/// Resolves the touch/pointer target floor a surface renders its action
/// rows, list rows, toggle rows and nav items at.
/// </summary>
/// <remarks>
/// Core sends <c>minimum_target_size</c> inside a surface's
/// <c>PresentationTokens</c> — the one touch-target value that reaches
/// the C ABI today (the rest of <c>tokens.json</c>'s <c>touch_target</c>
/// section does not). When a surface carries none, 48 is Android's floor
/// rather than Apple's 44: the age-friendly literature the inclusive
/// redesign cites argues 44 under-serves older hands.
/// </remarks>
public static class TargetSize
{
    public const double Default = 48;
    private const double Floor = 24;

    public static double From(JsonElement surface) =>
        surface.TryGetProperty("tokens", out JsonElement tokens)
        && tokens.TryGetProperty("minimum_target_size", out JsonElement minimum)
        && minimum.TryGetDouble(out double value)
            ? Math.Max(Floor, value)
            : Default;
}
