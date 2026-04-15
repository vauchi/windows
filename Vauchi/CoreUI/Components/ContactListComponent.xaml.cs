// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Text.Json;
using Vauchi.Helpers;
using Vauchi.UI;

namespace Vauchi.CoreUI.Components;

public sealed partial class ContactListComponent : UserControl, IRenderable
{
    public ContactListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
        bool searchable = data.TryGetProperty("searchable", out var searchEl) && searchEl.GetBoolean();

        SearchBox.Visibility = searchable ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(SearchBox, "Search contacts");

        if (searchable && onAction != null)
        {
            SearchBox.TextChanged += (sender, _) =>
                onAction(ActionJson.SearchChanged(componentId, sender.Text));
        }

        ContactContainer.Children.Clear();

        if (!data.TryGetProperty("contacts", out var contacts))
            return;

        foreach (var contact in contacts.EnumerateArray())
        {
            string contactId = contact.TryGetProperty("id", out var cIdEl) ? cIdEl.GetString() ?? "" : "";
            string name = contact.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
            string initials = contact.TryGetProperty("avatar_initials", out var initEl) ? initEl.GetString() ?? "" : "";
            string? subtitle = contact.TryGetProperty("subtitle", out var subEl) ? subEl.GetString() : null;

            var row = BuildContactRow(initials, name, subtitle);
            AutomationProperties.SetName(row, name);

            if (onAction != null && contactId.Length > 0)
            {
                string capturedId = contactId;
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
            Fill = new SolidColorBrush(Colors.SteelBlue),
        };

        var initialsBlock = new TextBlock
        {
            Text = initials,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Colors.White),
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
