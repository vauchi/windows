// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Vauchi.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Vauchi.CoreUI;

/// <summary>
/// Reads ScreenModel JSON and builds the UI via ComponentRenderer.
/// </summary>
public sealed partial class ScreenRenderer : UserControl
{
    public ScreenRenderer()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Parse ScreenModel JSON and render all components + screen-level actions.
    /// </summary>
    public void RenderFromJson(string screenJson)
    {
        System.Diagnostics.Debug.WriteLine($"[Vauchi] RenderScreen: {screenJson[..Math.Min(screenJson.Length, 300)]}");
        ComponentContainer.Children.Clear();

        using var doc = JsonDocument.Parse(screenJson);
        var root = doc.RootElement;

        // Render components
        if (root.TryGetProperty("components", out var components) &&
            components.ValueKind == JsonValueKind.Array)
        {
            foreach (var component in components.EnumerateArray())
            {
                var control = ComponentRenderer.CreateComponent(component, RaiseAction);
                if (control != null)
                {
                    ComponentContainer.Children.Add(control);
                }
            }
        }

        // Render screen-level action buttons
        if (root.TryGetProperty("actions", out var actions) &&
            actions.ValueKind == JsonValueKind.Array)
        {
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0),
            };

            foreach (var action in actions.EnumerateArray())
            {
                string actionId = action.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                string label = action.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? actionId : actionId;
                bool enabled = !action.TryGetProperty("enabled", out var en) || en.GetBoolean();
                string style = action.TryGetProperty("style", out var st) ? st.GetString() ?? "" : "";

                var btn = new Button { Content = label, IsEnabled = enabled };

                if (style == "Primary")
                    btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];

                string capturedId = actionId;
                btn.Click += (_, _) => RaiseAction(ActionJson.ActionPressed(capturedId));

                buttonPanel.Children.Add(btn);
            }

            ComponentContainer.Children.Add(buttonPanel);
        }
    }

    /// <summary>
    /// Raised when a user action should be sent back to the workflow engine.
    /// </summary>
    public event EventHandler<string>? ActionRequested;

    internal void RaiseAction(string actionJson)
    {
        ActionRequested?.Invoke(this, actionJson);
    }
}
