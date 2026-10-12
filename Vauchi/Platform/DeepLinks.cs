// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Vauchi.Platform;

/// <summary>
/// vauchi:// links handed to the app (vauchi/private#650). Core decides what
/// a link means; the shell only picks the links out of an activation.
/// </summary>
public static class DeepLinks
{
    private const string SchemePrefix = "vauchi://";

    public static IReadOnlyList<string> Select(IEnumerable<string> candidates) =>
        candidates
            .Where(candidate => candidate.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public static string OpenedEventJson(string uri) =>
        JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, string>>
        {
            ["DeepLinkOpened"] = new() { ["uri"] = uri },
        });
}
