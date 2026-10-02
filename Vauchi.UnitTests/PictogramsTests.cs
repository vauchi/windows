// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class PictogramsTests
{
    /// <summary>
    /// The exchange-mode pictograms Core names as
    /// <c>pictogram.exchange.&lt;name&gt;</c> (<c>mode_pictogram</c> in
    /// <c>vauchi-app/src/ui/exchange/mode_selection.rs</c>).
    /// </summary>
    public static TheoryData<string> ExchangePictogramTokens() => new()
    {
        "pictogram.exchange.glance",
        "pictogram.exchange.hover",
        "pictogram.exchange.bump",
        "pictogram.exchange.shake",
        "pictogram.exchange.magic",
        "pictogram.exchange.tap_tap",
        "pictogram.exchange.tap_hover_shake",
        "pictogram.exchange.link",
        "pictogram.exchange.cable",
    };

    [Theory]
    [MemberData(nameof(ExchangePictogramTokens))]
    public void EveryExchangePictogramIsBundledAsVisiblePaths(string token)
    {
        IReadOnlyList<PictogramPath>? paths = Pictograms.Resolve(token);

        Assert.NotNull(paths);
        Assert.NotEmpty(paths);
        Assert.All(paths, path =>
        {
            Assert.StartsWith("M", path.Data);
            Assert.True(path.Stroked || path.Filled, $"{token} has a path drawn with neither stroke nor fill");
        });
    }

    [Fact]
    public void HoverKeepsEveryPathWithItsOwnStrokeAndFill()
    {
        IReadOnlyList<PictogramPath> paths = Pictograms.Resolve("pictogram.exchange.hover")!;

        Assert.Equal(15, paths.Count);
        Assert.Equal(
            new PictogramPath(
                "M2.3 1.8L4.5 1.8A0.5 0.5 0 0 1 5 2.3L5 4.5A0.5 0.5 0 0 1 4.5 5L2.3 5A0.5 0.5 0 0 1 1.8 4.5L1.8 2.3A0.5 0.5 0 0 1 2.3 1.8Z",
                Stroked: true,
                StrokeThickness: 1.6,
                Filled: false),
            paths[0]);
        Assert.Equal(
            new PictogramPath(
                "M2.9 3.4A0.5 0.5 0 1 0 3.9 3.4A0.5 0.5 0 1 0 2.9 3.4Z",
                Stroked: false,
                StrokeThickness: 1,
                Filled: true),
            paths[1]);
        Assert.Equal(8, paths.Count(path => path.Filled));
    }

    [Theory]
    [InlineData("pictogram.exchange.not_a_mode")]
    [InlineData("pictogram.unknown_group.hover")]
    [InlineData("pictogram.exchange")]
    [InlineData("pictogram.exchange.hover.extra")]
    [InlineData("pictogram.exchange.../hover")]
    [InlineData("pictogram.exchange.HOVER")]
    [InlineData(" pictogram.exchange.hover")]
    [InlineData("exchange.hover")]
    [InlineData("person.2")]
    [InlineData("")]
    [InlineData(null)]
    public void AnyOtherTokenHasNoPictogram(string? token)
    {
        Assert.Null(Pictograms.Resolve(token));
    }

    private static JsonElement StatusPayload(string iconTokenJson) =>
        JsonDocument.Parse(
            "{\"title\":\"Hover\",\"detail\":null,\"icon_token\":" + iconTokenJson
            + ",\"badge\":null,\"tone\":\"neutral\",\"activation\":null}").RootElement;

    [Fact]
    public void ANodeCarryingAPictogramTokenDrawsThatPictogram()
    {
        Assert.Equal(
            Pictograms.Resolve("pictogram.exchange.hover"),
            Pictograms.ForNode(StatusPayload("\"pictogram.exchange.hover\"")));
    }

    [Theory]
    [InlineData("\"person.2\"")]
    [InlineData("\"pictogram.exchange.not_a_mode\"")]
    [InlineData("null")]
    [InlineData("42")]
    public void ANodeWithoutABundledPictogramTokenDrawsNoPictogram(string iconTokenJson)
    {
        Assert.Null(Pictograms.ForNode(StatusPayload(iconTokenJson)));
    }

    [Fact]
    public void ANodeWithNoIconTokenDrawsNoPictogram()
    {
        Assert.Null(Pictograms.ForNode(JsonDocument.Parse("{\"title\":\"Hover\"}").RootElement));
    }
}
