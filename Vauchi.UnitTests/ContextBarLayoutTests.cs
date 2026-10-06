// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

// Which of Core's slots a bar draws, in Core's order (vauchi/private#479):
// the fifth explains the surface and comes last; a bar from an older Core
// has four, and an empty bar draws no strip at all.
public class ContextBarLayoutTests
{
    private static JsonElement Bar(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();

    private const string Action =
        """{"interaction_id":"x","label":"L","accessibility_label":"L","icon_token":null,"enabled":true,"shortcut":null}""";

    // @scenario: generic_presentation_protocol.feature :: Contextual controls expose four stable roles
    [Fact]
    public void Slots_KeepCoresOrder_WithInfoLast()
    {
        JsonElement bar = Bar(
            $$"""{"back":{{Action}},"navigation":null,"primary":{{Action}},"secondary":{{Action}},"info":{{Action}}}""");

        Assert.Equal(new[] { "back", "primary", "secondary", "info" }, ContextBarLayout.Slots(bar));
    }

    // @scenario: generic_presentation_protocol.feature :: Contextual controls expose four stable roles
    [Fact]
    public void Slots_OfAnOlderFourSlotBar_HaveNoInfo()
    {
        JsonElement bar = Bar(
            $$"""{"back":null,"navigation":{{Action}},"primary":{{Action}},"secondary":null}""");

        Assert.Equal(new[] { "navigation", "primary" }, ContextBarLayout.Slots(bar));
    }

    // @scenario: generic_presentation_protocol.feature :: Contextual controls expose four stable roles
    [Fact]
    public void Slots_OfNoBarOrAnEmptyBar_AreNone()
    {
        Assert.Empty(ContextBarLayout.Slots(null));
        Assert.Empty(ContextBarLayout.Slots(Bar(
            """{"back":null,"navigation":null,"primary":null,"secondary":null,"info":null}""")));
    }

    // The retired bottom row is gone (2026-10-06 design, vauchi/private#534):
    // Back and Navigation now draw at the leading end of the surface's own
    // title row, Back first since a navigation launcher only ever appears
    // where no persistent destinations already show it.
    // @scenario: generic_presentation_protocol.feature :: Contextual controls expose four stable roles
    [Fact]
    public void LeadingSlots_PutsBackBeforeNavigation()
    {
        JsonElement bar = Bar(
            $$"""{"back":{{Action}},"navigation":{{Action}},"primary":null,"secondary":null,"info":null}""");

        Assert.Equal(new[] { "back", "navigation" }, ContextBarLayout.LeadingSlots(bar));
    }

    // @scenario: generic_presentation_protocol.feature :: Contextual controls expose four stable roles
    [Fact]
    public void LeadingSlots_OmitsAbsentRolesAndTrailingRoles()
    {
        JsonElement bar = Bar(
            $$"""{"back":null,"navigation":null,"primary":{{Action}},"secondary":{{Action}},"info":{{Action}}}""");

        Assert.Empty(ContextBarLayout.LeadingSlots(bar));
    }

    // Info sits beside Actions, closer to the title; the menu button is the
    // very trailing end (diagram: `[‹] Title … [ⓘ] [⋯]`).
    // @scenario: generic_presentation_protocol.feature :: Contextual controls expose four stable roles
    [Fact]
    public void TrailingSlots_PutsInfoBeforeSecondary()
    {
        JsonElement bar = Bar(
            $$"""{"back":{{Action}},"navigation":null,"primary":null,"secondary":{{Action}},"info":{{Action}}}""");

        Assert.Equal(new[] { "info", "secondary" }, ContextBarLayout.TrailingSlots(bar));
    }

    // @scenario: generic_presentation_protocol.feature :: Contextual controls expose four stable roles
    [Fact]
    public void TrailingSlots_OmitsAbsentRolesAndLeadingRoles()
    {
        JsonElement bar = Bar(
            $$"""{"back":{{Action}},"navigation":{{Action}},"primary":{{Action}},"secondary":null,"info":null}""");

        Assert.Empty(ContextBarLayout.TrailingSlots(bar));
    }
}
