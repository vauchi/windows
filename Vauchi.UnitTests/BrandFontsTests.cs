// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO;
using System.Runtime.CompilerServices;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class BrandFontsTests
{
    /// <summary>
    /// Pins the role → font mapping against the literal <c>ms-appx:///...#Family</c>
    /// sources rather than against <c>BrandFonts</c>'s own constants, so a typo in
    /// either the switch or a constant still fails this test.
    /// </summary>
    [Theory]
    [InlineData("heading", "ms-appx:///Assets/Fonts/BricolageGrotesque[opsz,wdth,wght].ttf#Bricolage Grotesque")]
    [InlineData("body", "ms-appx:///Assets/Fonts/HankenGrotesk[wght].ttf#Hanken Grotesk")]
    [InlineData("caption", "ms-appx:///Assets/Fonts/HankenGrotesk[wght].ttf#Hanken Grotesk")]
    [InlineData("muted", "ms-appx:///Assets/Fonts/HankenGrotesk[wght].ttf#Hanken Grotesk")]
    [InlineData("monospace", "ms-appx:///Assets/Fonts/JetBrainsMono[wght].ttf#JetBrains Mono")]
    public void KnownStylesMapToTheirIntendedBrandFontSource(string style, string expectedSource)
    {
        Assert.Equal(expectedSource, BrandFonts.SourceForStyle(style));
    }

    /// <summary>A style Core names that this build has not learned must still render.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-known-style")]
    public void UnknownStyleFallsBackToTheBodyFont(string? style)
    {
        Assert.Equal(BrandFonts.BodySource, BrandFonts.SourceForStyle(style));
    }

    /// <summary>
    /// The five variable TrueType files vendored from <c>themes/fonts/</c> must
    /// actually ship with the app, not just be named by <c>BrandFonts</c>.
    /// Resolved from this test file's own path (not the build output directory)
    /// so the check holds without a `dotnet build` having run first.
    /// </summary>
    [Theory]
    [InlineData("BricolageGrotesque[opsz,wdth,wght].ttf")]
    [InlineData("HankenGrotesk[wght].ttf")]
    [InlineData("HankenGrotesk-Italic[wght].ttf")]
    [InlineData("JetBrainsMono[wght].ttf")]
    [InlineData("JetBrainsMono-Italic[wght].ttf")]
    public void EachBrandFontFileIsVendoredUnderAssetsFonts(string fileName)
    {
        string path = Path.Combine(FontsDirectory(), fileName);
        Assert.True(File.Exists(path), $"expected vendored font file at {path}");
    }

    private static string FontsDirectory([CallerFilePath] string testSourcePath = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(testSourcePath)!, "..", "Vauchi", "Assets", "Fonts"));
}
