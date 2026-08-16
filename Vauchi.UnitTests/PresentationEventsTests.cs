// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class PresentationEventsTests
{
    [Fact]
    public void InteractionEvents_UseOpaqueSurfaceAndInteractionIdentifiers()
    {
        AssertEvent(
            PresentationEvents.SurfaceActivated("contacts"),
            "SurfaceActivated",
            """{"surface_id":"contacts"}""");
        AssertEvent(
            PresentationEvents.ActionActivated("contacts", "contact:42:open"),
            "ActionActivated",
            """{"surface_id":"contacts","interaction_id":"contact:42:open"}""");
        AssertEvent(
            PresentationEvents.BackRequested("detail"),
            "BackRequested",
            """{"surface_id":"detail"}""");
    }

    [Fact]
    public void BindingGestureEvents_NameTheBindingWithoutCarryingItsValue()
    {
        // Both report *that* something happened, not what the field now
        // holds — the value already reached Core through TextChanged.
        AssertEvent(
            PresentationEvents.InputSubmitted("groups", "custom_group"),
            "InputSubmitted",
            """{"surface_id":"groups","binding_id":"custom_group"}""");
        AssertEvent(
            PresentationEvents.InputFocusEnded("groups", "custom_group"),
            "InputFocusEnded",
            """{"surface_id":"groups","binding_id":"custom_group"}""");
    }

    [Fact]
    public void ValueEvents_PreserveTheCanonicalTaggedInputValue()
    {
        AssertEvent(
            PresentationEvents.TextChanged("editor", "display_name", "A \"quoted\" name"),
            "ValueChanged",
            """
            {"surface_id":"editor","binding_id":"display_name","value":{"Text":"A \"quoted\" name"}}
            """);
        AssertEvent(
            PresentationEvents.BooleanChanged("settings", "sync_enabled", true),
            "ValueChanged",
            """{"surface_id":"settings","binding_id":"sync_enabled","value":{"Boolean":true}}""");
        AssertEvent(
            PresentationEvents.ChoiceChanged("editor", "kind", null),
            "ValueChanged",
            """{"surface_id":"editor","binding_id":"kind","value":{"Choice":null}}""");
        AssertEvent(
            PresentationEvents.NumberChanged("settings", "volume", 0.75),
            "ValueChanged",
            """{"surface_id":"settings","binding_id":"volume","value":{"Number":0.75}}""");
    }

    [Fact]
    public void EnvironmentAndOverlayEvents_ReportOnlyNativeFacts()
    {
        AssertEvent(
            PresentationEvents.EnvironmentChanged(840, 700, reducedMotion: true),
            "PresentationEnvironmentChanged",
            """
            {"available_width":840,"available_height":700,"input_modes":["pointer","keyboard"],"motion":"reduced"}
            """);
        AssertEvent(
            PresentationEvents.OverlayDismissed("contacts", "navigation"),
            "OverlayDismissed",
            """{"surface_id":"contacts","kind":"navigation"}""");
    }

    private static void AssertEvent(
        string actualJson,
        string expectedVariant,
        string expectedPayloadJson)
    {
        using JsonDocument actual = JsonDocument.Parse(actualJson);
        using JsonDocument expectedPayload = JsonDocument.Parse(expectedPayloadJson);
        JsonElement root = actual.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Single(root.EnumerateObject());
        Assert.True(root.TryGetProperty(expectedVariant, out JsonElement payload));
        Assert.Equal(
            JsonSerializer.Serialize(expectedPayload.RootElement),
            JsonSerializer.Serialize(payload));
    }
}
