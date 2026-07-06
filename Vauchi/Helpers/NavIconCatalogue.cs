// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.Helpers;

/// <summary>
/// Maps core-owned screen ids to native WinUI <see cref="Symbol"/>
/// icons. Core owns the screen set, labels, and locale; the
/// frontend only owns its preferred native icon (a platform-icon
/// carve-out per ADR-021/043 §6).
///
/// Missing entries fall back to <see cref="Symbol.Home"/>.
///
/// Only Symbol values confirmed to exist in the project's
/// WindowsAppSDK version (1.7) are used — Shield and Protected
/// are present in the enum per Microsoft docs but missing in
/// this SDK build. "privacy" intentionally falls back to Home
/// until a better Symbol is validated or replaced with a
/// FontIcon glyph.
/// </summary>
public static class NavIconCatalogue
{
    // TODO(HUMBLE): W — maps domain screen IDs to native symbols; accept as platform-icon carve-out or core should provide icon hint (see _private/docs/problems/2026-07-06-desktop-tui-web-domain-shell-violations).
    private static readonly IReadOnlyDictionary<string, Symbol> Map = new Dictionary<string, Symbol>
    {
        ["my_info"]           = Symbol.ContactInfo,
        ["contacts"]          = Symbol.People,
        ["exchange"]          = Symbol.Send,
        ["groups"]            = Symbol.People,
        ["settings"]          = Symbol.Setting,
        ["recovery"]          = Symbol.Permissions,
        ["device_management"] = Symbol.PhoneBook,
        ["backup"]            = Symbol.SaveLocal,
        ["support"]           = Symbol.Comment,
        ["help"]              = Symbol.Help,
        ["activity_log"]      = Symbol.List,
        ["sync"]              = Symbol.Sync,
        ["more"]              = Symbol.More,
        ["onboarding"]        = Symbol.Add,
    };

    /// <summary>
    /// Returns the native <see cref="Symbol"/> mapped to the given
    /// core-owned screen id, or <see cref="Symbol.Home"/> if the id
    /// has no entry in the catalogue.
    /// </summary>
    public static Symbol For(string screenId) =>
        Map.TryGetValue(screenId, out var sym) ? sym : Symbol.Home;
}
