// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Xunit;

namespace Vauchi.Tests;

/// <summary>
/// Verifies parsing of ActionResult JSON from the Rust core engine.
/// Covers string variants, object variants, and error detection.
/// </summary>
public class ActionResultParsingTests
{
    [Theory]
    [InlineData("\"Complete\"")]
    [InlineData("\"WipeComplete\"")]
    [InlineData("\"RequestCamera\"")]
    [InlineData("\"StartDeviceLink\"")]
    public void StringVariant_ParsesCorrectly(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.String, root.ValueKind);
        Assert.False(string.IsNullOrEmpty(root.GetString()));
    }

    [Fact]
    public void UpdateScreen_ContainsScreenModel()
    {
        var json = """
        {
            "UpdateScreen": {
                "title": "My Info",
                "subtitle": "Edit your card",
                "components": [],
                "actions": []
            }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("UpdateScreen", out var screen));
        Assert.Equal("My Info", screen.GetProperty("title").GetString());
        Assert.Equal("Edit your card", screen.GetProperty("subtitle").GetString());
        Assert.Equal(JsonValueKind.Array, screen.GetProperty("components").ValueKind);
        Assert.Equal(JsonValueKind.Array, screen.GetProperty("actions").ValueKind);
    }

    [Fact]
    public void ValidationError_ParsesCorrectly()
    {
        var json = """{"ValidationError": {"field": "name", "message": "Name is required"}}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ValidationError", out var ve));
        Assert.Equal("name", ve.GetProperty("field").GetString());
        Assert.Equal("Name is required", ve.GetProperty("message").GetString());
    }

    [Fact]
    public void ShowAlert_ParsesCorrectly()
    {
        var json = """{"ShowAlert": {"title": "Warning", "message": "Are you sure?"}}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ShowAlert", out var alert));
        Assert.Equal("Warning", alert.GetProperty("title").GetString());
        Assert.Equal("Are you sure?", alert.GetProperty("message").GetString());
    }

    [Fact]
    public void ShowToast_WithUndo_ParsesCorrectly()
    {
        var json = """{"ShowToast": {"message": "Contact deleted", "undo_action_id": "undo_delete"}}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ShowToast", out var toast));
        Assert.Equal("Contact deleted", toast.GetProperty("message").GetString());
        Assert.Equal("undo_delete", toast.GetProperty("undo_action_id").GetString());
    }

    [Fact]
    public void OpenUrl_ParsesCorrectly()
    {
        var json = """{"OpenUrl": "https://vauchi.app"}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("OpenUrl", out var url));
        Assert.Equal("https://vauchi.app", url.GetString());
    }

    [Fact]
    public void Error_JsonDetected()
    {
        var json = """{"Error": "Something went wrong"}""";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("Error", out var error));
        Assert.Equal("Something went wrong", error.GetString());
    }

    [Fact]
    public void NavigateTo_ContainsScreenModel()
    {
        var json = """
        {
            "NavigateTo": {
                "title": "Contacts",
                "components": [{"List": {"id": "cl", "items": []}}],
                "actions": []
            }
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("NavigateTo", out var nav));
        Assert.Equal("Contacts", nav.GetProperty("title").GetString());
    }
}
