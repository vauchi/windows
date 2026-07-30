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
    private double _minimumTargetSize = 40;

    public event Action<string, string>? EventReady;

    public PresentationSurface(JsonElement surface)
    {
        Render(surface);
    }

    public string SurfaceId => _surfaceId;

    private void Render(JsonElement surface)
    {
        _surfaceId = String(surface, "surface_id");
        if (surface.TryGetProperty("tokens", out JsonElement tokens)
            && tokens.TryGetProperty("minimum_target_size", out JsonElement minimum)
            && minimum.TryGetDouble(out double minimumSize))
        {
            _minimumTargetSize = Math.Max(24, minimumSize);
        }

        var content = new StackPanel
        {
            Spacing = Token(surface, "spacing_medium", 12),
            Padding = new Thickness(Token(surface, "spacing_large", 24)),
        };
        content.Children.Add(new TextBlock
        {
            Text = String(surface, "title"),
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
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
        Content = layout == "scroll"
            ? new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            }
            : content;
        AutomationProperties.SetAutomationId(this, _surfaceId);
        AutomationProperties.SetName(this, String(surface, "accessibility_label"));
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
        if (String(action, "tone") == "destructive")
            button.Foreground = new SolidColorBrush(ThemeColors.Destructive);
        AutomationProperties.SetAutomationId(button, interactionId);
        AutomationProperties.SetName(
            button,
            String(action, "accessibility_label", String(action, "label")));
        button.Click += (_, _) => EmitAction(interactionId);
        return button;
    }

    private void EmitAction(string interactionId)
    {
        if (_surfaceId.Length == 0 || interactionId.Length == 0)
            return;
        EventReady?.Invoke(
            _surfaceId,
            PresentationEvents.ActionActivated(_surfaceId, interactionId));
    }

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

    private static double Token(JsonElement surface, string property, double fallback) =>
        surface.TryGetProperty("tokens", out JsonElement tokens)
        && tokens.TryGetProperty(property, out JsonElement value)
        && value.TryGetDouble(out double number)
            ? number
            : fallback;
}
