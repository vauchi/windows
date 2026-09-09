// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// <c>RenderImage</c> read <c>shape</c> only on the picture branch, and
/// dropped the initials into a bare <c>TextBlock</c> with no size and no
/// fill — so a contact with no avatar showed a stray letter on the page.
///
/// The decisions are asserted here rather than on the rendered element:
/// a WinUI <c>TextBlock</c> cannot be constructed without a XAML host, and
/// this project deliberately has none.
/// </summary>
public class PresentationImageShapeTests
{
    [Fact]
    public void ImageBytesWinOverTheInitialsCorePrepared()
    {
        Assert.Equal(
            PresentationImageVisual.Picture,
            PresentationImageShape.Visual(new byte[] { 1, 2, 3 }, "BS"));
    }

    [Fact]
    public void InitialsStandInWhenThereAreNoBytes()
    {
        Assert.Equal(
            PresentationImageVisual.Initials,
            PresentationImageShape.Visual(null, "BS"));
    }

    /// <summary>
    /// Core sends an empty array for a cleared avatar. Counting that as a
    /// picture drew an empty frame where the initials belonged.
    /// </summary>
    [Fact]
    public void EmptyBytesFallThroughToTheInitials()
    {
        Assert.Equal(
            PresentationImageVisual.Initials,
            PresentationImageShape.Visual(System.Array.Empty<byte>(), "BS"));
    }

    /// <summary>
    /// Guards the rest: with nothing to show, nothing is drawn. Without
    /// this a renderer that always emitted a filled square would satisfy
    /// every other assertion here.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NothingToShowResolvesToNothing(string? fallbackText)
    {
        Assert.Equal(
            PresentationImageVisual.Nothing,
            PresentationImageShape.Visual(null, fallbackText));
    }

    [Fact]
    public void ACircleIsRoundedToHalfItsSide()
    {
        Assert.True(PresentationImageShape.IsCircular("circle"));
        Assert.Equal(
            PresentationImageShape.AvatarSide / 2,
            PresentationImageShape.CornerRadiusFor("circle"));
    }

    /// <summary>
    /// A natural image keeps its corners. Rounding them to half the side
    /// would make a diagram look like a face.
    /// </summary>
    [Theory]
    [InlineData("natural")]
    [InlineData(null)]
    [InlineData("some_shape_this_build_has_not_learned")]
    public void EverythingElseKeepsItsCorners(string? shape)
    {
        Assert.False(PresentationImageShape.IsCircular(shape));
        Assert.Equal(
            PresentationImageShape.NaturalCornerRadius,
            PresentationImageShape.CornerRadiusFor(shape));
    }
}
