// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

/// <summary>
/// Renders <c>Component::SectionedActionList</c> — multiple labeled groups
/// of tappable items, each rendered as a native section (header + item
/// buttons). Item taps emit <c>UserAction::ListItemSelected</c> keyed by
/// the component id + the item id. Mirrors <c>ActionListComponent</c>'s
/// per-item button shape, grouped under section headers.
/// </summary>
public sealed partial class SectionedActionListComponent : UserControl, IRenderable
{
    public SectionedActionListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        SectionsContainer.Children.Clear();

        string componentId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

        if (!data.TryGetProperty("sections", out var sections) ||
            sections.ValueKind != JsonValueKind.Array)
            return;

        foreach (var section in sections.EnumerateArray())
        {
            string sectionLabel = section.TryGetProperty("label", out var lblEl)
                ? lblEl.GetString() ?? ""
                : "";

            var sectionPanel = new StackPanel { Spacing = 4 };

            if (!string.IsNullOrEmpty(sectionLabel))
            {
                sectionPanel.Children.Add(new TextBlock
                {
                    Text = sectionLabel.ToUpperInvariant(),
                    Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                });
            }

            if (section.TryGetProperty("items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    sectionPanel.Children.Add(BuildItemButton(componentId, item, onAction));
                }
            }

            SectionsContainer.Children.Add(sectionPanel);
        }
    }

    private static UIElement BuildItemButton(string componentId, JsonElement item, Action<string>? onAction)
    {
        string itemId = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        string label = item.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : "";
        string? detail = item.TryGetProperty("detail", out var det) && det.ValueKind == JsonValueKind.String
            ? det.GetString()
            : null;

        var content = new StackPanel { Spacing = 2 };
        content.Children.Add(new TextBlock { Text = label });
        if (detail != null)
        {
            content.Children.Add(new TextBlock
            {
                Text = detail,
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }

        var btn = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };

        AutomationProperties.SetName(btn, label);

        if (onAction != null)
        {
            string capturedComponent = componentId;
            string capturedItem = itemId;
            btn.Click += (_, _) => onAction(ActionJson.ListItemSelected(capturedComponent, capturedItem));
        }

        return btn;
    }
}
