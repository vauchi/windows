// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class ContactListComponent : UserControl, IRenderable
{
    public ContactListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        ContactContainer.Children.Clear();

        string componentId = data.TryGetProperty("id", out var cid)
            ? cid.GetString() ?? "" : "";

        if (!data.TryGetProperty("contacts", out var contacts) ||
            contacts.ValueKind != JsonValueKind.Array)
        {
            if (data.TryGetProperty("title", out var title))
            {
                ContactContainer.Children.Add(
                    new TextBlock { Text = title.GetString() ?? "[ContactList]" });
            }
            return;
        }

        foreach (var contact in contacts.EnumerateArray())
        {
            string contactId = contact.TryGetProperty("id", out var id)
                ? id.GetString() ?? "" : "";
            string name = contact.TryGetProperty("name", out var n)
                ? n.GetString() ?? contactId : contactId;
            string? initials = contact.TryGetProperty("avatar_initials", out var av)
                ? av.GetString() : null;
            string? subtitle = contact.TryGetProperty("subtitle", out var sub)
                ? sub.GetString() : null;

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
            };

            if (initials != null)
            {
                row.Children.Add(new TextBlock
                {
                    Text = initials,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 32,
                    TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                });
            }

            var nameBlock = new TextBlock
            {
                Text = name,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var textStack = new StackPanel { Spacing = 2 };
            textStack.Children.Add(nameBlock);

            if (subtitle != null)
            {
                textStack.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    FontSize = 12,
                    Opacity = 0.6,
                });
            }

            row.Children.Add(textStack);

            var btn = new Button
            {
                Content = row,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };

            if (onAction != null)
            {
                string capturedId = contactId;
                string capturedComponentId = componentId;
                btn.Click += (_, _) =>
                    onAction(ActionJson.ListItemSelected(capturedComponentId, capturedId));
            }

            ContactContainer.Children.Add(btn);
        }
    }
}
