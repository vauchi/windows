// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// Core may send a display code's <c>placement</c> (its side and the
/// offset of its top-left corner, in permille of the node's square) and
/// its <c>error_correction</c> level. Absent placement means the code
/// fills the square; absent level means medium. The shell draws where it
/// is told and never outside its own square (vauchi/private#450, as on
/// iOS/macOS).
///
/// Traces to: features/generic_presentation_protocol.feature
/// </summary>
public class PresentationQrPlacementTests
{
    private static JsonElement QrPayload(string? placement = null, string? errorCorrection = null)
    {
        string field = (placement is null ? "" : $"\"placement\": {placement},")
            + (errorCorrection is null ? "" : $"\"error_correction\": \"{errorCorrection}\",");
        string json = $$"""
        {
          "id": "own_qr",
          "payloads": ["FRAME"],
          "purpose": "display",
          {{field}}
          "label": null
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void APlacementIsDecodedInPermille()
    {
        JsonElement payload = QrPayload(placement: """{"size": 650, "x": 350, "y": 175}""");

        Assert.Equal(new QrPlacement(650, 350, 175), QrPlacement.FromJson(payload));
    }

    [Fact]
    public void AnAbsentPlacementLeavesTheCodeFillingItsSquare()
    {
        Assert.Null(QrPlacement.FromJson(QrPayload()));
    }

    /// <summary>Core never sends this, but a malformed value must not throw.</summary>
    [Fact]
    public void APlacementThatIsNotAnObjectDecodesToNull()
    {
        Assert.Null(QrPlacement.FromJson(QrPayload(placement: "\"not_an_object\"")));
    }

    [Fact]
    public void AFullSquareDrawsTheCodeEdgeToEdge()
    {
        Assert.Equal(
            new QrFrameSpec(250, 0, 0),
            QrFrameSpec.For(null, 250));
    }

    [Fact]
    public void APlacedCodeIsScaledAndOffsetWithinTheSquare()
    {
        Assert.Equal(
            new QrFrameSpec(162.5, 87.5, 43.75),
            QrFrameSpec.For(new QrPlacement(650, 350, 175), 250));
        Assert.Equal(
            new QrFrameSpec(200, 50, 0),
            QrFrameSpec.For(new QrPlacement(800, 200, 0), 250));
    }

    /// <summary>Core never sends these; the shell still must not draw past its node.</summary>
    [Fact]
    public void APlacementReachingOutsideTheSquareIsPulledBackInside()
    {
        Assert.Equal(
            new QrFrameSpec(200, 50, 50),
            QrFrameSpec.For(new QrPlacement(800, 900, 5000), 250));
        Assert.Equal(
            new QrFrameSpec(250, 0, 0),
            QrFrameSpec.For(new QrPlacement(4000, 10, 10), 250));
        Assert.Equal(
            new QrFrameSpec(125, 0, 0),
            QrFrameSpec.For(new QrPlacement(500, -20, -1), 250));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ANonsensicalSizeFallsBackToTheFullSquare(int size)
    {
        Assert.Equal(
            new QrFrameSpec(250, 0, 0),
            QrFrameSpec.For(new QrPlacement(size, 0, 0), 250));
    }

    [Fact]
    public void ALowErrorCorrectionLevelIsDecodedAndDrawnAsSuch()
    {
        JsonElement payload = QrPayload(errorCorrection: "low");

        Assert.Equal("low", PresentationQrCorrection.FromJson(payload));
        Assert.Equal(
            ZXing.QrCode.Internal.ErrorCorrectionLevel.L,
            PresentationQrCorrection.Level(PresentationQrCorrection.FromJson(payload)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("medium")]
    [InlineData("ultra")]
    public void AnAbsentOrUnrecognisedLevelDrawsAtMedium(string? errorCorrection)
    {
        Assert.Equal(
            ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
            PresentationQrCorrection.Level(errorCorrection));
    }

    [Fact]
    public void AnAbsentLevelDecodesToNull()
    {
        Assert.Null(PresentationQrCorrection.FromJson(QrPayload()));
    }
}
