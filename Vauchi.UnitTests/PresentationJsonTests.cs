// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class PresentationJsonTests
{
    [Theory]
    [InlineData("""{"Text":{"content":"Hello"}}""", "Text")]
    [InlineData("""{"Input":{"binding_id":"name"}}""", "Input")]
    [InlineData("""{"Toggle":{"binding_id":"enabled"}}""", "Toggle")]
    [InlineData("""{"Choice":{"binding_id":"kind"}}""", "Choice")]
    [InlineData("""{"Group":{"children":[]}}""", "Group")]
    [InlineData("""{"List":{"id":"contacts"}}""", "List")]
    [InlineData("""{"Image":{"data":null}}""", "Image")]
    [InlineData("""{"Status":{"title":"Ready"}}""", "Status")]
    [InlineData("""{"Qr":{"id":"exchange"}}""", "Qr")]
    [InlineData("""{"Confirmation":{"id":"delete"}}""", "Confirmation")]
    [InlineData("""{"Slider":{"binding_id":"level"}}""", "Slider")]
    [InlineData("""{"Progress":{"value":null}}""", "Progress")]
    public void ObjectNode_ExposesItsExternalVariant(string json, string expectedVariant)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        var (variant, payload) = PresentationJson.Variant(document.RootElement);

        Assert.Equal(expectedVariant, variant);
        Assert.Equal(JsonValueKind.Object, payload?.ValueKind);
    }

    [Fact]
    public void DividerString_ExposesTheUnitVariant()
    {
        using JsonDocument document = JsonDocument.Parse("\"Divider\"");
        var (variant, payload) = PresentationJson.Variant(document.RootElement);

        Assert.Equal("Divider", variant);
        Assert.Null(payload);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("42")]
    public void InvalidNode_HasNoVariant(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        var (variant, payload) = PresentationJson.Variant(document.RootElement);

        Assert.Null(variant);
        Assert.Null(payload);
    }

    [Fact]
    public void ByteArray_RejectsValuesOutsideTheWireRange()
    {
        using JsonDocument valid = JsonDocument.Parse("[0,127,255]");
        using JsonDocument invalid = JsonDocument.Parse("[0,256]");

        Assert.Equal(new byte[] { 0, 127, 255 }, PresentationJson.Bytes(valid.RootElement));
        Assert.Null(PresentationJson.Bytes(invalid.RootElement));
    }
}
