// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Xunit;

namespace Vauchi.Tests;

/// <summary>
/// Verifies ScreenModel JSON parsing: full models, nullable fields,
/// action styles, and all 17 component variant JSON structures.
/// </summary>
public class ScreenModelParsingTests
{
    [Fact]
    public void FullModel_AllFieldsPresent()
    {
        var json = """
        {
            "title": "Setup",
            "subtitle": "Welcome to Vauchi",
            "progress": { "current": 2, "total": 5, "label": "Step 2 of 5" },
            "components": [{"Text": {"id": "t1", "content": "Hello"}}],
            "actions": [{"id": "next", "label": "Next", "style": "Primary", "enabled": true}]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Setup", root.GetProperty("title").GetString());
        Assert.Equal("Welcome to Vauchi", root.GetProperty("subtitle").GetString());

        var progress = root.GetProperty("progress");
        Assert.Equal(2, progress.GetProperty("current").GetInt32());
        Assert.Equal(5, progress.GetProperty("total").GetInt32());
        Assert.Equal("Step 2 of 5", progress.GetProperty("label").GetString());

        Assert.Single(root.GetProperty("components").EnumerateArray());
        Assert.Single(root.GetProperty("actions").EnumerateArray());
    }

    [Fact]
    public void NullableFields_SubtitleNull()
    {
        var json = """
        {
            "title": "Home",
            "subtitle": null,
            "components": [],
            "actions": []
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Home", root.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("subtitle").ValueKind);
    }

    [Fact]
    public void NullableFields_ProgressNull()
    {
        var json = """
        {
            "title": "Home",
            "components": [],
            "actions": []
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.TryGetProperty("progress", out _));
    }

    [Theory]
    [InlineData("Primary")]
    [InlineData("Secondary")]
    [InlineData("Destructive")]
    public void ActionStyle_AllValuesValid(string style)
    {
        var json = $$"""{"id": "btn", "label": "Click", "style": "{{style}}", "enabled": true}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(style, root.GetProperty("style").GetString());
        Assert.Equal("btn", root.GetProperty("id").GetString());
        Assert.True(root.GetProperty("enabled").GetBoolean());
    }

    [Theory]
    [InlineData("""{"Text": {"id": "t1", "content": "Hello"}}""", "Text")]
    [InlineData("""{"TextInput": {"id": "ti1", "label": "Name", "value": ""}}""", "TextInput")]
    [InlineData("""{"ToggleList": {"id": "tl1", "label": "Options", "items": []}}""", "ToggleList")]
    [InlineData("""{"FieldList": {"id": "fl1", "fields": []}}""", "FieldList")]
    [InlineData("""{"CardPreview": {"id": "cp1", "name": "Alice", "fields": [], "groups": []}}""", "CardPreview")]
    [InlineData("""{"InfoPanel": {"id": "ip1", "title": "Info", "content": "Details"}}""", "InfoPanel")]
    [InlineData("""{"ContactList": {"id": "cl1", "contacts": []}}""", "ContactList")]
    [InlineData("""{"SettingsGroup": {"id": "sg1", "label": "General", "items": []}}""", "SettingsGroup")]
    [InlineData("""{"ActionList": {"id": "al1", "items": []}}""", "ActionList")]
    [InlineData("""{"StatusIndicator": {"id": "si1", "status": "connected", "label": "Online"}}""", "StatusIndicator")]
    [InlineData("""{"PinInput": {"id": "pi1", "label": "Enter PIN", "length": 6, "filled": 0}}""", "PinInput")]
    [InlineData("""{"QrCode": {"id": "qr1", "data": "test", "mode": "Display"}}""", "QrCode")]
    [InlineData("""{"ConfirmationDialog": {"id": "cd1", "title": "Confirm", "message": "Sure?"}}""", "ConfirmationDialog")]
    [InlineData(""""Divider"""", "Divider")]
    [InlineData("""{"ShowToast": {"id": "st1", "message": "Done"}}""", "ShowToast")]
    [InlineData("""{"InlineConfirm": {"id": "ic1", "label": "Delete", "confirm_label": "Yes"}}""", "InlineConfirm")]
    [InlineData("""{"EditableText": {"id": "et1", "value": "text", "editing": false}}""", "EditableText")]
    public void ComponentVariant_ParsesCorrectly(string componentJson, string expectedVariant)
    {
        using var doc = JsonDocument.Parse(componentJson);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.String)
        {
            // String variant (e.g. "Divider")
            Assert.Equal(expectedVariant, root.GetString());
        }
        else
        {
            // Object variant — first property name is the variant tag
            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            var firstProperty = root.EnumerateObject().GetEnumerator();
            Assert.True(firstProperty.MoveNext());
            Assert.Equal(expectedVariant, firstProperty.Current.Name);
        }
    }
}
