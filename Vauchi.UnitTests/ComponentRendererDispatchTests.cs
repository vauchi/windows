// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;
using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class ComponentRendererDispatchTests
{
    [Theory]
    [InlineData("""{"Text": {"id": "t1", "content": "Hello", "style": "Title"}}""", "Text")]
    [InlineData("""{"TextInput": {"id": "ti1", "label": "Name", "value": "", "input_type": "Text"}}""", "TextInput")]
    [InlineData("""{"ActionList": {"id": "al1", "items": []}}""", "ActionList")]
    [InlineData("""{"ToggleList": {"id": "tl1", "label": "Options", "items": []}}""", "ToggleList")]
    [InlineData("""{"FieldList": {"id": "fl1", "fields": []}}""", "FieldList")]
    [InlineData("""{"List": {"id": "cl1", "items": [], "searchable": false}}""", "List")]
    [InlineData("""{"SettingsGroup": {"id": "sg1", "label": "General", "items": []}}""", "SettingsGroup")]
    [InlineData("""{"Preview": {"name": "Alice", "fields": [], "variants": []}}""", "Preview")]
    [InlineData("""{"InfoPanel": {"id": "ip1", "title": "Info", "items": []}}""", "InfoPanel")]
    [InlineData("""{"StatusIndicator": {"id": "si1", "title": "Online", "status": "Success"}}""", "StatusIndicator")]
    [InlineData("""{"PinInput": {"id": "pi1", "label": "PIN", "length": 6, "filled": 0, "masked": true}}""", "PinInput")]
    [InlineData("""{"QrCode": {"id": "qr1", "data": "test", "mode": "Display"}}""", "QrCode")]
    [InlineData("""{"ConfirmationDialog": {"id": "cd1", "title": "Confirm", "message": "Sure?", "confirm_text": "Yes", "destructive": false}}""", "ConfirmationDialog")]
    [InlineData("""{"ShowToast": {"id": "st1", "message": "Done", "duration_ms": 3000}}""", "ShowToast")]
    [InlineData("""{"InlineConfirm": {"id": "ic1", "warning": "Delete?", "confirm_text": "Yes", "cancel_text": "No", "destructive": true}}""", "InlineConfirm")]
    [InlineData("""{"EditableText": {"id": "et1", "label": "Name", "value": "Alice", "editing": false}}""", "EditableText")]
    [InlineData("""{"Dropdown": {"id": "dd1", "label": "Pick", "selected": null, "options": [{"id": "a", "label": "A"}]}}""", "Dropdown")]
    [InlineData("""{"Row": {"id": "exchange_preview_row", "items": []}}""", "Row")]
    [InlineData("""{"Indicator": {"id": "ind1", "label": "Offline", "kind": "Error", "action_id": null}}""", "Indicator")]
    [InlineData("""{"SectionedActionList": {"id": "sal1", "sections": []}}""", "SectionedActionList")]
    public void ExternallyTagged_Object_Dispatches(string json, string expectedVariant)
    {
        using var doc = JsonDocument.Parse(json);
        var (variantName, data) = ComponentRenderer.ExtractVariant(doc.RootElement);
        Assert.Equal(expectedVariant, variantName);
        Assert.NotNull(data);
    }

    [Fact]
    public void ExternallyTagged_StringVariant_Divider()
    {
        using var doc = JsonDocument.Parse("\"Divider\"");
        var (variantName, data) = ComponentRenderer.ExtractVariant(doc.RootElement);
        Assert.Equal("Divider", variantName);
        Assert.Null(data);
    }

    [Fact]
    public void Row_Variant_ExposesNestedItems()
    {
        // The Row variant carries a recursive `items` array of child
        // components, mirroring ActionList/List. ExtractVariant must surface
        // the inner object so the renderer can recurse into each child.
        var json = """
            {"Row": {"id": "exchange_preview_row", "items": [
                {"QrCode": {"id": "qr1", "data": "x", "mode": "Display"}},
                {"ActionList": {"id": "al1", "items": []}}
            ]}}
            """;
        using var doc = JsonDocument.Parse(json);
        var (variantName, data) = ComponentRenderer.ExtractVariant(doc.RootElement);

        Assert.Equal("Row", variantName);
        Assert.NotNull(data);

        Assert.True(data!.Value.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(2, items.GetArrayLength());

        // First child is the flexing QR preview, second the action list.
        var children = items.EnumerateArray().ToArray();
        var (firstVariant, _) = ComponentRenderer.ExtractVariant(children[0]);
        var (secondVariant, _) = ComponentRenderer.ExtractVariant(children[1]);
        Assert.Equal("QrCode", firstVariant);
        Assert.Equal("ActionList", secondVariant);
    }

    [Fact]
    public void Unknown_Variant_Returns_NullName()
    {
        using var doc = JsonDocument.Parse("""{"FutureComponent": {"id": "x"}}""");
        var (variantName, data) = ComponentRenderer.ExtractVariant(doc.RootElement);
        Assert.Equal("FutureComponent", variantName);
        Assert.NotNull(data);
    }

    [Fact]
    public void Empty_Object_Returns_NullName()
    {
        using var doc = JsonDocument.Parse("{}");
        var (variantName, _) = ComponentRenderer.ExtractVariant(doc.RootElement);
        Assert.Null(variantName);
    }

    [Fact]
    public void Null_Element_Returns_NullName()
    {
        using var doc = JsonDocument.Parse("null");
        var (variantName, _) = ComponentRenderer.ExtractVariant(doc.RootElement);
        Assert.Null(variantName);
    }

    [Fact]
    public void Array_Element_Returns_NullName()
    {
        using var doc = JsonDocument.Parse("[1,2,3]");
        var (variantName, _) = ComponentRenderer.ExtractVariant(doc.RootElement);
        Assert.Null(variantName);
    }
}
