// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Vauchi.Interop;

namespace Vauchi.Platform;

/// <summary>
/// Reports the OS language tag to Core, unparsed, so "Follow system" renders
/// in it; Core alone maps the tag to a language (vauchi/private#721, ADR-066).
/// </summary>
public static class SystemLocale
{
    public static string ContextJson(CultureInfo culture, string? restorableState = null)
    {
        var context = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(culture.Name))
            context["system_locale"] = culture.Name;
        if (restorableState is not null)
            context["restorable_state"] = restorableState;
        return JsonSerializer.Serialize(context);
    }

    /// <summary>
    /// The OS language and the restorable state the shell kept travel in one
    /// push, so the kept pick applies with the system language
    /// (vauchi/private#814).
    /// </summary>
    public static void Report(IntPtr appHandle, string dataDirectory) =>
        VauchiNative.AppSetRenderContextJson(
            appHandle,
            ContextJson(CultureInfo.CurrentUICulture, RestorableState.Kept(dataDirectory)));
}
