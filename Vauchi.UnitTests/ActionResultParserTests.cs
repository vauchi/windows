// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// Verify ActionResultParser correctly classifies vauchi-cabi ActionResult JSON.
/// </summary>
public class ActionResultParserTests
{
    [Fact]
    public void Classifies_UpdateScreen()
    {
        string json = """{"UpdateScreen":{"screen_id":"welcome","title":"Welcome","components":[],"actions":[],"progress":null}}""";
        Assert.Equal(ActionResultKind.UpdateScreen, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_NavigateTo()
    {
        string json = """{"NavigateTo":{"screen_id":"default_name","title":"Name","components":[],"actions":[],"progress":null}}""";
        Assert.Equal(ActionResultKind.NavigateTo, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_Complete_UnitVariant()
    {
        // Serde unit variants serialize as "Complete"
        string json = "\"Complete\"";
        Assert.Equal(ActionResultKind.Complete, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_WipeComplete_UnitVariant()
    {
        string json = "\"WipeComplete\"";
        Assert.Equal(ActionResultKind.WipeComplete, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_RequestCamera_UnitVariant()
    {
        string json = "\"RequestCamera\"";
        Assert.Equal(ActionResultKind.RequestCamera, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_ValidationError()
    {
        string json = """{"ValidationError":{"component_id":"name_input","message":"Name is required"}}""";
        Assert.Equal(ActionResultKind.ValidationError, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_ShowAlert()
    {
        string json = """{"ShowAlert":{"title":"Error","message":"Something went wrong"}}""";
        Assert.Equal(ActionResultKind.ShowAlert, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_OpenUrl()
    {
        string json = """{"OpenUrl":{"url":"https://vauchi.app"}}""";
        Assert.Equal(ActionResultKind.OpenUrl, ActionResultParser.Classify(json));
    }

    // OpenContact / EditContact / OpenEntryDetail were retired from the wire by
    // core!968 — route_result resolves every navigation ActionResult to
    // NavigateTo (routing.rs: "no path returns it raw anymore"). They are no
    // longer classified: a stale or adversarial core emitting one falls through
    // to Unknown (HandleActionResult → RefreshScreen), never a domain-nav
    // fallback (ADR-043/044 Pure Humble UI). Tier-1 of the opaque-nav-api plan.
    [Theory]
    [InlineData("""{"OpenContact":{"contact_id":"abc123"}}""")]
    [InlineData("""{"EditContact":{"contact_id":"abc123"}}""")]
    [InlineData("""{"OpenEntryDetail":{"field_id":"email_work"}}""")]
    public void Classifies_RetiredNavVariants_AsUnknown(string json)
    {
        Assert.Equal(ActionResultKind.Unknown, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_ShowToast()
    {
        string json = """{"ShowToast":{"message":"Contact deleted","undo_action_id":"undo_delete"}}""";
        Assert.Equal(ActionResultKind.ShowToast, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_StartDeviceLink()
    {
        string json = "\"StartDeviceLink\"";
        Assert.Equal(ActionResultKind.StartDeviceLink, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_Commands()
    {
        string json = """{"Commands":{"commands":["QrRequestScan"]}}""";
        Assert.Equal(ActionResultKind.Commands, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classify_BackupExportComplete_ReturnsCorrectKind()
    {
        string json = """{"BackupExportComplete": {"data": "deadbeef"}}""";
        Assert.Equal(ActionResultKind.BackupExportComplete, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_NativeError()
    {
        string json = """{"error":"null action JSON"}""";
        Assert.Equal(ActionResultKind.Error, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_UnknownVariant_AsUnknown()
    {
        string json = """{"FutureVariant":{"data":"something"}}""";
        Assert.Equal(ActionResultKind.Unknown, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_UnknownStringVariant_AsUnknown()
    {
        string json = "\"FutureUnitVariant\"";
        Assert.Equal(ActionResultKind.Unknown, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_Commands_WithQrDisplay()
    {
        string json = """{"Commands":{"commands":[{"QrDisplay":{"data":"vauchi://..."}},"QrRequestScan"]}}""";
        Assert.Equal(ActionResultKind.Commands, ActionResultParser.Classify(json));
    }

    [Fact]
    public void Classifies_Commands_EmptyCommands()
    {
        string json = """{"Commands":{"commands":[]}}""";
        Assert.Equal(ActionResultKind.Commands, ActionResultParser.Classify(json));
    }
}
