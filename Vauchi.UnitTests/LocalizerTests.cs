// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using Vauchi.Services;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// Unit tests for the Windows <see cref="Localizer"/> facade over the CABI
/// i18n subsystem.
///
/// These tests run without the native vauchi_cabi library loaded. Localizer
/// is designed to fall back to raw keys in that case (matching linux-qt
/// tr_vauchi behaviour), so every assertion here targets the fallback path.
/// The happy path (CABI present, locale JSON loaded) is covered by the
/// integration tests that run against a built app in CI.
/// </summary>
public class LocalizerTests
{
    [Fact]
    public void Init_with_missing_directory_returns_false()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), "vauchi-tests-no-such-dir-" + Guid.NewGuid());

        bool ok = Localizer.Init(nonExistent);

        Assert.False(ok, "Init must return false when the resource directory does not exist");
        Assert.False(Localizer.IsInitialized, "IsInitialized must remain false after a failed Init");
    }

    [Fact]
    public void Init_with_null_or_empty_returns_false()
    {
        Assert.False(Localizer.Init(""), "Empty path must fail fast");
        Assert.False(Localizer.Init(null!), "Null path must fail fast");
    }

    [Fact]
    public void T_returns_key_when_not_initialized()
    {
        // Starting state: Init has not succeeded in this process (or DLL
        // is absent), so every lookup returns the raw key.
        const string key = "app.name";
        string value = Localizer.T(key);

        Assert.Equal(key, value);
    }

    [Fact]
    public void T_with_args_substitutes_placeholders_in_returned_string()
    {
        // When uninitialized T returns the key itself, but the arg-substituter
        // still runs on that string — verify it doesn't mangle literal keys.
        // A key with no {placeholder} must survive unchanged.
        string result = Localizer.T(
            "import_contacts.result_imported",
            new Dictionary<string, string> { ["count"] = "3" }
        );

        // Uninitialized path: returns the key (no placeholder present), args
        // loop is a no-op.
        Assert.Equal("import_contacts.result_imported", result);
    }

    [Fact]
    public void SetLocale_updates_CurrentLocale()
    {
        string original = Localizer.CurrentLocale;
        try
        {
            Localizer.SetLocale("de");
            Assert.Equal("de", Localizer.CurrentLocale);

            Localizer.SetLocale("fr");
            Assert.Equal("fr", Localizer.CurrentLocale);
        }
        finally
        {
            Localizer.SetLocale(original);
        }
    }

    [Fact]
    public void SetLocale_with_empty_falls_back_to_default()
    {
        string original = Localizer.CurrentLocale;
        try
        {
            Localizer.SetLocale("");
            Assert.Equal("en", Localizer.CurrentLocale);

            Localizer.SetLocale(null!);
            Assert.Equal("en", Localizer.CurrentLocale);
        }
        finally
        {
            Localizer.SetLocale(original);
        }
    }

    [Fact]
    public void ApplySystemLocale_uses_current_ui_culture()
    {
        string original = Localizer.CurrentLocale;
        try
        {
            Localizer.SetLocale("xx"); // force off of system
            Localizer.ApplySystemLocale();

            // CurrentUICulture.TwoLetterISOLanguageName is always two lowercase
            // letters; ApplySystemLocale must copy that in.
            string expected = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (string.IsNullOrEmpty(expected))
                expected = "en";
            Assert.Equal(expected, Localizer.CurrentLocale);
        }
        finally
        {
            Localizer.SetLocale(original);
        }
    }

    [Fact]
    public void T_with_empty_args_returns_plain_string()
    {
        string result = Localizer.T("action.ok", new Dictionary<string, string>());
        // Uninitialized path — returns key verbatim.
        Assert.Equal("action.ok", result);
    }
}
