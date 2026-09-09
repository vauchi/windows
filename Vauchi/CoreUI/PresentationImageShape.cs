// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi.CoreUI;

/// <summary>What an <c>Image</c> node resolves to.</summary>
public enum PresentationImageVisual
{
    /// <summary>Core sent bytes and they are worth decoding.</summary>
    Picture,

    /// <summary>No picture, but Core prepared initials to stand in.</summary>
    Initials,

    /// <summary>Neither. Nothing is drawn.</summary>
    Nothing,
}

/// <summary>
/// The shape and content decisions for a Core <c>Image</c> node, taken
/// before any WinUI element exists so they can be asserted without a XAML
/// host. <c>RenderImage</c> honoured <c>shape</c> only for the picture
/// branch, and gave the initials fallback no size and no fill at all — so
/// a contact with no avatar got a bare letter on the page, the same defect
/// <c>ios!651</c> fixed on Apple.
/// </summary>
public static class PresentationImageShape
{
    /// <summary>
    /// Side of a standalone avatar. Core names no size for <c>Image</c>, so
    /// the shell picks one.
    /// </summary>
    public const double AvatarSide = 160;

    /// <summary>Radius for a natural-shaped image, which keeps its corners.</summary>
    public const double NaturalCornerRadius = 8;

    /// <summary>
    /// Empty bytes are not a picture: Core sends an empty array for a
    /// cleared avatar, and treating it as image data drew an empty frame
    /// where the initials belonged.
    /// </summary>
    public static PresentationImageVisual Visual(byte[]? data, string? fallbackText)
    {
        if (data is { Length: > 0 })
        {
            return PresentationImageVisual.Picture;
        }

        return string.IsNullOrEmpty(fallbackText)
            ? PresentationImageVisual.Nothing
            : PresentationImageVisual.Initials;
    }

    public static bool IsCircular(string? shape) => shape == "circle";

    /// <summary>
    /// A circle is half the side. Anything else keeps its corners, because
    /// rounding them away loses what distinguishes a diagram from a face.
    /// </summary>
    public static double CornerRadiusFor(string? shape) =>
        IsCircular(shape) ? AvatarSide / 2 : NaturalCornerRadius;
}
