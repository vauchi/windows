// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Vauchi.CoreUI;

/// <summary>
/// Reads ScreenModel JSON and builds the full UI: title, subtitle, progress,
/// components, and screen-level action buttons.
/// </summary>
public sealed partial class ScreenRenderer : UserControl
{
    public ScreenRenderer()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when a user action should be sent back to the engine.
    /// </summary>
    public event EventHandler<string>? ActionRequested;

    /// <summary>
    /// Parse ScreenModel JSON and render the full screen.
    /// </summary>
    public void RenderFromJson(string screenJson)
    {
        ComponentContainer.Children.Clear();
        ActionBar.Children.Clear();

        using var doc = JsonDocument.Parse(screenJson);
        var root = doc.RootElement;

        // ── Title ────────────────────────────────────────────
        if (root.TryGetProperty("title", out var titleEl) &&
            titleEl.ValueKind == JsonValueKind.String)
        {
            TitleText.Text = titleEl.GetString() ?? "";
            TitleText.Visibility = Visibility.Visible;
        }
        else
        {
            TitleText.Text = "";
            TitleText.Visibility = Visibility.Collapsed;
        }

        // ── Subtitle ─────────────────────────────────────────
        if (root.TryGetProperty("subtitle", out var subtitleEl) &&
            subtitleEl.ValueKind == JsonValueKind.String)
        {
            SubtitleText.Text = subtitleEl.GetString() ?? "";
            SubtitleText.Visibility = Visibility.Visible;
        }
        else
        {
            SubtitleText.Visibility = Visibility.Collapsed;
        }

        // ── Progress ─────────────────────────────────────────
        if (root.TryGetProperty("progress", out var progressEl) &&
            progressEl.ValueKind == JsonValueKind.Object)
        {
            ProgressArea.Visibility = Visibility.Visible;

            if (progressEl.TryGetProperty("current", out var currentEl))
            {
                ProgressBar.Value = currentEl.GetDouble();
            }

            if (progressEl.TryGetProperty("total", out var totalEl))
            {
                ProgressBar.Maximum = totalEl.GetDouble();
            }

            if (progressEl.TryGetProperty("label", out var labelEl) &&
                labelEl.ValueKind == JsonValueKind.String)
            {
                ProgressLabel.Text = labelEl.GetString() ?? "";
                ProgressLabel.Visibility = Visibility.Visible;
            }
            else
            {
                ProgressLabel.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            ProgressArea.Visibility = Visibility.Collapsed;
        }

        // ── Components ───────────────────────────────────────
        if (root.TryGetProperty("components", out var components) &&
            components.ValueKind == JsonValueKind.Array)
        {
            foreach (var component in components.EnumerateArray())
            {
                var control = ComponentRenderer.CreateComponent(component);
                if (control != null)
                {
                    // Wire action bubble-up if the component supports it
                    if (control is IRenderable renderable)
                    {
                        renderable.ActionRequested += (_, actionJson) =>
                            ActionRequested?.Invoke(this, actionJson);
                    }

                    ComponentContainer.Children.Add(control);
                }
            }
        }

        // ── Screen-level actions ─────────────────────────────
        if (root.TryGetProperty("actions", out var actionsEl) &&
            actionsEl.ValueKind == JsonValueKind.Array)
        {
            bool hasActions = false;

            foreach (var actionEl in actionsEl.EnumerateArray())
            {
                var button = CreateActionButton(actionEl);
                if (button != null)
                {
                    ActionBar.Children.Add(button);
                    hasActions = true;
                }
            }

            ActionBar.Visibility = hasActions ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            ActionBar.Visibility = Visibility.Collapsed;
        }
    }

    private Button? CreateActionButton(JsonElement actionEl)
    {
        if (actionEl.ValueKind != JsonValueKind.Object)
            return null;

        string? actionId = null;
        string? label = null;
        string? style = null;
        bool enabled = true;

        if (actionEl.TryGetProperty("id", out var idEl))
            actionId = idEl.GetString();

        if (actionEl.TryGetProperty("label", out var labelEl))
            label = labelEl.GetString();

        if (actionEl.TryGetProperty("style", out var styleEl))
            style = styleEl.GetString();

        if (actionEl.TryGetProperty("enabled", out var enabledEl) &&
            enabledEl.ValueKind == JsonValueKind.False)
            enabled = false;

        if (string.IsNullOrEmpty(actionId) || string.IsNullOrEmpty(label))
            return null;

        var button = new Button
        {
            Content = label,
            IsEnabled = enabled,
            MinWidth = 120,
        };

        // Apply style based on action style
        switch (style)
        {
            case "Primary":
                button.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                break;
            case "Destructive":
                button.Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 53, 69));
                break;
        }

        string capturedId = actionId;
        button.Click += (_, _) =>
        {
            string json = JsonSerializer.Serialize(new { ActionPressed = new { action_id = capturedId } });
            ActionRequested?.Invoke(this, json);
        };

        return button;
    }

    internal void RaiseAction(string actionJson)
    {
        ActionRequested?.Invoke(this, actionJson);
    }
}
