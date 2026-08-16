// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Windows.System;

namespace Vauchi.CoreUI;

public sealed partial class PresentationSurface
{
    private FrameworkElement RenderText(JsonElement payload)
    {
        string style = String(payload, "style");
        var text = new TextBlock
        {
            Text = String(payload, "content"),
            TextWrapping = TextWrapping.Wrap,
            Opacity = style == "muted" ? 0.65 : 1,
            FontSize = style switch
            {
                "heading" => 22,
                "caption" => 12,
                _ => 14,
            },
            FontFamily = style == "monospace"
                ? new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                : null,
        };
        ApplyAccessibility(text, payload);
        return text;
    }

    private FrameworkElement RenderInput(JsonElement payload)
    {
        string bindingId = String(payload, "binding_id");
        string kind = String(payload, "input_kind");
        var container = FieldContainer(String(payload, "label"));
        Control input;
        if (kind is "password" or "pin")
        {
            var password = new PasswordBox
            {
                Password = String(payload, "value"),
                PlaceholderText = String(payload, "placeholder"),
                IsEnabled = Boolean(payload, "enabled", true),
                MinHeight = _minimumTargetSize,
            };
            password.PasswordChanged += (_, _) => EmitText(bindingId, password.Password);
            input = password;
        }
        else
        {
            var text = new TextBox
            {
                Text = String(payload, "value"),
                PlaceholderText = String(payload, "placeholder"),
                IsEnabled = Boolean(payload, "enabled", true),
                MinHeight = _minimumTargetSize,
            };
            if (payload.TryGetProperty("max_length", out JsonElement maximum)
                && maximum.TryGetInt32(out int maxLength)
                && maxLength > 0)
            {
                text.MaxLength = maxLength;
            }
            text.TextChanged += (_, _) => EmitText(bindingId, text.Text);
            // Enter is the submit gesture; LostFocus fires whatever took the
            // focus, so no click-outside handling is needed here.
            text.KeyDown += (_, args) =>
            {
                if (args.Key == VirtualKey.Enter)
                {
                    EmitInputSubmitted(bindingId);
                }
            };
            text.LostFocus += (_, _) => EmitInputFocusEnded(bindingId);
            input = text;
        }
        AutomationProperties.SetAutomationId(input, bindingId);
        ApplyAccessibility(input, payload);
        container.Children.Add(input);

        string error = String(payload, "validation_error");
        if (error.Length > 0)
        {
            container.Children.Add(new TextBlock
            {
                Text = error,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    ThemeColors.Destructive),
                TextWrapping = TextWrapping.Wrap,
            });
        }
        return container;
    }

    private FrameworkElement RenderToggle(JsonElement payload)
    {
        string bindingId = String(payload, "binding_id");
        var toggle = new ToggleSwitch
        {
            Header = String(payload, "label"),
            IsOn = Boolean(payload, "value"),
            IsEnabled = Boolean(payload, "enabled", true),
            MinHeight = _minimumTargetSize,
        };
        AutomationProperties.SetAutomationId(toggle, bindingId);
        ApplyAccessibility(toggle, payload);
        toggle.Toggled += (_, _) => EmitBoolean(bindingId, toggle.IsOn);
        return toggle;
    }

    private FrameworkElement RenderChoice(JsonElement payload)
    {
        string bindingId = String(payload, "binding_id");
        string selected = String(payload, "selected");
        var container = FieldContainer(String(payload, "label"));
        var choice = new ComboBox
        {
            IsEnabled = Boolean(payload, "enabled", true),
            MinHeight = _minimumTargetSize,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        if (payload.TryGetProperty("options", out JsonElement options)
            && options.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement option in options.EnumerateArray())
            {
                var item = new ComboBoxItem
                {
                    Content = String(option, "label"),
                    Tag = String(option, "id"),
                };
                choice.Items.Add(item);
                if ((string)item.Tag == selected)
                    choice.SelectedItem = item;
            }
        }
        AutomationProperties.SetAutomationId(choice, bindingId);
        ApplyAccessibility(choice, payload);
        choice.SelectionChanged += (_, _) =>
            EmitChoice(bindingId, (choice.SelectedItem as ComboBoxItem)?.Tag as string);
        container.Children.Add(choice);
        return container;
    }

    private FrameworkElement RenderSlider(JsonElement payload)
    {
        string bindingId = String(payload, "binding_id");
        var container = FieldContainer(String(payload, "label"));
        var slider = new Slider
        {
            Minimum = Number(payload, "minimum", 0),
            Maximum = Number(payload, "maximum", 1),
            Value = Number(payload, "value", 0),
            StepFrequency = Math.Max(0.0001, Number(payload, "step", 0.01)),
            MinHeight = _minimumTargetSize,
        };
        AutomationProperties.SetAutomationId(slider, bindingId);
        ApplyAccessibility(slider, payload);
        slider.ValueChanged += (_, args) => EmitNumber(bindingId, args.NewValue);
        container.Children.Add(slider);
        return container;
    }

    private FrameworkElement RenderProgress(JsonElement payload)
    {
        var container = FieldContainer(String(payload, "label"));
        var progress = new ProgressBar { Minimum = 0, Maximum = 1 };
        if (payload.TryGetProperty("value", out JsonElement value)
            && value.TryGetDouble(out double number))
        {
            progress.Value = number;
        }
        else
        {
            progress.IsIndeterminate = true;
        }
        ApplyAccessibility(progress, payload);
        container.Children.Add(progress);
        return container;
    }

    private static StackPanel FieldContainer(string label)
    {
        var container = new StackPanel { Spacing = 4 };
        if (label.Length > 0)
            container.Children.Add(new TextBlock { Text = label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        return container;
    }

    private static double Number(JsonElement value, string property, double fallback) =>
        value.TryGetProperty(property, out JsonElement element)
        && element.TryGetDouble(out double number)
            ? number
            : fallback;
}
