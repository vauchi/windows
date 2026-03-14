// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class ToggleListComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";

    public ToggleListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        _componentId = data.TryGetProperty("id", out var id)
            ? id.GetString() ?? ""
            : "";

        if (data.TryGetProperty("label", out var label))
        {
            ListLabel.Text = label.GetString() ?? "";
        }

        ItemsContainer.Children.Clear();

        if (!data.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in items.EnumerateArray())
        {
            var itemId = item.TryGetProperty("id", out var iid)
                ? iid.GetString() ?? ""
                : "";
            var itemLabel = item.TryGetProperty("label", out var lbl)
                ? lbl.GetString() ?? ""
                : "";
            var selected = item.TryGetProperty("selected", out var sel)
                           && sel.GetBoolean();
            var hasSubtitle = item.TryGetProperty("subtitle", out var subtitle)
                              && subtitle.ValueKind != JsonValueKind.Null;

            var header = hasSubtitle
                ? $"{itemLabel} - {subtitle.GetString()}"
                : itemLabel;

            var toggle = new ToggleSwitch
            {
                Header = header,
                IsOn = selected
            };

            var capturedId = itemId;
            toggle.Toggled += (_, _) =>
            {
                ActionRequested?.Invoke(this,
                    JsonSerializer.Serialize(new
                    {
                        ItemToggled = new
                        {
                            component_id = _componentId,
                            item_id = capturedId
                        }
                    }));
            };

            ItemsContainer.Children.Add(toggle);
        }
    }
}
