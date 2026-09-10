// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class PresentationStateNavigationTests
{
    // @scenario: generic_presentation_protocol.feature :: Core publishes navigation persistently
    [Fact]
    public void SetNavigationWithFixtureShapedItem_ParsesEverySixFields()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":1,"title":"Contacts","nodes":[]}}},
              {"SetNavigation":{"surface_id":"contacts","revision":1,"navigation":{"items":[
                {
                  "interaction_id":"surface.1.context.presentation.navigation.contacts",
                  "label":"Contacts",
                  "accessibility_label":"Contacts",
                  "icon_token":"person.2",
                  "selected":true,
                  "badge_count":3
                }
              ]}}}
            ]}
            """, out _, out var error), error);

        NavigationSpec item = Assert.Single(state.Navigation("contacts")!);
        Assert.Equal("surface.1.context.presentation.navigation.contacts", item.InteractionId);
        Assert.Equal("Contacts", item.Label);
        Assert.Equal("Contacts", item.AccessibilityLabel);
        Assert.Equal("person.2", item.IconToken);
        Assert.True(item.Selected);
        Assert.Equal(3, item.BadgeCount);
    }

    /// <summary>Empty items means a locked app: no sidebar to show.</summary>
    [Fact]
    public void SetNavigationWithEmptyItems_ProducesAnEmptyList()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"locked","revision":1,"title":"Locked","nodes":[]}}},
              {"SetNavigation":{"surface_id":"locked","revision":1,"navigation":{"items":[]}}}
            ]}
            """, out _, out var error), error);

        Assert.Empty(state.Navigation("locked")!);
    }

    [Fact]
    public void SetNavigationWithUnknownFields_IgnoresThemAndParsesKnownOnes()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":1,"title":"Contacts","nodes":[]}}},
              {"SetNavigation":{"surface_id":"contacts","revision":1,"navigation":{"items":[
                {
                  "interaction_id":"nav.contacts",
                  "label":"Contacts",
                  "accessibility_label":"Contacts",
                  "icon_token":"person.2",
                  "selected":false,
                  "badge_count":0,
                  "section":"primary",
                  "future_field":"ignored"
                }
              ]}}}
            ]}
            """, out _, out var error), error);

        NavigationSpec item = Assert.Single(state.Navigation("contacts")!);
        Assert.Equal("nav.contacts", item.InteractionId);
        Assert.False(item.Selected);
        Assert.Equal(0, item.BadgeCount);
    }

    [Fact]
    public void AbsentIconToken_ParsesAsNull()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":1,"title":"Contacts","nodes":[]}}},
              {"SetNavigation":{"surface_id":"contacts","revision":1,"navigation":{"items":[
                {
                  "interaction_id":"nav.contacts",
                  "label":"Contacts",
                  "accessibility_label":"Contacts",
                  "icon_token":null,
                  "selected":false,
                  "badge_count":0
                }
              ]}}}
            ]}
            """, out _, out var error), error);

        Assert.Null(state.Navigation("contacts")![0].IconToken);
    }

    [Fact]
    public void ActiveNavigation_MirrorsActiveContextBar()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":1,"title":"Contacts","nodes":[]}}},
              {"SetNavigation":{"surface_id":"contacts","revision":1,"navigation":{"items":[
                {
                  "interaction_id":"nav.contacts",
                  "label":"Contacts",
                  "accessibility_label":"Contacts",
                  "icon_token":"person.2",
                  "selected":true,
                  "badge_count":0
                }
              ]}}},
              {"SetPresentationProfile":{"profile":{"window_class":"compact","pane_layout":"single","primary_surface":"contacts","detail_surface":null,"active_surface":"contacts"}}}
            ]}
            """, out _, out var error), error);

        Assert.Equal("contacts", state.ActiveSurfaceId);
        Assert.Single(state.ActiveNavigation!);
        Assert.Equal("nav.contacts", state.ActiveNavigation![0].InteractionId);
    }

    [Fact]
    public void SetNavigation_RejectsAStaleSurfaceRevision()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":2,"title":"Contacts","nodes":[]}}}
            ]}
            """, out _, out _));

        Assert.False(state.TryApplyEnvelope("""
            {"commands":[
              {"SetNavigation":{"surface_id":"contacts","revision":1,"navigation":{"items":[]}}}
            ]}
            """, out var effects, out var error));

        Assert.Empty(effects);
        Assert.Equal("SetNavigation does not match the current surface revision", error);
        Assert.Null(state.Navigation("contacts"));
    }

    [Fact]
    public void ReplacingASurface_ClearsItsPreviousNavigation()
    {
        var state = new PresentationState();
        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":1,"title":"First","nodes":[]}}},
              {"SetNavigation":{"surface_id":"contacts","revision":1,"navigation":{"items":[
                {
                  "interaction_id":"nav.contacts",
                  "label":"Contacts",
                  "accessibility_label":"Contacts",
                  "icon_token":"person.2",
                  "selected":true,
                  "badge_count":0
                }
              ]}}}
            ]}
            """, out _, out _));

        Assert.True(state.TryApplyEnvelope("""
            {"commands":[
              {"ReplaceSurface":{"surface":{"surface_id":"contacts","revision":2,"title":"Second","nodes":[]}}}
            ]}
            """, out _, out _));

        Assert.Null(state.Navigation("contacts"));
    }
}
