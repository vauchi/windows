// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI;

public sealed partial class PresentationSurface : UserControl
{
    private string _surfaceId = "";
    private double _minimumTargetSize = TargetSize.Default;

    public event Action<string, string>? EventReady;

    public PresentationSurface(JsonElement surface, JsonElement? contextBar)
    {
        Render(surface, contextBar);
    }

    public string SurfaceId => _surfaceId;

    /// <summary>
    /// The trailing ⋯ button, if this surface's bar has a Secondary slot —
    /// the anchor the action-menu overlay opens against (vauchi/private#534).
    /// </summary>
    public Button? SecondaryActionButton { get; private set; }

    /// <summary>
    /// Builds the surface the way the design canvas now draws every screen
    /// (vauchi/private#534): a title row carrying Back/Navigation leading
    /// and Info/Secondary trailing, the body below, and — only when Core
    /// sends one — a full-width Primary button under it. Absent slots take
    /// no space; the retired separate bottom row no longer exists.
    /// </summary>
    private void Render(JsonElement surface, JsonElement? contextBar)
    {
        _surfaceId = String(surface, "surface_id");
        _minimumTargetSize = TargetSize.From(surface);

        var content = new StackPanel
        {
            Spacing = Token(surface, "spacing_medium", 12),
            Padding = new Thickness(Token(surface, "spacing_large", 24)),
        };
        string subtitle = String(surface, "subtitle");
        if (subtitle.Length > 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = subtitle,
                Opacity = 0.72,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        if (surface.TryGetProperty("nodes", out JsonElement nodes)
            && nodes.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement node in nodes.EnumerateArray())
                content.Children.Add(RenderNode(node));
        }

        string layout = String(surface, "layout");
        FrameworkElement body = layout == "scroll"
            ? new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            }
            : content;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        root.Children.Add(RenderTitleRow(surface, contextBar));
        root.Children.Add(body);

        if (contextBar is { } bar
            && bar.TryGetProperty("primary", out JsonElement primary)
            && primary.ValueKind == JsonValueKind.Object)
        {
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Button primaryButton = ActionButton(primary);
            primaryButton.HorizontalContentAlignment = HorizontalAlignment.Center;
            AutomationProperties.SetAutomationId(primaryButton, "context-primary");
            Grid.SetRow(primaryButton, 2);
            root.Children.Add(primaryButton);
        }

        Content = root;
        AutomationProperties.SetAutomationId(this, _surfaceId);
        AutomationProperties.SetName(this, String(surface, "accessibility_label"));
    }

    /// <summary>
    /// `[‹] Title ……… [ⓘ] [⋯]` — Back/Navigation leading, Info/Secondary
    /// trailing, the title wrapping or shortening before either side moves
    /// (vauchi/private#534 design).
    /// </summary>
    private FrameworkElement RenderTitleRow(JsonElement surface, JsonElement? contextBar)
    {
        var row = new Grid { ColumnSpacing = 4 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        foreach (string role in ContextBarLayout.LeadingSlots(contextBar))
            leading.Children.Add(RoleButton(contextBar!.Value, role));
        Grid.SetColumn(leading, 0);

        var title = new TextBlock
        {
            Text = String(surface, "title"),
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0),
        };
        Grid.SetColumn(title, 1);

        var trailing = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        foreach (string role in ContextBarLayout.TrailingSlots(contextBar))
        {
            Button button = RoleButton(contextBar!.Value, role);
            if (role == "secondary")
                SecondaryActionButton = button;
            trailing.Children.Add(button);
        }
        Grid.SetColumn(trailing, 2);

        row.Children.Add(leading);
        row.Children.Add(title);
        row.Children.Add(trailing);
        return row;
    }

    private Button RoleButton(JsonElement contextBar, string role)
    {
        JsonElement action = contextBar.GetProperty(role);
        string interactionId = String(action, "interaction_id");
        var button = new Button
        {
            Content = String(action, "label"),
            IsEnabled = Boolean(action, "enabled", true),
            MinWidth = _minimumTargetSize,
            MinHeight = _minimumTargetSize,
        };
        FocusVisualStyle.Apply(button);
        AutomationProperties.SetAutomationId(button, $"context-{role}");
        AutomationProperties.SetName(
            button,
            String(action, "accessibility_label", String(action, "label")));
        button.Click += (_, _) => EmitAction(interactionId);
        return button;
    }

    private FrameworkElement RenderNode(JsonElement node)
    {
        var (variant, payload) = PresentationJson.Variant(node);
        if (payload is not { } value)
            return variant == "Divider" ? RenderDivider() : new Border();

        return variant switch
        {
            "Text" => RenderText(value),
            "Input" => RenderInput(value),
            "Toggle" => RenderToggle(value),
            "Choice" => RenderChoice(value),
            "Group" => RenderGroup(value),
            "List" => RenderList(value),
            "Image" => RenderImage(value),
            "Status" => RenderStatus(value),
            "Qr" => RenderQr(value),
            "Confirmation" => RenderConfirmation(value),
            "Slider" => RenderSlider(value),
            "Progress" => RenderProgress(value),
            _ => new Border(),
        };
    }

    private Button ActionButton(JsonElement action)
    {
        string interactionId = String(action, "interaction_id");
        var button = new Button
        {
            Content = String(action, "label"),
            IsEnabled = Boolean(action, "enabled", true),
            MinHeight = _minimumTargetSize,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        ApplyToneStyle(button, ActionToneStyle.From(String(action, "tone")));
        FocusVisualStyle.Apply(button);
        AutomationProperties.SetAutomationId(button, interactionId);
        AutomationProperties.SetName(
            button,
            String(action, "accessibility_label", String(action, "label")));
        button.Click += (_, _) => EmitAction(interactionId);
        return button;
    }

    private static void ApplyToneStyle(Button button, ActionTone tone)
    {
        switch (tone)
        {
            case ActionTone.Destructive:
                button.Foreground = new SolidColorBrush(ThemeColors.Destructive);
                break;
            case ActionTone.Serious:
                // Serious (e.g. Recovery) is consequential but not
                // destructive: an outline in the warning colour reads as
                // "pay attention" without borrowing destructive's alarm
                // red or a filled treatment neither tone earns.
                button.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                button.BorderBrush = new SolidColorBrush(ThemeColors.Warning);
                button.BorderThickness = new Thickness(2);
                button.Foreground = new SolidColorBrush(ThemeColors.Warning);
                break;
            case ActionTone.Standard:
                break;
        }
    }

    private void EmitAction(string interactionId)
    {
        if (_surfaceId.Length == 0 || interactionId.Length == 0)
            return;
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.ActionActivated(_surfaceId, interactionId));
    }

    private void EmitInputSubmitted(string bindingId) =>
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.InputSubmitted(_surfaceId, bindingId));

    private void EmitInputFocusEnded(string bindingId) =>
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.InputFocusEnded(_surfaceId, bindingId));

    private void EmitText(string bindingId, string value) =>
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.TextChanged(_surfaceId, bindingId, value));

    private void EmitBoolean(string bindingId, bool value) =>
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.BooleanChanged(_surfaceId, bindingId, value));

    private void EmitChoice(string bindingId, string? value) =>
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.ChoiceChanged(_surfaceId, bindingId, value));

    private void EmitNumber(string bindingId, double value) =>
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.NumberChanged(_surfaceId, bindingId, value));

    private static Border RenderDivider() => new()
    {
        Height = 1,
        Background = new SolidColorBrush(ThemeColors.Divider),
        Margin = new Thickness(0, 8, 0, 8),
    };

    private static void ApplyAccessibility(DependencyObject element, JsonElement payload)
    {
        if (!payload.TryGetProperty("accessibility", out JsonElement accessibility)
            || accessibility.ValueKind != JsonValueKind.Object)
            return;
        AutomationProperties.SetName(element, String(accessibility, "label"));
        string description = String(accessibility, "description");
        if (description.Length > 0)
            AutomationProperties.SetHelpText(element, description);
    }

    private static string String(
        JsonElement value,
        string property,
        string fallback = "") =>
        value.TryGetProperty(property, out JsonElement element)
        && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? fallback
            : fallback;

    private static bool Boolean(JsonElement value, string property, bool fallback = false) =>
        value.TryGetProperty(property, out JsonElement element)
        && element.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? element.GetBoolean()
            : fallback;

    /// <summary>
    /// Absent (not `null`) is how Core spells "the shell decides" for an
    /// optional integer such as `Image.size` — the decoder must keep
    /// accepting JSON without the property.
    /// </summary>
    private static int? Int32(JsonElement value, string property) =>
        value.TryGetProperty(property, out JsonElement element)
        && element.TryGetInt32(out int number)
            ? number
            : null;

    private static double Token(JsonElement surface, string property, double fallback) =>
        surface.TryGetProperty("tokens", out JsonElement tokens)
        && tokens.TryGetProperty(property, out JsonElement value)
        && value.TryGetDouble(out double number)
            ? number
            : fallback;
}
