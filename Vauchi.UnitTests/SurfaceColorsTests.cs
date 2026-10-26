// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Windows.UI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// The shell paints the colours Core sends in <c>tokens.colors</c>, not a
/// palette of its own (vauchi/private#814).
/// </summary>
public class SurfaceColorsTests
{
    private const string Required = """
        "background":"#1e1e2e","surface":"#181825","text":"#cdd6f4",
        "text_muted":"#a6adc8","accent":"#89b4fa","border":"#45475a",
        "success":"#a6e3a1","warning":"#fab387","error":"#f38ba8"
        """;

    private static JsonElement Surface(string colors) =>
        JsonDocument.Parse(
            "{\"surface_id\":\"s\",\"tokens\":{\"spacing_small\":8" + colors + "}}")
            .RootElement.Clone();

    private static JsonElement WithColors(string roles) =>
        Surface(",\"colors\":{" + roles + "}");

    private static Color Rgb(byte r, byte g, byte b) => Color.FromArgb(255, r, g, b);

    [Fact]
    public void EveryRequiredRoleIsPaintedAsCoreSentIt()
    {
        SurfaceColors colors = SurfaceColors.From(WithColors(Required))!;

        Assert.Equal(Rgb(0x1e, 0x1e, 0x2e), colors.Background);
        Assert.Equal(Rgb(0x18, 0x18, 0x25), colors.Surface);
        Assert.Equal(Rgb(0xcd, 0xd6, 0xf4), colors.Text);
        Assert.Equal(Rgb(0xa6, 0xad, 0xc8), colors.TextMuted);
        Assert.Equal(Rgb(0x89, 0xb4, 0xfa), colors.Accent);
        Assert.Equal(Rgb(0x45, 0x47, 0x5a), colors.Border);
        Assert.Equal(Rgb(0xa6, 0xe3, 0xa1), colors.Success);
        Assert.Equal(Rgb(0xfa, 0xb3, 0x87), colors.Warning);
        Assert.Equal(Rgb(0xf3, 0x8b, 0xa8), colors.Error);
    }

    [Fact]
    public void AnOptionalRoleTheThemeLeavesOutPaintsItsNearestRequiredRole()
    {
        SurfaceColors colors = SurfaceColors.From(WithColors(Required + """
            ,"surface_input":null,"surface_muted":null,"surface_elevated":null,
            "text_on_accent":null,"focus_ring":null
            """))!;

        Assert.Equal(colors.Surface, colors.SurfaceInput);
        Assert.Equal(colors.Surface, colors.SurfaceMuted);
        Assert.Equal(colors.Surface, colors.SurfaceElevated);
        Assert.Equal(colors.Text, colors.TextOnAccent);
        Assert.Equal(colors.Accent, colors.FocusRing);
    }

    [Fact]
    public void AnOptionalRoleTheThemeSetsIsPainted()
    {
        SurfaceColors colors = SurfaceColors.From(WithColors(Required + """
            ,"surface_input":"#313244","surface_muted":"#11111b","surface_elevated":"#45475a",
            "text_on_accent":"#11111b","focus_ring":"#f5c2e7"
            """))!;

        Assert.Equal(Rgb(0x31, 0x32, 0x44), colors.SurfaceInput);
        Assert.Equal(Rgb(0x11, 0x11, 0x1b), colors.SurfaceMuted);
        Assert.Equal(Rgb(0x45, 0x47, 0x5a), colors.SurfaceElevated);
        Assert.Equal(Rgb(0x11, 0x11, 0x1b), colors.TextOnAccent);
        Assert.Equal(Rgb(0xf5, 0xc2, 0xe7), colors.FocusRing);
    }

    [Fact]
    public void ASurfaceWithoutColoursHasNoPaletteSoTheSystemBrushesStay()
    {
        Assert.Null(SurfaceColors.From(Surface("")));
        Assert.Null(SurfaceColors.From(JsonDocument.Parse("{}").RootElement));
        Assert.Null(SurfaceColors.From(Surface(",\"colors\":null")));
    }

    [Theory]
    [InlineData("\"#12345\"")]
    [InlineData("\"red\"")]
    [InlineData("\"#gggggg\"")]
    [InlineData("\"#1234567\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("null")]
    public void AMalformedRequiredRoleDropsThePaletteWhole(string background)
    {
        string roles = Required.Replace("\"#1e1e2e\"", background);

        Assert.Null(SurfaceColors.From(WithColors(roles)));
    }

    [Fact]
    public void AMalformedOptionalRolePaintsItsNearestRequiredRole()
    {
        SurfaceColors colors = SurfaceColors.From(
            WithColors(Required + ",\"focus_ring\":\"blue\""))!;

        Assert.Equal(colors.Accent, colors.FocusRing);
    }

    [Fact]
    public void ARequiredRoleThatIsMissingDropsThePaletteWhole()
    {
        string roles = Required.Replace("\"error\":\"#f38ba8\"", "\"other\":\"#f38ba8\"");

        Assert.Null(SurfaceColors.From(WithColors(roles)));
    }

    [Fact]
    public void OverlayTintsKeepTheirRolesChannelsAtReducedAlpha()
    {
        SurfaceColors colors = SurfaceColors.From(WithColors(Required))!;

        Assert.Equal(Color.FromArgb(32, 0x89, 0xb4, 0xfa), colors.SelectedBackground);
        Assert.Equal(Color.FromArgb(160, 0x89, 0xb4, 0xfa), colors.ActiveSurfaceBorder);
        Assert.Equal(Color.FromArgb(64, 0xa6, 0xad, 0xc8), colors.Divider);
        Assert.Equal(Color.FromArgb(40, 0xa6, 0xad, 0xc8), colors.SecondaryContainer);
    }

    [Theory]
    [InlineData("#eff1f5", true)]
    [InlineData("#ffffff", true)]
    [InlineData("#1e1e2e", false)]
    [InlineData("#000000", false)]
    public void ALightBackgroundIsLightAndADarkOneIsNot(string background, bool light)
    {
        SurfaceColors colors = SurfaceColors.From(
            WithColors(Required.Replace("#1e1e2e", background)))!;

        Assert.Equal(light, colors.IsLight);
    }
}
