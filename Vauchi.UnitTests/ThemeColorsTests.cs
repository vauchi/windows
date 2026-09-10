// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.CoreUI;
using Windows.UI;
using Xunit;

namespace Vauchi.UnitTests;

public class ThemeColorsTests
{
    /// <summary>
    /// <c>AvatarFallback</c> and <c>AvatarFallbackHex</c> are two
    /// hand-maintained copies of the same color (RG-11: core has no
    /// avatar-fallback token to consume yet, see the TODO on
    /// <c>ThemeColors.AvatarFallbackHex</c>). This pins the pair so a
    /// future edit to one without the other fails loudly instead of
    /// silently drifting.
    /// </summary>
    [Fact]
    public void AvatarFallbackMatchesItsHexConstant()
    {
        Assert.Equal("#4682B4", ThemeColors.AvatarFallbackHex);
        Assert.Equal((byte)255, ThemeColors.AvatarFallback.A);
        Assert.Equal((byte)0x46, ThemeColors.AvatarFallback.R);
        Assert.Equal((byte)0x82, ThemeColors.AvatarFallback.G);
        Assert.Equal((byte)0xB4, ThemeColors.AvatarFallback.B);
    }

    /// <summary>
    /// Every alpha-variant color must derive from the palette above, never
    /// a WinUI system color — this is the invariant the class doc-comment
    /// promises. Checked for the one alpha variant that shares its base
    /// with the avatar fallback's sibling tokens: <c>SelectedBackground</c>
    /// derived from <c>Info</c>.
    /// </summary>
    [Fact]
    public void SelectedBackgroundKeepsInfosChannelsWithReducedAlpha()
    {
        Assert.Equal((byte)32, ThemeColors.SelectedBackground.A);
        Assert.Equal(ThemeColors.Info.R, ThemeColors.SelectedBackground.R);
        Assert.Equal(ThemeColors.Info.G, ThemeColors.SelectedBackground.G);
        Assert.Equal(ThemeColors.Info.B, ThemeColors.SelectedBackground.B);
    }

    /// <summary>
    /// The avatar-initials fill must stay a muted overlay of
    /// <c>Neutral</c>, never a literal color — the invariant this class's
    /// doc-comment promises for every alpha variant.
    /// </summary>
    [Fact]
    public void SecondaryContainerKeepsNeutralsChannelsWithReducedAlpha()
    {
        Assert.Equal((byte)40, ThemeColors.SecondaryContainer.A);
        Assert.Equal(ThemeColors.Neutral.R, ThemeColors.SecondaryContainer.R);
        Assert.Equal(ThemeColors.Neutral.G, ThemeColors.SecondaryContainer.G);
        Assert.Equal(ThemeColors.Neutral.B, ThemeColors.SecondaryContainer.B);
    }

    /// <summary>
    /// Core has no dedicated focus-ring token yet, so this reuses
    /// <c>Info</c> rather than a second hand-copied blue. If the two ever
    /// diverge, a future edit changed one without the other.
    /// </summary>
    [Fact]
    public void FocusRingReusesInfoUntilCoreExposesItsOwnToken()
    {
        Assert.Equal(ThemeColors.Info.A, ThemeColors.FocusRing.A);
        Assert.Equal(ThemeColors.Info.R, ThemeColors.FocusRing.R);
        Assert.Equal(ThemeColors.Info.G, ThemeColors.FocusRing.G);
        Assert.Equal(ThemeColors.Info.B, ThemeColors.FocusRing.B);
    }

    public static TheoryData<Color, byte, byte, byte> PaletteColorsWithExpectedChannels() =>
        new()
        {
            // Catppuccin Mocha hexes from the class doc-comment — a typo in
            // any literal parses to a plausible but wrong color, which
            // `ParseHex`'s magenta fallback only catches for malformed
            // *syntax*, not wrong-but-valid hex digits.
            { ThemeColors.Destructive, 0xf3, 0x8b, 0xa8 },
            { ThemeColors.Success, 0xa6, 0xe3, 0xa1 },
            { ThemeColors.Warning, 0xfa, 0xb3, 0x87 },
            { ThemeColors.Info, 0x89, 0xb4, 0xfa },
            { ThemeColors.Neutral, 0xa6, 0xad, 0xc8 },
            { ThemeColors.OnColored, 0xcd, 0xd6, 0xf4 },
        };

    [Theory]
    [MemberData(nameof(PaletteColorsWithExpectedChannels))]
    public void PaletteColorsParseToTheirDocumentedRgbChannels(Color color, byte r, byte g, byte b)
    {
        Assert.Equal((byte)255, color.A);
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }
}
