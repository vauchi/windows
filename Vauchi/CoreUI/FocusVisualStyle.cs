// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI;

/// <summary>
/// The WCAG 2.2 (2.4.11 Focus Not Obscured) focus ring every interactive
/// presentation control renders on keyboard focus.
/// </summary>
/// <remarks>
/// Mirrors <c>tokens.json</c>'s <c>focus.ring_width</c> (3) and
/// <c>ring_offset</c> (2) by hand: that section does not cross the C ABI
/// yet. Reconcile against a generated value once core exposes one. The
/// ring's colour is the surface's <c>focus_ring</c> role
/// (vauchi/private#814).
/// </remarks>
public static class FocusVisualStyle
{
    public const double RingWidth = 3;
    public const double RingOffset = 2;

    /// <param name="colors">The surface's palette; without one the ring
    /// keeps the system brush.</param>
    public static void Apply(Control control, SurfaceColors? colors)
    {
        if (colors is not null)
            control.FocusVisualPrimaryBrush = new SolidColorBrush(colors.FocusRing);
        control.FocusVisualPrimaryThickness = new Thickness(RingWidth);
        control.FocusVisualMargin = new Thickness(RingOffset);
    }
}
