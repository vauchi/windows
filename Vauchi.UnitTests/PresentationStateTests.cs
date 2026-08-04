// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Interop;
using Xunit;

namespace Vauchi.UnitTests;

public class PresentationStateTests
{
    // @scenario: generic_presentation_protocol.feature :: Every shell renders the same prepared presentation
    [Fact]
    public void SharedPresentationContract_ReachesExpectedState()
    {
        string fixtureJson = Assert.IsType<string>(VauchiNative.PresentationContractFixture());
        using JsonDocument fixture = JsonDocument.Parse(fixtureJson);
        JsonElement root = fixture.RootElement;
        Assert.Equal(1, root.GetProperty("schema_version").GetInt32());

        var state = new PresentationState();
        Apply(state, root.GetProperty("initial_commands"));
        foreach (JsonElement step in root.GetProperty("steps").EnumerateArray())
            Apply(state, step.GetProperty("commands"));

        JsonElement expected = root.GetProperty("expected_state");
        string surfaceId = expected.GetProperty("active_surface_id").GetString()!;
        Assert.Equal(surfaceId, state.ActiveSurfaceId);
        AssertJsonEqual(expected.GetProperty("surface"), state.Surface(surfaceId));
        AssertJsonEqual(expected.GetProperty("context_bar"), state.ContextBar(surfaceId));
    }

    private static void AssertJsonEqual(JsonElement expected, JsonElement? actual)
    {
        Assert.True(actual.HasValue, "expected presentation state to contain JSON");
        Assert.True(JsonEquals(expected, actual.Value), "JSON values differ");
    }

    private static bool JsonEquals(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind != actual.ValueKind)
            return false;

        return expected.ValueKind switch
        {
            JsonValueKind.Object => expected.EnumerateObject().All(property =>
                actual.TryGetProperty(property.Name, out JsonElement value)
                && JsonEquals(property.Value, value))
                && expected.EnumerateObject().Count() == actual.EnumerateObject().Count(),
            JsonValueKind.Array => expected.GetArrayLength() == actual.GetArrayLength()
                && expected.EnumerateArray()
                    .Select((value, index) => JsonEquals(value, actual[index]))
                    .All(equal => equal),
            JsonValueKind.String => expected.GetString() == actual.GetString(),
            JsonValueKind.Number => expected.GetRawText() == actual.GetRawText(),
            JsonValueKind.True or JsonValueKind.False =>
                expected.GetBoolean() == actual.GetBoolean(),
            JsonValueKind.Null or JsonValueKind.Undefined => true,
            _ => false,
        };
    }

    private static void Apply(PresentationState state, JsonElement commands)
    {
        string envelope = $"{{\"commands\":{commands.GetRawText()}}}";
        Assert.True(state.TryApplyEnvelope(envelope, out var effects, out var error), error);
        Assert.Empty(effects);
    }

    [Fact]
    public void OrderedBatch_DerivesVisibleSurfacesAndActiveContext()
    {
        var state = new PresentationState();
        const string batch = """
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":7,"title":"Contacts","nodes":[]}}},
              {"ReplaceSurface":{"surface":{"surface_id":"detail","revision":7,"title":"Bob","nodes":[]}}},
              {"SetContextBar":{"surface_id":"contacts","revision":7,"bar":{"primary":{"interaction_id":"add","label":"Add"}}}},
              {"SetContextBar":{"surface_id":"detail","revision":7,"bar":{"primary":{"interaction_id":"edit","label":"Edit"}}}},
              {"SetPresentationProfile":{"profile":{"window_class":"expanded","pane_layout":"split","primary_surface":"contacts","detail_surface":"detail","active_surface":"detail"}}}
            ]}
            """;

        Assert.True(state.TryApplyEnvelope(batch, out var effects, out var error), error);
        Assert.Empty(effects);
        Assert.Equal(new[] { "contacts", "detail" }, state.VisibleSurfaceIds);
        Assert.Equal("detail", state.ActiveSurfaceId);
        Assert.Equal("Bob", state.Surface("detail")?.GetProperty("title").GetString());
        Assert.Equal(
            "edit",
            state.ActiveContextBar?.GetProperty("primary").GetProperty("interaction_id").GetString());
    }

    /// <summary>
    /// Core's revision advances only on user actions, so racing full rebuilds
    /// (wakeup re-load, invalidation dispatch) legitimately re-emit the same
    /// surface at the same revision. Only a strictly older revision is stale.
    ///
    /// This shell is already correct, accepting <c>revision &gt;= current</c>.
    /// Nothing pinned that, so tightening to <c>&gt;</c> would reintroduce the
    /// bug silently. Android and macOS both had it wrong
    /// (vauchi/android!610, vauchi/macos!346); on Android it failed every cold
    /// launch, and because envelopes apply atomically each rejection discarded
    /// every command batched with it.
    /// </summary>
    [Fact]
    public void ReEmittedSameRevision_ReAppliesInsteadOfFailing()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":2,"title":"First","nodes":[]}}}
            ]}
            """, out _, out var firstError), firstError);

        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":2,"title":"Rebuilt","nodes":[]}}}
            ]}
            """, out _, out var rebuildError), rebuildError);

        Assert.Equal("Rebuilt", state.Surface("main")?.GetProperty("title").GetString());
    }

    [Fact]
    public void StaleSurfaceAndContextRevisions_DoNotReplaceCurrentState()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":8,"title":"Current","nodes":[]}}},
              {"SetContextBar":{"surface_id":"main","revision":8,"bar":{"primary":{"interaction_id":"save","label":"Save"}}}},
              {"SetPresentationProfile":{"profile":{"window_class":"compact","pane_layout":"single","primary_surface":"main","detail_surface":null,"active_surface":"main"}}}
            ]}
            """, out _, out _));

        Assert.False(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":7,"title":"Stale","nodes":[]}}},
              {"SetContextBar":{"surface_id":"main","revision":7,"bar":{"primary":{"interaction_id":"old","label":"Old"}}}}
            ]}
            """, out var effects, out var error));

        Assert.Empty(effects);
        Assert.Equal("SetContextBar does not match the current surface revision", error);
        Assert.Equal("Current", state.Surface("main")?.GetProperty("title").GetString());
        Assert.Equal(
            "save",
            state.ActiveContextBar?.GetProperty("primary").GetProperty("interaction_id").GetString());
    }

    [Fact]
    public void InvalidPresentationCommand_RejectsTheWholeBatchAtomically()
    {
        var state = new PresentationState();
        Assert.False(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":1,"title":"Partial","nodes":[]}}},
              {"SetPresentationProfile":{"profile":{"window_class":"expanded","pane_layout":"split","active_surface":"main"}}}
            ]}
            """, out var effects, out var error));

        Assert.Empty(effects);
        Assert.Equal("SetPresentationProfile is missing primary_surface", error);
        Assert.Null(state.Surface("main"));
        Assert.Empty(state.VisibleSurfaceIds);
    }

    [Fact]
    public void NativeEffectCommands_AreReturnedWithoutFrontendDomainParsing()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":3,"title":"Current","nodes":[]}}},
              {"SetPresentationProfile":{"profile":{"window_class":"compact","pane_layout":"single","primary_surface":"main","detail_surface":null,"active_surface":"main"}}},
              {"PresentOverlay":{"surface_id":"main","revision":3,"overlay":{"kind":"action_menu","title":"More","items":[]}}},
              {"ShowToast":{"toast":{"message":"Saved"}}},
              "PerformNativeBack"
            ]}
            """, out var effects, out var error), error);

        Assert.Equal(
            new[] { "PresentOverlay", "ShowToast", "PerformNativeBack" },
            effects.Select(PresentationState.CommandName));
        Assert.Equal(
            "action_menu",
            state.PresentedOverlay?.GetProperty("overlay").GetProperty("kind").GetString());
    }

    [Fact]
    public void ContextAndOverlayCommands_RejectAStaleSurfaceRevision()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":8,"title":"Current","nodes":[]}}},
              {"SetPresentationProfile":{"profile":{"window_class":"compact","pane_layout":"single","primary_surface":"main","detail_surface":null,"active_surface":"main"}}}
            ]}
            """, out _, out _));

        Assert.False(state.TryApplyEnvelope("""
            {"commands":[
              {"SetContextBar":{"surface_id":"main","revision":7,"bar":{"primary":{"interaction_id":"old","label":"Old"}}}}
            ]}
            """, out var contextEffects, out var contextError));
        Assert.Empty(contextEffects);
        Assert.Equal("SetContextBar does not match the current surface revision", contextError);

        Assert.False(state.TryApplyEnvelope("""
            {"commands":[
              {"PresentOverlay":{"surface_id":"main","revision":7,"overlay":{"kind":"navigation","items":[]}}}
            ]}
            """, out var overlayEffects, out var overlayError));
        Assert.Empty(overlayEffects);
        Assert.Equal("PresentOverlay does not match the current surface revision", overlayError);
        Assert.Null(state.ActiveContextBar);
        Assert.Null(state.PresentedOverlay);
    }

    [Fact]
    public void ReplacingASurface_ClearsItsPreviousContextAndOverlay()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":1,"title":"First","nodes":[]}}},
              {"SetContextBar":{"surface_id":"main","revision":1,"bar":{"primary":{"interaction_id":"save","label":"Save"}}}},
              {"SetPresentationProfile":{"profile":{"window_class":"compact","pane_layout":"single","primary_surface":"main","detail_surface":null,"active_surface":"main"}}},
              {"PresentOverlay":{"surface_id":"main","revision":1,"overlay":{"kind":"navigation","items":[]}}}
            ]}
            """, out _, out _));

        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"main","revision":2,"title":"Second","nodes":[]}}}
            ]}
            """, out _, out _));

        Assert.Equal("Second", state.Surface("main")?.GetProperty("title").GetString());
        Assert.Null(state.ContextBar("main"));
        Assert.Null(state.PresentedOverlay);
    }

    [Fact]
    public void ResponsiveProfile_PreservesSelectionActiveSurfaceAndCausalUndo()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{
                "surface_id":"detail",
                "revision":4,
                "title":"Detail",
                "nodes":[{"Choice":{
                  "binding_id":"trust",
                  "selected":"verified",
                  "options":[{"id":"verified","label":"Verified"}]
                }}]
              }}},
              {"SetContextBar":{
                "surface_id":"detail",
                "revision":4,
                "bar":{"primary":{
                  "interaction_id":"undo.archive",
                  "label":"Undo archive",
                  "shortcut":"undo"
                }}
              }},
              {"SetPresentationProfile":{"profile":{
                "window_class":"compact",
                "pane_layout":"single",
                "primary_surface":"detail",
                "detail_surface":null,
                "active_surface":"detail"
              }}}
            ]}
            """, out _, out var initialError), initialError);

        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"SetPresentationProfile":{"profile":{
                "window_class":"expanded",
                "pane_layout":"single",
                "primary_surface":"detail",
                "detail_surface":null,
                "active_surface":"detail"
              }}}
            ]}
            """, out _, out var expandedError), expandedError);

        Assert.Equal("detail", state.ActiveSurfaceId);
        JsonElement? detail = state.Surface("detail");
        JsonElement? context = state.ActiveContextBar;
        Assert.True(detail.HasValue);
        Assert.True(context.HasValue);
        Assert.Equal(
            "verified",
            detail.Value
                .GetProperty("nodes")[0]
                .GetProperty("Choice")
                .GetProperty("selected")
                .GetString());
        Assert.Equal(
            "undo",
            context.Value
                .GetProperty("primary")
                .GetProperty("shortcut")
                .GetString());
    }
}
