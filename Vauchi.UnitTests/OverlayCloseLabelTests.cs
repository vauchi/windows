// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

// The overlay close button's label (vauchi/private#517): Core sends an
// optional `close_label` on every overlay it composes; an overlay from an
// older Core has none and the button falls back to the localized word.
public class OverlayCloseLabelTests
{
    private static JsonElement Overlay(string closeLabel) =>
        JsonDocument.Parse(
            "{\"kind\":\"information\",\"title\":\"About\",\"items\":[]" + closeLabel + "}")
            .RootElement.Clone();

    // @scenario: generic_presentation_protocol.feature :: Every shell renders the same prepared presentation
    [Fact]
    public void For_WithCoresCloseLabel_ReturnsIt()
    {
        JsonElement overlay = Overlay(",\"close_label\":\"Schließen\"");

        Assert.Equal("Schließen", OverlayCloseLabel.For(overlay, _ => "fallback"));
    }

    // @scenario: generic_presentation_protocol.feature :: Every shell renders the same prepared presentation
    [Fact]
    public void For_OfAnOverlayFromAnOlderCore_FallsBackToTheLocalizedWord()
    {
        Assert.Equal("fallback", OverlayCloseLabel.For(Overlay(""), _ => "fallback"));
        Assert.Equal("fallback", OverlayCloseLabel.For(Overlay(",\"close_label\":null"), _ => "fallback"));
    }

    // @scenario: generic_presentation_protocol.feature :: Every shell renders the same prepared presentation
    [Fact]
    public void For_WithAnEmptyCloseLabel_FallsBackToTheLocalizedWord()
    {
        Assert.Equal("fallback", OverlayCloseLabel.For(Overlay(",\"close_label\":\"\""), _ => "fallback"));
    }
}
