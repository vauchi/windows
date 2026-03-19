// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class ToggleListComponent : UserControl, IRenderable
{
    public ToggleListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        ToggleContainer.Children.Clear();

        string componentId = data.TryGetProperty("id", out var cid)
            ? cid.GetString() ?? "" : "";

        // Group header (key is "label", not "title")
        if (data.TryGetProperty("label", out var label))
        {
            ToggleContainer.Children.Add(new TextBlock
            {
                Text = label.GetString() ?? "[ToggleList]",
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
            bool selected = item.TryGetProperty("selected", out var sel)
                && sel.GetBoolean();

            var checkBox = new CheckBox
            {
                Content = itemLabel,
                IsChecked = selected,
            };

            if (onAction != null)
            {
                string capturedId = itemId;
                string capturedComponentId = componentId;
                checkBox.Checked += (_, _) =>
                    onAction(ActionJson.ItemToggled(capturedComponentId, capturedId));
                checkBox.Unchecked += (_, _) =>
                    onAction(ActionJson.ItemToggled(capturedComponentId, capturedId));
            }

            ToggleContainer.Children.Add(checkBox);
        }
    }
}
