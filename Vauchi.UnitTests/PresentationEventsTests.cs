// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Interop;
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
            {"surface_id":"editor","binding_id":"display_name","value":{"text":"A \"quoted\" name"}}
            """);
        AssertEvent(
            PresentationEvents.BooleanChanged("settings", "sync_enabled", true),
            "ValueChanged",
            """{"surface_id":"settings","binding_id":"sync_enabled","value":{"boolean":true}}""");
        AssertEvent(
            PresentationEvents.ChoiceChanged("editor", "kind", null),
            "ValueChanged",
            """{"surface_id":"editor","binding_id":"kind","value":{"choice":null}}""");
        AssertEvent(
            PresentationEvents.NumberChanged("settings", "volume", 0.75),
            "ValueChanged",
            """{"surface_id":"settings","binding_id":"volume","value":{"number":0.75}}""");
    }

    [Fact]
    public void EnvironmentAndOverlayEvents_ReportOnlyNativeFacts()
    {
        AssertEvent(
            PresentationEvents.EnvironmentChanged(840, 700, reducedMotion: true),
            "PresentationEnvironmentChanged",
            """
            {"available_width":840,"available_height":700,"input_modes":["pointer","keyboard"],"motion":"reduced","pinned_actions":true}
            """);
        AssertEvent(
            PresentationEvents.EnvironmentChanged(840, 700, reducedMotion: false, colorScheme: "light"),
            "PresentationEnvironmentChanged",
            """
            {"available_width":840,"available_height":700,"input_modes":["pointer","keyboard"],"motion":"full","pinned_actions":true,"color_scheme":"light"}
            """);
        AssertEvent(
            PresentationEvents.OverlayDismissed("contacts", "navigation"),
            "OverlayDismissed",
            """{"surface_id":"contacts","kind":"navigation"}""");
    }

    // The events Core's own contract fixture replays are the reference
    // for what a shell sends. A literal expectation in this file once
    // pinned {"Text":…} while the contract said {"text":…}, and Core
    // answered every value with an error (vauchi/private#1099).
    [Fact]
    public void ContractFixtureEvents_AreWhatThisShellBuilds()
    {
        string fixtureJson = Assert.IsType<string>(VauchiNative.PresentationContractFixture());
        using JsonDocument fixture = JsonDocument.Parse(fixtureJson);
        int compared = 0;
        foreach (JsonElement step in fixture.RootElement.GetProperty("steps").EnumerateArray())
        {
            JsonElement expected = step.GetProperty("event");
            JsonProperty variant = Assert.Single(expected.EnumerateObject());
            JsonElement payload = variant.Value;
            string surfaceId = payload.GetProperty("surface_id").GetString()!;
            string built = variant.Name switch
            {
                "ActionActivated" => PresentationEvents.ActionActivated(
                    surfaceId, payload.GetProperty("interaction_id").GetString()!),
                "ValueChanged" => BuildValueChanged(surfaceId, payload),
                _ => throw new Xunit.Sdk.XunitException(
                    $"the contract fixture replays {variant.Name}, which this test does not build yet"),
            };
            using JsonDocument actual = JsonDocument.Parse(built);
            Assert.Equal(
                JsonSerializer.Serialize(expected),
                JsonSerializer.Serialize(actual.RootElement));
            compared++;
        }
        Assert.True(compared > 0, "the contract fixture replays no events");
    }

    private static string BuildValueChanged(string surfaceId, JsonElement payload)
    {
        string bindingId = payload.GetProperty("binding_id").GetString()!;
        JsonProperty value = Assert.Single(payload.GetProperty("value").EnumerateObject());
        return value.Name switch
        {
            "text" => PresentationEvents.TextChanged(surfaceId, bindingId, value.Value.GetString()!),
            "boolean" => PresentationEvents.BooleanChanged(surfaceId, bindingId, value.Value.GetBoolean()),
            "choice" => PresentationEvents.ChoiceChanged(surfaceId, bindingId, value.Value.GetString()),
            "number" => PresentationEvents.NumberChanged(surfaceId, bindingId, value.Value.GetDouble()),
            _ => throw new Xunit.Sdk.XunitException(
                $"the contract fixture uses the value tag {value.Name}, which this shell cannot send"),
        };
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
