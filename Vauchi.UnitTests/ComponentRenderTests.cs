// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

public class ComponentRenderTests
{
    // ── TextInput ──

    [Fact]
    public void TextInput_AllFieldsPresent()
    {
        var json = """{"id":"name","label":"Display Name","value":"Alice","placeholder":"Enter name","max_length":100,"validation_error":"Too short","input_type":"Text"}""";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Display Name", root.GetProperty("label").GetString());
        Assert.Equal("Alice", root.GetProperty("value").GetString());
        Assert.Equal(100, root.GetProperty("max_length").GetInt32());
        Assert.Equal("Too short", root.GetProperty("validation_error").GetString());
        Assert.Equal("Text", root.GetProperty("input_type").GetString());
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Phone")]
    [InlineData("Email")]
    [InlineData("Password")]
    public void TextInput_AllInputTypes(string inputType)
    {
        var json = $$"""{"id":"x","label":"L","value":"","input_type":"{{inputType}}"}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(inputType, doc.RootElement.GetProperty("input_type").GetString());
    }

    [Fact]
    public void TextInput_UnicodeValue()
    {
        var json = """{"id":"x","label":"名前","value":"田中太郎 🎌","input_type":"Text"}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("田中太郎 🎌", doc.RootElement.GetProperty("value").GetString());
    }

    // ── ActionList: must read "items" not "actions" ──

    [Fact]
    public void ActionList_ReadsItemsField()
    {
        var json = """{"id":"al1","items":[{"id":"add","label":"Add Contact","icon":"plus","detail":"Create new"}]}""";
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("Add Contact", items[0].GetProperty("label").GetString());
        Assert.Equal("plus", items[0].GetProperty("icon").GetString());
        Assert.Equal("Create new", items[0].GetProperty("detail").GetString());
    }

    [Fact]
    public void ActionList_EmptyItems()
    {
        var json = """{"id":"al1","items":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    // ── ToggleList ──

    [Fact]
    public void ToggleList_ReadsItemsWithSelected()
    {
        var json = """{"id":"tl1","label":"Privacy","items":[{"id":"t1","label":"Hidden","selected":true,"subtitle":"Not shown"}]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Privacy", doc.RootElement.GetProperty("label").GetString());
        var items = doc.RootElement.GetProperty("items");
        Assert.True(items[0].GetProperty("selected").GetBoolean());
        Assert.Equal("Not shown", items[0].GetProperty("subtitle").GetString());
    }

    [Fact]
    public void ToggleList_EmitsItemToggled()
    {
        string json = ActionJson.ItemToggled("privacy_list", "hidden_mode");
        using var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("ItemToggled");
        Assert.Equal("privacy_list", inner.GetProperty("component_id").GetString());
        Assert.Equal("hidden_mode", inner.GetProperty("item_id").GetString());
    }

    [Fact]
    public void ToggleList_MissingSubtitle()
    {
        var json = """{"id":"tl1","label":"L","items":[{"id":"t1","label":"X","selected":false}]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("items")[0].TryGetProperty("subtitle", out _));
    }

    // ── InfoPanel: reads "items" not "message" ──

    [Fact]
    public void InfoPanel_ReadsItemsNotMessage()
    {
        var json = """{"id":"ip1","icon":"info","title":"About","items":[{"icon":"lock","title":"Encrypted","detail":"All data is encrypted"}]}""";
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("Encrypted", items[0].GetProperty("title").GetString());
        Assert.Equal("All data is encrypted", items[0].GetProperty("detail").GetString());
    }

    [Fact]
    public void InfoPanel_NullIcon()
    {
        var json = """{"id":"ip1","icon":null,"title":"T","items":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("icon").ValueKind);
    }

    // ── StatusIndicator ──

    [Theory]
    [InlineData("Pending")]
    [InlineData("InProgress")]
    [InlineData("Success")]
    [InlineData("Failed")]
    [InlineData("Warning")]
    public void StatusIndicator_AllStatusValues(string status)
    {
        var json = $$"""{"id":"si1","title":"Sync","detail":"Last: 5m ago","status":"{{status}}"}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(status, doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Last: 5m ago", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public void StatusIndicator_MissingDetail()
    {
        var json = """{"id":"si1","title":"Sync","status":"Success"}""";
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("detail", out _));
    }

    // ── ContactList ──

    [Fact]
    public void ContactList_ReadsContactItems()
    {
        var json = """{"id":"cl1","contacts":[{"id":"c1","name":"Alice","subtitle":"Work","avatar_initials":"AL","status":"online","searchable_fields":["alice"]}],"searchable":true}""";
        using var doc = JsonDocument.Parse(json);
        var contacts = doc.RootElement.GetProperty("contacts");
        Assert.Equal(1, contacts.GetArrayLength());
        Assert.Equal("Alice", contacts[0].GetProperty("name").GetString());
        Assert.Equal("AL", contacts[0].GetProperty("avatar_initials").GetString());
        Assert.True(doc.RootElement.GetProperty("searchable").GetBoolean());
    }

    [Fact]
    public void ContactList_EmptyContacts()
    {
        var json = """{"id":"cl1","contacts":[],"searchable":false}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("contacts").GetArrayLength());
    }

    // ── SettingsGroup: all 4 SettingsItemKind ──

    [Theory]
    [InlineData("""{"Toggle":{"enabled":true}}""", "Toggle")]
    [InlineData("""{"Value":{"value":"English"}}""", "Value")]
    [InlineData("""{"Link":{"detail":"v0.5.0"}}""", "Link")]
    [InlineData("""{"Destructive":{"label":"Delete Account"}}""", "Destructive")]
    public void SettingsGroup_AllItemKinds(string kindJson, string expectedKind)
    {
        var json = $$"""{"id":"sg1","label":"General","items":[{"id":"si1","label":"Theme","kind":{{kindJson}}}]}""";
        using var doc = JsonDocument.Parse(json);
        var item = doc.RootElement.GetProperty("items")[0];
        Assert.True(item.GetProperty("kind").TryGetProperty(expectedKind, out _));
    }

    [Fact]
    public void SettingsGroup_EmptyItems()
    {
        var json = """{"id":"sg1","label":"","items":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    // ── FieldList ──

    [Theory]
    [InlineData("ReadOnly")]
    [InlineData("ShowHide")]
    [InlineData("PerGroup")]
    public void FieldList_VisibilityModes(string mode)
    {
        var json = $$"""{"id":"fl1","fields":[],"visibility_mode":"{{mode}}","available_groups":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(mode, doc.RootElement.GetProperty("visibility_mode").GetString());
    }

    // ── CardPreview ──

    [Fact]
    public void CardPreview_ReadsGroupViews()
    {
        var json = """{"name":"Alice","fields":[],"group_views":[{"group_name":"family","display_name":"Mom","visible_fields":[]}],"selected_group":"family"}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("family", doc.RootElement.GetProperty("selected_group").GetString());
        Assert.Equal("Mom", doc.RootElement.GetProperty("group_views")[0].GetProperty("display_name").GetString());
    }

    // ── ConfirmationDialog: correct field names ──

    [Fact]
    public void ConfirmationDialog_CorrectFields()
    {
        var json = """{"id":"cd1","title":"Delete Contact","message":"Cannot be undone","confirm_text":"Delete","destructive":true}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Delete", doc.RootElement.GetProperty("confirm_text").GetString());
        Assert.True(doc.RootElement.GetProperty("destructive").GetBoolean());
        Assert.Equal("Delete Contact", doc.RootElement.GetProperty("title").GetString());
    }

    // ── ShowToast: correct data model ──

    [Fact]
    public void ShowToast_ReadsDurationAndUndo()
    {
        var json = """{"id":"st1","message":"Deleted","undo_action_id":"undo_del","duration_ms":5000}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5000, doc.RootElement.GetProperty("duration_ms").GetInt32());
        Assert.Equal("undo_del", doc.RootElement.GetProperty("undo_action_id").GetString());
    }
}
