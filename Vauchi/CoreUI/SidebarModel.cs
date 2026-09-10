// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;

namespace Vauchi.CoreUI;

/// <summary>One persistent navigation row, mapped to what the pane renders.</summary>
public sealed record SidebarItemModel(
    string InteractionId,
    string Label,
    string AccessibilityLabel,
    string Glyph,
    bool Selected,
    int BadgeCount);

/// <summary>
/// The persistent navigation pane's render-ready state, kept free of WinUI
/// types so the mapping from <see cref="NavigationSpec"/> is unit-testable
/// without a XAML runtime.
/// </summary>
public sealed record SidebarModel(IReadOnlyList<SidebarItemModel> Items, int SelectedIndex, bool Hidden)
{
    public static SidebarModel From(IReadOnlyList<NavigationSpec>? specs)
    {
        IReadOnlyList<SidebarItemModel> items = (specs ?? Array.Empty<NavigationSpec>())
            .Select(spec => new SidebarItemModel(
                spec.InteractionId,
                spec.Label,
                spec.AccessibilityLabel,
                NavigationIcons.Glyph(spec.IconToken),
                spec.Selected,
                spec.BadgeCount))
            .ToList();
        int selectedIndex = items.Select((item, index) => (item, index))
            .Where(pair => pair.item.Selected)
            .Select(pair => pair.index)
            .DefaultIfEmpty(-1)
            .First();
        return new SidebarModel(items, selectedIndex, Hidden: items.Count == 0);
    }
}
