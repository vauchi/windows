// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Helpers;
using Vauchi.Services;
using Vauchi.UI;

namespace Vauchi.CoreUI.Components;

public sealed partial class ContactListComponent : UserControl, IRenderable
{
    public ContactListComponent()
    {
        InitializeComponent();
        SearchBox.PlaceholderText = Localizer.T("contacts.search_placeholder");
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
        bool searchable = data.TryGetProperty("searchable", out var searchEl) && searchEl.GetBoolean();

        SearchBox.Visibility = searchable ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(SearchBox, Localizer.T("a11y.search_contacts"));

        if (searchable && onAction != null)
        {
            SearchBox.TextChanged += (sender, _) =>
                onAction(ActionJson.SearchChanged(componentId, sender.Text));
        }

        ContactContainer.Children.Clear();

        if (!data.TryGetProperty("items", out var items))
            return;

        foreach (var item in items.EnumerateArray())
        {
            string itemId = item.TryGetProperty("id", out var itemIdEl) ? itemIdEl.GetString() ?? "" : "";
            string name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
            string initials = item.TryGetProperty("avatar_initials", out var initEl) ? initEl.GetString() ?? "" : "";
            string? subtitle = item.TryGetProperty("subtitle", out var subEl) ? subEl.GetString() : null;

            var row = BuildContactRow(initials, name, subtitle);
            AutomationProperties.SetName(row, name);

            if (onAction != null && itemId.Length > 0)
            {
                string capturedId = itemId;
                row.PointerPressed += (_, _) =>
                    onAction(ActionJson.ListItemSelected(componentId, capturedId));
            }

            ContactContainer.Children.Add(row);
        }
    }

    private static StackPanel BuildContactRow(string initials, string name, string? subtitle)
    {
        var avatar = new Grid
        {
            Width = 40,
            Height = 40,
        };

        var circle = new Ellipse
        {
            Width = 40,
            Height = 40,
            Fill = new SolidColorBrush(ThemeColors.AvatarFallback),
        };

        var initialsBlock = new TextBlock
        {
            Text = initials,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(ThemeColors.OnColored),
            FontSize = 14,
        };

        avatar.Children.Add(circle);
        avatar.Children.Add(initialsBlock);

        var nameBlock = new TextBlock
        {
            Text = name,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(nameBlock);

        if (!string.IsNullOrEmpty(subtitle))
        {
            var subtitleBlock = new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Opacity = 0.6,
            };
            textStack.Children.Add(subtitleBlock);
        }

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Tokens.SpacingDirection.ListItemInlineStart,
            Padding = new Thickness(Tokens.Spacing.Sm),
        };

        row.Children.Add(avatar);
        row.Children.Add(textStack);

        return row;
    }
}
