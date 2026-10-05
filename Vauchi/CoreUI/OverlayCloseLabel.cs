// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>
/// The overlay close button's label (vauchi/private#517), read from Core's
/// optional <c>close_label</c>. Falls back to the localized word for an
/// overlay from an older Core that omits it.
/// </summary>
public static class OverlayCloseLabel
{
    public static string For(JsonElement overlay, Func<string, string> t) =>
        overlay.TryGetProperty("close_label", out JsonElement closeLabel)
        && closeLabel.ValueKind == JsonValueKind.String
        && closeLabel.GetString() is { Length: > 0 } label
            ? label
            : t("action.close");
}
