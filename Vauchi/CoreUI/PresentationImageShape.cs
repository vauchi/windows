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
    /// Side of a standalone avatar's circular clip when Core leaves sizing
    /// to the shell — every avatar today. Core names an explicit `size`
    /// only for the onboarding mark.
    /// </summary>
    public const double AvatarSide = 160;

    /// <summary>Cap for a free-form picture when Core leaves sizing to the shell.</summary>
    public const double UnsizedPictureCap = 240;

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
    /// The square side Core asked for, or the shell's own default for the
    /// element asking (the free-form cap, the avatar clip side, the
    /// initials diameter) when Core left sizing to the shell.
    /// </summary>
    public static double SideFor(int? size, double unsizedDefault) => size ?? unsizedDefault;

    /// <summary>
    /// A circle is half the side. Anything else keeps its corners, because
    /// rounding them away loses what distinguishes a diagram from a face.
    /// </summary>
    public static double CornerRadiusFor(string? shape) => CornerRadiusFor(shape, AvatarSide);

    /// <summary>Same rule as <see cref="CornerRadiusFor(string?)"/>, for a side Core named.</summary>
    public static double CornerRadiusFor(string? shape, double side) =>
        IsCircular(shape) ? side / 2 : NaturalCornerRadius;
}
