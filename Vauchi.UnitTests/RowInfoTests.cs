// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

// A row can explain its item (vauchi/private#479): Core sends an optional
// `info` action named "About <item>". A row from an older Core has none.
public class RowInfoTests
{
    private static JsonElement Row(string info) =>
        JsonDocument.Parse(
            "{\"title\":\"Home address\",\"secondary_actions\":[],\"controls\":[]" + info + "}")
            .RootElement.Clone();

    // @scenario: generic_presentation_protocol.feature :: Every shell renders the same prepared presentation
    [Fact]
    public void Read_ReturnsCoresInfoAction()
    {
        RowInfo? info = RowInfo.Read(Row(
            ",\"info\":{\"interaction_id\":\"surface.7.interaction.3\",\"label\":\"Info\"," +
            "\"accessibility_label\":\"About Home address\",\"enabled\":true}"));

        Assert.NotNull(info);
        Assert.Equal("surface.7.interaction.3", info!.InteractionId);
        Assert.Equal("About Home address", info.AccessibilityLabel);
        Assert.True(info.Enabled);
    }

    // @scenario: generic_presentation_protocol.feature :: Every shell renders the same prepared presentation
    [Fact]
    public void Read_OfARowFromAnOlderCore_IsNull()
    {
        Assert.Null(RowInfo.Read(Row("")));
        Assert.Null(RowInfo.Read(Row(",\"info\":null")));
    }
}
