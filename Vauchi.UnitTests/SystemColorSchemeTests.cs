// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.Platform;
using Windows.UI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// Windows reports its app mode as the system background colour: black in
/// dark mode, white in light (vauchi/private#814).
/// </summary>
public class SystemColorSchemeTests
{
    [Theory]
    [InlineData(0, 0, 0, "dark")]
    [InlineData(255, 255, 255, "light")]
    [InlineData(32, 32, 32, "dark")]
    [InlineData(240, 240, 240, "light")]
    public void TheOsBackgroundNamesTheScheme(byte r, byte g, byte b, string scheme)
    {
        Assert.Equal(scheme, SystemColorScheme.From(Color.FromArgb(255, r, g, b)));
    }
}
