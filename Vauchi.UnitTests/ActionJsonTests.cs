// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// Verify ActionJson produces valid JSON matching the vauchi-cabi serde format.
/// Format: {"VariantName":{"field":"value"}}
/// </summary>
public class ActionJsonTests
{
    [Fact]
    public void ActionPressed_MatchesSerdeFormat()
    {
        string json = ActionJson.ActionPressed("create_new");

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ActionPressed", out var inner));
        Assert.Equal("create_new", inner.GetProperty("action_id").GetString());
    }

    [Fact]
    public void ActionPressed_EscapesQuotesInId()
    {
        string json = ActionJson.ActionPressed("action\"with\"quotes");

        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("ActionPressed");
        Assert.Equal("action\"with\"quotes", inner.GetProperty("action_id").GetString());
    }

    [Fact]
    public void ActionPressed_HandlesEmptyId()
    {
        string json = ActionJson.ActionPressed("");

        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("ActionPressed");
        Assert.Equal("", inner.GetProperty("action_id").GetString());
    }

    [Fact]
    public void TextChanged_MatchesSerdeFormat()
    {
        string json = ActionJson.TextChanged("name_input", "Alice");

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("TextChanged", out var inner));
        Assert.Equal("name_input", inner.GetProperty("component_id").GetString());
        Assert.Equal("Alice", inner.GetProperty("value").GetString());
    }

    [Fact]
    public void TextChanged_EscapesSpecialCharacters()
    {
        string json = ActionJson.TextChanged("input", "line1\nline2\ttab\\back\"quote");

        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("TextChanged");
        Assert.Equal("line1\nline2\ttab\\back\"quote", inner.GetProperty("value").GetString());
    }

    [Fact]
    public void TextChanged_HandlesUnicode()
    {
        string json = ActionJson.TextChanged("input", "日本語テスト 🎉");

        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("TextChanged");
        Assert.Equal("日本語テスト 🎉", inner.GetProperty("value").GetString());
    }

    [Fact]
    public void TextChanged_HandlesEmptyValue()
    {
        string json = ActionJson.TextChanged("input", "");

        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("TextChanged");
        Assert.Equal("", inner.GetProperty("value").GetString());
    }

    [Fact]
    public void ItemToggled_MatchesSerdeFormat()
    {
        string json = ActionJson.ItemToggled("toggle_list", "item_1");

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ItemToggled", out var inner));
        Assert.Equal("toggle_list", inner.GetProperty("component_id").GetString());
        Assert.Equal("item_1", inner.GetProperty("item_id").GetString());
    }

    [Fact]
    public void ListItemSelected_MatchesSerdeFormat()
    {
        string json = ActionJson.ListItemSelected("contact_list", "contact_abc");

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ListItemSelected", out var inner));
        Assert.Equal("contact_list", inner.GetProperty("component_id").GetString());
        Assert.Equal("contact_abc", inner.GetProperty("item_id").GetString());
    }

    [Fact]
    public void SettingsToggled_MatchesSerdeFormat()
    {
        string json = ActionJson.SettingsToggled("settings", "notifications");

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("SettingsToggled", out var inner));
        Assert.Equal("settings", inner.GetProperty("component_id").GetString());
        Assert.Equal("notifications", inner.GetProperty("item_id").GetString());
    }

    [Fact]
    public void AllVariants_ProduceValidJson()
    {
        // Verify all builders produce parseable JSON (no exceptions)
        string[] jsons =
        [
            ActionJson.ActionPressed("test"),
            ActionJson.TextChanged("id", "val"),
            ActionJson.ItemToggled("id", "item"),
            ActionJson.ListItemSelected("id", "item"),
            ActionJson.SettingsToggled("id", "item"),
        ];

        foreach (string json in jsons)
        {
            var doc = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
    }
}
