// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class SidebarModelTests
{
    /// <summary>Nothing to show is not the same as nothing sent yet: both map to hidden.</summary>
    [Fact]
    public void EmptySpecs_ProducesAHiddenModelWithNoSelection()
    {
        SidebarModel model = SidebarModel.From(new List<NavigationSpec>());

        Assert.True(model.Hidden);
        Assert.Empty(model.Items);
        Assert.Equal(-1, model.SelectedIndex);
    }

    [Fact]
    public void NullSpecs_ProducesAHiddenModel()
    {
        SidebarModel model = SidebarModel.From(null);

        Assert.True(model.Hidden);
        Assert.Empty(model.Items);
    }

    [Fact]
    public void SpecsWithASelectedItem_ProducesAVisibleModelWithThatSelectedIndex()
    {
        var specs = new List<NavigationSpec>
        {
            new("nav.contacts", "Contacts", "Contacts", "person.2", false, 0),
            new("nav.settings", "Settings", "Settings", "gearshape", true, 0),
        };

        SidebarModel model = SidebarModel.From(specs);

        Assert.False(model.Hidden);
        Assert.Equal(2, model.Items.Count);
        Assert.Equal(1, model.SelectedIndex);
    }

    [Fact]
    public void NoItemSelected_SelectedIndexIsNegativeOne()
    {
        var specs = new List<NavigationSpec>
        {
            new("nav.contacts", "Contacts", "Contacts", "person.2", false, 0),
        };

        SidebarModel model = SidebarModel.From(specs);

        Assert.Equal(-1, model.SelectedIndex);
    }

    [Fact]
    public void KnownIconToken_ResolvesToItsNavigationIconsGlyph()
    {
        var specs = new List<NavigationSpec>
        {
            new("nav.settings", "Settings", "Settings", "gearshape", false, 0),
        };

        SidebarModel model = SidebarModel.From(specs);

        Assert.Equal(NavigationIcons.Glyph("gearshape"), model.Items[0].Glyph);
        Assert.NotEqual(NavigationIcons.FallbackGlyph, model.Items[0].Glyph);
    }

    [Fact]
    public void UnknownIconToken_ResolvesToTheFallbackGlyph()
    {
        var specs = new List<NavigationSpec>
        {
            new("nav.mystery", "Mystery", "Mystery", "not.a.real.token", false, 0),
        };

        SidebarModel model = SidebarModel.From(specs);

        Assert.Equal(NavigationIcons.FallbackGlyph, model.Items[0].Glyph);
    }

    [Fact]
    public void BadgeCountAndLabelsCarryThroughFromTheSpec()
    {
        var specs = new List<NavigationSpec>
        {
            new("nav.contacts", "Contacts", "Contacts, 3 unread", "person.2", true, 3),
        };

        SidebarModel model = SidebarModel.From(specs);

        SidebarItemModel item = model.Items[0];
        Assert.Equal("nav.contacts", item.InteractionId);
        Assert.Equal("Contacts", item.Label);
        Assert.Equal("Contacts, 3 unread", item.AccessibilityLabel);
        Assert.Equal(3, item.BadgeCount);
        Assert.True(item.Selected);
    }
}
