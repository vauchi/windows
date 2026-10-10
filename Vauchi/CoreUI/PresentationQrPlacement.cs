// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>
/// Where a display code sits in its node's square, in permille: its side
/// and its top-left corner (vauchi/private#450).
/// </summary>
public readonly record struct QrPlacement(int Size, int X, int Y)
{
    /// <summary>
    /// Decodes Core's optional <c>placement</c> object, or <c>null</c> when
    /// Core left it out — the code then fills the square — or sent
    /// something that is not the object Core's own schema produces.
    /// </summary>
    public static QrPlacement? FromJson(JsonElement payload)
    {
        if (!payload.TryGetProperty("placement", out JsonElement placement)
            || placement.ValueKind != JsonValueKind.Object)
            return null;
        return new QrPlacement(
            IntProperty(placement, "size"),
            IntProperty(placement, "x"),
            IntProperty(placement, "y"));
    }

    private static int IntProperty(JsonElement value, string property) =>
        value.TryGetProperty(property, out JsonElement element)
        && PresentationJson.Int32(element) is int number
            ? number
            : 0;
}

/// <summary>
/// A code's side and top-left corner inside a square, in device-independent
/// pixels. Pure, so <c>PresentationQrPlacementTests</c> can assert the
/// numbers without a XAML host, which this test project deliberately has
/// none of.
/// </summary>
public readonly record struct QrFrameSpec(double Side, double Left, double Top)
{
    private const int FullSquare = 1000;

    /// <summary>
    /// No placement, or a non-positive size, is the full square. A
    /// placement reaching outside the square is pulled back in, so the
    /// code is never drawn past its node.
    /// </summary>
    public static QrFrameSpec For(QrPlacement? placement, double squareSide)
    {
        if (placement is not { Size: > 0 } value)
            return new QrFrameSpec(squareSide, 0, 0);

        int size = Math.Min(value.Size, FullSquare);
        int room = FullSquare - size;
        // Multiply before dividing: 650 * 250 / 1000 is exact.
        double Scaled(int permille) => (double)permille * squareSide / FullSquare;

        return new QrFrameSpec(
            Scaled(size),
            Scaled(Math.Clamp(value.X, 0, room)),
            Scaled(Math.Clamp(value.Y, 0, room)));
    }
}

/// <summary>
/// The error-correction level to draw a display code at. Absent or
/// unrecognised draws at medium, the level this shell drew before Core
/// could ask for less (vauchi/private#450).
/// </summary>
public static class PresentationQrCorrection
{
    public static string? FromJson(JsonElement payload) =>
        payload.TryGetProperty("error_correction", out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    public static ZXing.QrCode.Internal.ErrorCorrectionLevel Level(string? errorCorrection) =>
        errorCorrection == "low"
            ? ZXing.QrCode.Internal.ErrorCorrectionLevel.L
            : ZXing.QrCode.Internal.ErrorCorrectionLevel.M;
}
