// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Vauchi.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI;

/// <summary>
/// Reads ScreenModel JSON and builds the UI via ComponentRenderer.
/// Renders title, subtitle, progress, components, and screen-level action buttons.
/// </summary>
public sealed partial class ScreenRenderer : UserControl
{
    public ScreenRenderer()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Parse ScreenModel JSON and render all parts: header, components, action buttons.
    /// </summary>
    public void RenderFromJson(string screenJson)
    {
        ComponentContainer.Children.Clear();
        ActionButtonPanel.Children.Clear();

        using var doc = JsonDocument.Parse(screenJson);
        var root = doc.RootElement;

        string screenId = root.TryGetProperty("screen_id", out var sid) ? sid.GetString() ?? "" : "";
        System.Diagnostics.Debug.WriteLine($"[Vauchi] RenderScreen: {screenId}");

        // Title
        ScreenTitle.Text = root.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";

        // Subtitle (hide if null/absent)
        if (root.TryGetProperty("subtitle", out var subtitle) && subtitle.ValueKind == JsonValueKind.String)
        {
            ScreenSubtitle.Text = subtitle.GetString() ?? "";
            ScreenSubtitle.Visibility = Visibility.Visible;
        }
        else
        {
            ScreenSubtitle.Visibility = Visibility.Collapsed;
        }

        // Progress bar (hide if null/absent)
        if (root.TryGetProperty("progress", out var progress) && progress.ValueKind == JsonValueKind.Object)
        {
            int current = progress.TryGetProperty("current_step", out var cs) ? cs.GetInt32() : 0;
            int total = progress.TryGetProperty("total_steps", out var ts) ? ts.GetInt32() : 1;
            string? label = progress.TryGetProperty("label", out var pl) ? pl.GetString() : null;

            ProgressBar.Maximum = total;
            ProgressBar.Value = current;
            ProgressBar.Visibility = Visibility.Visible;

            if (label != null)
            {
                ProgressLabel.Text = label;
                ProgressLabel.Visibility = Visibility.Visible;
            }
            else
            {
                ProgressLabel.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressLabel.Visibility = Visibility.Collapsed;
        }

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

        // Render screen-level action buttons (pinned to bottom)
        if (root.TryGetProperty("actions", out var actions) &&
            actions.ValueKind == JsonValueKind.Array)
        {
            foreach (var action in actions.EnumerateArray())
            {
                string actionId = action.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                string label = action.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? actionId : actionId;
                bool enabled = !action.TryGetProperty("enabled", out var en) || en.GetBoolean();
                string style = action.TryGetProperty("style", out var st) ? st.GetString() ?? "" : "";

                var btn = new Button { Content = label, IsEnabled = enabled, MinWidth = 80 };

                if (style == "Primary")
                    btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                else if (style == "Destructive")
                    btn.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);

                string capturedId = actionId;
                btn.Click += (_, _) => RaiseAction(ActionJson.ActionPressed(capturedId));

                AutomationProperties.SetName(btn, label);

                ActionButtonPanel.Children.Add(btn);
            }
        }
    }

    /// <summary>
    /// Raised when a user action should be sent back to the core engine.
    /// </summary>
    public event EventHandler<string>? ActionRequested;

    internal void RaiseAction(string actionJson)
    {
        ActionRequested?.Invoke(this, actionJson);
    }
}
