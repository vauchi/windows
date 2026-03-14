// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.Helpers;
using Xunit;

namespace Vauchi.Tests;

/// <summary>
/// Verifies that UserAction JSON formats match the expected structure
/// consumed by the Rust core engine.
/// </summary>
public class ActionJsonTests
{
    [Fact]
    public void TextChanged_SerializesCorrectly()
    {
        var json = JsonSerializer.Serialize(new
        {
            TextChanged = new { component_id = "name_input", value = "Alice" }
        });

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("TextChanged", out var tc));
        Assert.Equal("name_input", tc.GetProperty("component_id").GetString());
        Assert.Equal("Alice", tc.GetProperty("value").GetString());
    }

    [Fact]
    public void ItemToggled_SerializesCorrectly()
    {
        var json = JsonSerializer.Serialize(new
        {
            ItemToggled = new { component_id = "visibility_list", item_id = "phone" }
        });

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ItemToggled", out var it));
        Assert.Equal("visibility_list", it.GetProperty("component_id").GetString());
        Assert.Equal("phone", it.GetProperty("item_id").GetString());
    }

    [Fact]
    public void ActionPressed_SerializesCorrectly()
    {
        var json = JsonSerializer.Serialize(new
        {
            ActionPressed = new { action_id = "continue" }
        });

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ActionPressed", out var ap));
        Assert.Equal("continue", ap.GetProperty("action_id").GetString());
    }

    [Fact]
    public void FieldVisibilityChanged_WithNullGroupId()
    {
        // group_id can be null for top-level fields
        var json = """{"FieldVisibilityChanged": {"component_id": "email", "group_id": null, "visible": true}}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("FieldVisibilityChanged", out var fvc));
        Assert.Equal("email", fvc.GetProperty("component_id").GetString());
        Assert.Equal(JsonValueKind.Null, fvc.GetProperty("group_id").ValueKind);
        Assert.True(fvc.GetProperty("visible").GetBoolean());
    }

    [Fact]
    public void GroupViewSelected_WithNullGroupName()
    {
        var json = """{"GroupViewSelected": {"component_id": "card_preview", "group_name": null}}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("GroupViewSelected", out var gvs));
        Assert.Equal("card_preview", gvs.GetProperty("component_id").GetString());
        Assert.Equal(JsonValueKind.Null, gvs.GetProperty("group_name").ValueKind);
    }

    [Fact]
    public void UndoPressed_SerializesCorrectly()
    {
        var json = JsonSerializer.Serialize(new
        {
            UndoPressed = new { action_id = "undo_delete" }
        });

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("UndoPressed", out var up));
        Assert.Equal("undo_delete", up.GetProperty("action_id").GetString());
    }

    // ── ActionJson helper tests ─────────────────────────────────

    [Fact]
    public void Helper_ActionPressed_MatchesSerdeFormat()
    {
        string json = ActionJson.ActionPressed("create_new");
        var doc = JsonDocument.Parse(json);
        Assert.Equal("create_new", doc.RootElement.GetProperty("ActionPressed").GetProperty("action_id").GetString());
    }

    [Fact]
    public void Helper_ActionPressed_EscapesQuotes()
    {
        string json = ActionJson.ActionPressed("action\"with\"quotes");
        var doc = JsonDocument.Parse(json);
        Assert.Equal("action\"with\"quotes", doc.RootElement.GetProperty("ActionPressed").GetProperty("action_id").GetString());
    }

    [Fact]
    public void Helper_TextChanged_EscapesSpecialCharacters()
    {
        string json = ActionJson.TextChanged("input", "line1\nline2\ttab\\back\"quote");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("TextChanged");
        Assert.Equal("line1\nline2\ttab\\back\"quote", inner.GetProperty("value").GetString());
    }

    [Fact]
    public void Helper_TextChanged_HandlesUnicode()
    {
        string json = ActionJson.TextChanged("input", "日本語テスト");
        var doc = JsonDocument.Parse(json);
        Assert.Equal("日本語テスト", doc.RootElement.GetProperty("TextChanged").GetProperty("value").GetString());
    }

    [Fact]
    public void Helper_ItemToggled_MatchesSerdeFormat()
    {
        string json = ActionJson.ItemToggled("toggle_list", "item_1");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("ItemToggled");
        Assert.Equal("toggle_list", inner.GetProperty("component_id").GetString());
        Assert.Equal("item_1", inner.GetProperty("item_id").GetString());
    }

    [Fact]
    public void Helper_ListItemSelected_MatchesSerdeFormat()
    {
        string json = ActionJson.ListItemSelected("contact_list", "contact_abc");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("ListItemSelected");
        Assert.Equal("contact_list", inner.GetProperty("component_id").GetString());
        Assert.Equal("contact_abc", inner.GetProperty("item_id").GetString());
    }

    [Fact]
    public void Helper_SettingsToggled_MatchesSerdeFormat()
    {
        string json = ActionJson.SettingsToggled("settings", "notifications");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("SettingsToggled");
        Assert.Equal("settings", inner.GetProperty("component_id").GetString());
        Assert.Equal("notifications", inner.GetProperty("item_id").GetString());
    }
}
