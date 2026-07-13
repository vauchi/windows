// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Vauchi.Helpers;
using Vauchi.Services;
using Microsoft.UI.Xaml.Automation;
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
    /// Raised when the user activates the core-driven back chrome. The host
    /// forwards <c>UserAction::NavigateBack</c> to core and routes the result.
    /// Replaces the per-screen footer "Back" action.
    /// </summary>
    public event Action? BackRequested;

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

        // Layout: "Scroll" (default, often omitted) wraps the components in a
        // ScrollViewer; "Fixed" renders them directly so the content cannot
        // reflow or scroll (e.g. the QR exchange screen, where a moving QR
        // breaks the peer camera's lock). Mirrors android ScreenLayout.
        string layout = root.TryGetProperty("layout", out var lay) && lay.ValueKind == JsonValueKind.String
            ? lay.GetString() ?? "Scroll"
            : "Scroll";
        ApplyLayout(layout);

        // Core-driven nav chrome (ADR-044 Am2a). Core owns which chrome
        // affordances exist; the reserved `go_back` id is the visible back
        // button. All others (e.g. `open_settings`) are forwarded as
        // ActionPressed(action_id).
        RenderNavActions(root);

        ScreenTitle.Text = root.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";

        if (root.TryGetProperty("subtitle", out var subtitle) && subtitle.ValueKind == JsonValueKind.String)
        {
            ScreenSubtitle.Text = subtitle.GetString() ?? "";
            ScreenSubtitle.Visibility = Visibility.Visible;
        }
        else
        {
            ScreenSubtitle.Visibility = Visibility.Collapsed;
        }

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
                    btn.Foreground = new SolidColorBrush(ThemeColors.Destructive);

                string capturedId = actionId;
                btn.Click += (_, _) => RaiseAction(ActionJson.ActionPressed(capturedId));

                // Core-provided a11y override (plan Task 3.1 /
                // _private/docs/problems/2026-04-20-screen-action-a11y-identifier-gap).
                // When `a11y.label` is present it replaces the visible-text-
                // derived screen-reader announcement; `a11y.hint` maps to
                // UIA HelpText. Absent → fall back to `label`.
                string a11yLabel = label;
                string? a11yHint = null;
                if (action.TryGetProperty("a11y", out var a11y) && a11y.ValueKind == JsonValueKind.Object)
                {
                    if (a11y.TryGetProperty("label", out var al) && al.ValueKind == JsonValueKind.String)
                        a11yLabel = al.GetString() ?? label;
                    if (a11y.TryGetProperty("hint", out var ah) && ah.ValueKind == JsonValueKind.String)
                        a11yHint = ah.GetString();
                }

                // AutomationId = stable test-driver identifier (equivalent
                // to GTK's widget name / Qt's objectName / Compose testTag).
                AutomationProperties.SetAutomationId(btn, actionId);
                AutomationProperties.SetName(btn, a11yLabel);
                if (!string.IsNullOrEmpty(a11yHint))
                    AutomationProperties.SetHelpText(btn, a11yHint);

                ActionButtonPanel.Children.Add(btn);
            }
        }
    }

    /// <summary>
    /// Place <see cref="ComponentContainer"/> inside or outside the
    /// ScrollViewer based on the ScreenModel `layout` value. "Fixed" detaches
    /// the StackPanel from the ScrollViewer and parents it directly under
    /// ContentHost (no scroll, no reflow); anything else (incl. "Scroll" and
    /// unknown values, for forward compatibility) keeps the scroll wrapper.
    /// Idempotent: re-renders may toggle layout, so it always resets to the
    /// requested arrangement.
    /// </summary>
    private void ApplyLayout(string layout)
    {
        bool isFixed = layout == "Fixed";

        // Detach ComponentContainer from whatever currently parents it.
        ContentScroller.Content = null;
        ContentHost.Children.Remove(ComponentContainer);

        if (isFixed)
        {
            ContentScroller.Visibility = Visibility.Collapsed;
            if (!ContentHost.Children.Contains(ComponentContainer))
                ContentHost.Children.Add(ComponentContainer);
        }
        else
        {
            ContentScroller.Visibility = Visibility.Visible;
            ContentScroller.Content = ComponentContainer;
        }
    }

    /// <summary>
    /// Render the core-driven <c>nav_actions</c> chrome. The reserved
    /// <c>go_back</c> id maps to the visible back affordance and raises
    /// <see cref="BackRequested"/>; every other action is forwarded as
    /// <c>ActionPressed(action_id)</c>.
    /// </summary>
    private void RenderNavActions(JsonElement root)
    {
        NavActionsPanel.Children.Clear();

        if (!root.TryGetProperty("nav_actions", out var navActions)
            || navActions.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var action in navActions.EnumerateArray())
        {
            string actionId = action.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            string label = action.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? actionId : actionId;
            bool enabled = !action.TryGetProperty("enabled", out var en) || en.GetBoolean();
            string style = action.TryGetProperty("style", out var st) ? st.GetString() ?? "" : "";

            if (actionId == "go_back")
            {
                var backBtn = new Button
                {
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 4,
                        Children =
                        {
                            new FontIcon { Glyph = "\uE72B", FontSize = 14 },
                            new TextBlock { Text = label }
                        }
                    },
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    IsEnabled = enabled,
                };
                backBtn.SetValue(AutomationProperties.AutomationIdProperty, "nav_back");
                backBtn.SetValue(AutomationProperties.NameProperty, Localizer.T("action.back"));
                backBtn.Click += (_, _) => BackRequested?.Invoke();
                NavActionsPanel.Children.Add(backBtn);
            }
            else
            {
                var btn = new Button
                {
                    Content = label,
                    IsEnabled = enabled,
                    MinWidth = 80,
                };
                if (style == "Primary")
                    btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                else if (style == "Destructive")
                    btn.Foreground = new SolidColorBrush(ThemeColors.Destructive);

                string capturedId = actionId;
                btn.Click += (_, _) => RaiseAction(ActionJson.ActionPressed(capturedId));
                AutomationProperties.SetAutomationId(btn, actionId);
                NavActionsPanel.Children.Add(btn);
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
