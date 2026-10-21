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
    public static string ContextJson(CultureInfo culture) =>
        string.IsNullOrEmpty(culture.Name)
            ? "{}"
            : JsonSerializer.Serialize(new Dictionary<string, string> { ["system_locale"] = culture.Name });

    public static void Report(IntPtr appHandle) =>
        VauchiNative.AppSetRenderContextJson(appHandle, ContextJson(CultureInfo.CurrentUICulture));
}
