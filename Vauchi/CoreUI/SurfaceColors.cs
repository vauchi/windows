// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Windows.UI;

namespace Vauchi.CoreUI;

/// <summary>
/// The active theme's colours as Core sends them in a surface's
/// <c>tokens.colors</c> (vauchi/private#814).
/// </summary>
public sealed record SurfaceColors(
    Color Background,
    Color Surface,
    Color Text,
    Color TextMuted,
    Color Accent,
    Color Border,
    Color Success,
    Color Warning,
    Color Error,
    Color SurfaceInput,
    Color SurfaceMuted,
    Color SurfaceElevated,
    Color TextOnAccent,
    Color FocusRing)
{
    public Color SelectedBackground => Accent;
    public Color ActiveSurfaceBorder => Accent;
    public Color Divider => TextMuted;
    public Color SecondaryContainer => TextMuted;
    public bool IsLight => false;

    public static SurfaceColors? From(JsonElement surface) => null;
}
