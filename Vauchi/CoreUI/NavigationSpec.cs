// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi.CoreUI;

/// <summary>
/// One persistent navigation destination, as Core prepares it in a
/// <c>SetNavigation</c> command's <c>navigation.items</c> array.
/// </summary>
public sealed record NavigationSpec(
    string InteractionId,
    string Label,
    string AccessibilityLabel,
    string? IconToken,
    bool Selected,
    int BadgeCount);
