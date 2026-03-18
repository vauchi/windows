// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class ActionListComponent : UserControl, IRenderable
{
    public ActionListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        ActionContainer.Children.Clear();

        if (!data.TryGetProperty("actions", out var actions) ||
            actions.ValueKind != JsonValueKind.Array)
        {
            if (data.TryGetProperty("title", out var title))
            {
                ActionContainer.Children.Add(
                    new TextBlock { Text = title.GetString() ?? "[ActionList]" });
            }
            return;
        }

        foreach (var action in actions.EnumerateArray())
        {
            string actionId = action.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            string label = action.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? actionId : actionId;
            bool enabled = !action.TryGetProperty("enabled", out var en) || en.GetBoolean();
            string style = action.TryGetProperty("style", out var st) ? st.GetString() ?? "" : "";

            var btn = new Button { Content = label, IsEnabled = enabled };

            if (style == "Primary")
                btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];

            if (onAction != null)
            {
                string capturedId = actionId;
                btn.Click += (_, _) => onAction(ActionJson.ActionPressed(capturedId));
            }

            ActionContainer.Children.Add(btn);
        }
    }
}
