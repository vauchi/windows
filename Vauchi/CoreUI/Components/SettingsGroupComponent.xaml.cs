// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class SettingsGroupComponent : UserControl, IRenderable
{
    public SettingsGroupComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        SettingsContainer.Children.Clear();

        string componentId = data.TryGetProperty("id", out var cid)
            ? cid.GetString() ?? "" : "";

        // Label is the group header (key is "label", not "title")
        if (data.TryGetProperty("label", out var label))
        {
            SettingsContainer.Children.Add(new TextBlock
            {
                Text = label.GetString() ?? "[SettingsGroup]",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
        }

        if (!data.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in items.EnumerateArray())
        {
            string itemId = item.TryGetProperty("id", out var id)
                ? id.GetString() ?? "" : "";
            string itemLabel = item.TryGetProperty("label", out var lbl)
                ? lbl.GetString() ?? itemId : itemId;

            string kindName = DetectKind(item);

            if (kindName == "Toggle")
            {
                var toggle = new ToggleSwitch
                {
                    Header = itemLabel,
                    IsOn = GetToggleValue(item),
                };

                if (onAction != null)
                {
                    string capturedId = itemId;
                    string capturedComponentId = componentId;
                    toggle.Toggled += (_, _) =>
                        onAction(ActionJson.SettingsToggled(capturedComponentId, capturedId));
                }

                SettingsContainer.Children.Add(toggle);
            }
            else
            {
                // Link, Value, Destructive — render as clickable row
                var btn = new Button
                {
                    Content = itemLabel,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                };

                if (kindName == "Destructive")
                {
                    btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                }

                if (onAction != null)
                {
                    string capturedId = itemId;
                    string capturedComponentId = componentId;
                    btn.Click += (_, _) =>
                        onAction(ActionJson.ListItemSelected(capturedComponentId, capturedId));
                }

                SettingsContainer.Children.Add(btn);
            }
        }
    }

    private static string DetectKind(JsonElement item)
    {
        if (!item.TryGetProperty("kind", out var kind))
            return "Link";

        if (kind.ValueKind == JsonValueKind.String)
            return kind.GetString() ?? "Link";

        // Externally-tagged enum: {"Toggle": {...}}, "Link", {"Value": {...}}, etc.
        if (kind.ValueKind == JsonValueKind.Object)
        {
            if (kind.TryGetProperty("Toggle", out _)) return "Toggle";
            if (kind.TryGetProperty("Value", out _)) return "Value";
            if (kind.TryGetProperty("Link", out _)) return "Link";
            if (kind.TryGetProperty("Destructive", out _)) return "Destructive";
        }

        return "Link";
    }

    private static bool GetToggleValue(JsonElement item)
    {
        if (item.TryGetProperty("kind", out var kind) &&
            kind.ValueKind == JsonValueKind.Object &&
            kind.TryGetProperty("Toggle", out var toggle))
        {
            if (toggle.TryGetProperty("value", out var v))
                return v.GetBoolean();
            if (toggle.TryGetProperty("enabled", out var e))
                return e.GetBoolean();
        }
        return false;
    }
}
