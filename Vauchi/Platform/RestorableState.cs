// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi.Platform;

/// <summary>
/// Core's opaque restorable state, kept in the data directory between
/// starts (vauchi/private#814).
/// </summary>
public static class RestorableState
{
    public static void Keep(string dataDirectory, string state)
    {
    }

    public static string? Kept(string dataDirectory) => null;
}
