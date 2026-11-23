// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Vauchi.UnitTests;

public class BrandFontsTests
{
    /// <summary>
    /// The five variable TrueType files vendored from <c>themes/fonts/</c> must
    /// actually ship with the app: the display shell (windows-display) names
    /// them as <c>ms-appx:///Assets/Fonts/…</c> sources, and only the app
    /// carries them. Resolved from this test file's own path (not the build
    /// output directory) so the check holds without a `dotnet build` first.
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
