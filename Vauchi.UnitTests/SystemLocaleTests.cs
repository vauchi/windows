// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;
using Vauchi.Platform;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// The OS language reaches Core unparsed so "Follow system" renders in it;
/// Core alone maps the tag to a language (vauchi/private#721, ADR-066).
/// </summary>
public class SystemLocaleTests
{
    [Fact]
    public void TheUiLanguageIsReportedAsItsTag()
    {
        Assert.Equal(
            """{"system_locale":"fr-CH"}""",
            SystemLocale.ContextJson(new CultureInfo("fr-CH")));
    }

    // The invariant culture names no language, so Core falls back on its own.
    [Fact]
    public void TheInvariantCultureReportsNoSystemLanguage()
    {
        Assert.Equal("{}", SystemLocale.ContextJson(CultureInfo.InvariantCulture));
    }

    // One push carries both, so the kept pick applies with the system
    // language (vauchi/private#814).
    [Fact]
    public void TheKeptRestorableStateRidesInTheSamePush()
    {
        Assert.Equal(
            """{"system_locale":"fr-CH","restorable_state":"{\u0022v\u0022:1}"}""",
            SystemLocale.ContextJson(new CultureInfo("fr-CH"), "{\"v\":1}"));
        Assert.Equal(
            """{"restorable_state":"kept"}""",
            SystemLocale.ContextJson(CultureInfo.InvariantCulture, "kept"));
    }
}
