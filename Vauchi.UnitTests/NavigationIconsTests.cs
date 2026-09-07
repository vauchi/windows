// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class NavigationIconsTests
{
    /// <summary>
    /// The icon tokens Core attaches to navigation items, mirroring
    /// <c>tab_metadata</c> in
    /// <c>vauchi-app/src/ui/app_engine/navigation.rs</c>. Core owns the list;
    /// this copy exists so the map can be proven total over everything Core
    /// ships today.
    /// </summary>
    public static readonly IReadOnlyList<string> CoreNavigationTokens = new[]
    {
        "person.crop.rectangle",
        "person.2",
        "qrcode",
        "folder",
        "tag",
        "mappin.and.ellipse",
        "person.badge.plus",
        "gearshape",
        "questionmark.circle",
        "key.horizontal",
        "laptopcomputer",
        "externaldrive",
        "hand.raised",
        "bubble.left.and.bubble.right",
        "list.bullet.rectangle",
        "house",
    };

    public static TheoryData<string> Tokens()
    {
        var data = new TheoryData<string>();
        foreach (string token in CoreNavigationTokens)
            data.Add(token);
        return data;
    }

    /// <summary>
    /// A Segoe Fluent Icons glyph is a single code point in the Unicode
    /// Private Use Area. Anything else renders as a tofu box or an empty gap
    /// beside the label, which is the failure this map exists to prevent.
    /// </summary>
    [Theory]
    [MemberData(nameof(Tokens))]
    public void EveryCoreTokenMapsToASinglePrivateUseGlyph(string token)
    {
        string glyph = NavigationIcons.Glyph(token);

        Assert.Equal(1, glyph.Length);
        Assert.InRange(glyph[0], '\uE000', '\uF8FF');
    }

    /// <summary>
    /// Each token maps to the glyph it is supposed to, not merely to some
    /// glyph.
    ///
    /// The shape checks above pass for any private-use codepoint, and
    /// distinctness passes for any sixteen different ones, so between them
    /// they would still accept Contacts and Settings being swapped. These are
    /// opaque codepoints rather than names — nobody reviewing a diff can see
    /// that \uE716 is People — so the intended value is pinned here or it is
    /// pinned nowhere.
    /// </summary>
    [Theory]
    [InlineData("person.2", "\uE716")]
    [InlineData("qrcode", "\uED14")]
    [InlineData("gearshape", "\uE713")]
    [InlineData("externaldrive", "\uEDA2")]
    [InlineData("house", "\uE80F")]
    public void KnownTokensMapToTheirIntendedGlyph(string token, string expected)
    {
        Assert.Equal(expected, NavigationIcons.Glyph(token));
    }

    /// <summary>
    /// A token Core names but this build has not learned about must not leave
    /// the row without an icon.
    /// </summary>
    [Fact]
    public void UnknownTokenFallsBackToTheDefaultGlyph()
    {
        Assert.Equal(
            NavigationIcons.FallbackGlyph,
            NavigationIcons.Glyph("sparkles.rectangle.not.a.token"));
    }

    /// <summary><c>icon_token</c> is optional in the presentation protocol.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AbsentTokenFallsBackToTheDefaultGlyph(string? token)
    {
        Assert.Equal(NavigationIcons.FallbackGlyph, NavigationIcons.Glyph(token));
    }

    [Fact]
    public void FallbackGlyphIsItselfASinglePrivateUseGlyph()
    {
        Assert.Equal(1, NavigationIcons.FallbackGlyph.Length);
        Assert.InRange(NavigationIcons.FallbackGlyph[0], '\uE000', '\uF8FF');
    }

    /// <summary>
    /// Distinct destinations must stay visually distinguishable; collapsing
    /// two tokens onto one glyph, or onto the fallback, would silently undo
    /// the recognition the icons exist to provide.
    /// </summary>
    [Fact]
    public void DistinctTokensGetDistinctGlyphs()
    {
        var seen = new HashSet<string>();
        foreach (string token in CoreNavigationTokens)
        {
            string glyph = NavigationIcons.Glyph(token);
            Assert.NotEqual(NavigationIcons.FallbackGlyph, glyph);
            Assert.True(
                seen.Add(glyph),
                $"token {token} reuses a glyph already mapped elsewhere");
        }
    }
}
