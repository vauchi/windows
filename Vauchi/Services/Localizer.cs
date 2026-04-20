// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Vauchi.Interop;

namespace Vauchi.Services;

/// <summary>
/// Translates UI strings via the CABI i18n subsystem
/// (<see cref="VauchiNative.I18nGetString"/>).
///
/// Lifecycle:
/// <list type="number">
///   <item>
///     <see cref="Init"/> points core at the directory that holds the
///     locale JSON files. Copy them into <c>AppDir/locales</c> at build
///     time via the <c>Vauchi.csproj</c> <c>&lt;Content&gt;</c> glob.
///   </item>
///   <item>
///     <see cref="CurrentLocale"/> defaults to the OS language extracted
///     from <see cref="CultureInfo.CurrentUICulture"/>. Callers can override
///     via <see cref="SetLocale"/>.
///   </item>
///   <item>
///     <see cref="T(string)"/> / <see cref="T(string, IReadOnlyDictionary{string,string})"/>
///     look up a key; the key itself is returned when the bindings are
///     unavailable or the key is missing — mirroring linux-qt and iOS/macOS.
///   </item>
/// </list>
/// </summary>
public static class Localizer
{
    private const string DefaultLocale = "en";

    private static bool _initialized;
    private static string _currentLocale = DefaultLocale;

    /// <summary>
    /// BCP-47 code (e.g. "en", "de") currently active for lookups.
    /// </summary>
    public static string CurrentLocale => _currentLocale;

    /// <summary>
    /// Whether the CABI i18n subsystem successfully loaded locale data.
    /// </summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// Load locale JSON files from <paramref name="resourceDir"/> into
    /// core's i18n cache. Called once from <c>App.xaml.cs</c> OnLaunched.
    /// </summary>
    /// <returns>True on success; false when the directory is missing or
    /// the native library reports a load failure.</returns>
    public static bool Init(string resourceDir)
    {
        if (string.IsNullOrEmpty(resourceDir) || !Directory.Exists(resourceDir))
        {
            _initialized = false;
            return false;
        }

        try
        {
            int status = VauchiNative.I18nInit(resourceDir);
            _initialized = status == 0 && VauchiNative.I18nIsInitialized() == 1;
        }
        catch (DllNotFoundException)
        {
            _initialized = false;
        }

        if (_initialized)
        {
            ApplySystemLocale();
        }

        return _initialized;
    }

    /// <summary>
    /// Reset the current locale to the system UI culture's two-letter code.
    /// </summary>
    public static void ApplySystemLocale()
    {
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        SetLocale(string.IsNullOrEmpty(code) ? DefaultLocale : code);
    }

    /// <summary>
    /// Switch the current locale. Unknown codes fall back to English on
    /// lookup; no error is raised here so callers can set freely.
    /// </summary>
    public static void SetLocale(string code)
    {
        _currentLocale = string.IsNullOrEmpty(code) ? DefaultLocale : code;
    }

    /// <summary>
    /// Translate <paramref name="key"/> in the current locale. Returns
    /// the key itself when i18n is not initialised or the key is unknown.
    /// </summary>
    public static string T(string key)
    {
        if (!_initialized) return key;

        try
        {
            string? translated = VauchiNative.I18nGetString(_currentLocale, key);
            return string.IsNullOrEmpty(translated) ? key : translated;
        }
        catch (DllNotFoundException)
        {
            return key;
        }
    }

    /// <summary>
    /// Translate <paramref name="key"/> and substitute <c>{name}</c>-style
    /// placeholders with the supplied arguments. Unreferenced placeholders
    /// pass through unchanged (makes partial args visible during dev).
    /// </summary>
    public static string T(string key, IReadOnlyDictionary<string, string> args)
    {
        string value = T(key);
        if (args.Count == 0) return value;

        foreach (var pair in args)
        {
            value = value.Replace("{" + pair.Key + "}", pair.Value);
        }
        return value;
    }
}
