// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Text.Json;
using Vauchi.Helpers;
using Windows.System;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextInputComponent : UserControl, IRenderable
{
    private Action<string>? _onAction;
    private string _componentId = "";

    public TextInputComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        _componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        _onAction = onAction;

        // Label
        if (data.TryGetProperty("label", out var label))
            LabelText.Text = label.GetString() ?? "";

        // Value
        string value = data.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";

        // Placeholder
        string placeholder = data.TryGetProperty("placeholder", out var ph) ? ph.GetString() ?? "" : "";

        // Max length
        int maxLength = data.TryGetProperty("max_length", out var ml) ? ml.GetInt32() : 0;

        // Input type
        string inputType = data.TryGetProperty("input_type", out var it) ? it.GetString() ?? "Text" : "Text";

        if (inputType == "Password")
        {
            InputBox.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;
            PasswordInput.Password = value;
            PasswordInput.PlaceholderText = placeholder;
            if (maxLength > 0) PasswordInput.MaxLength = maxLength;

            // Send TextChanged on LostFocus only — avoids full re-render on every keystroke
            PasswordInput.LostFocus += (_, _) => SendTextChanged(PasswordInput.Password);
            PasswordInput.KeyDown += OnKeyDown;
        }
        else
        {
            PasswordInput.Visibility = Visibility.Collapsed;
            InputBox.Visibility = Visibility.Visible;
            InputBox.Text = value;
            InputBox.PlaceholderText = placeholder;
            if (maxLength > 0) InputBox.MaxLength = maxLength;

            if (inputType == "Phone")
                InputBox.InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.TelephoneNumber) } };
            else if (inputType == "Email")
                InputBox.InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.EmailNameOrAddress) } };

            // Send TextChanged on LostFocus only — avoids full re-render on every keystroke
            InputBox.LostFocus += (_, _) => SendTextChanged(InputBox.Text);
            InputBox.KeyDown += OnKeyDown;
        }

        // Validation error
        if (data.TryGetProperty("validation_error", out var ve) && ve.ValueKind == JsonValueKind.String)
        {
            ValidationError.Text = ve.GetString() ?? "";
            ValidationError.Visibility = Visibility.Visible;
        }

        // Accessibility
        AutomationProperties.SetName(InputBox, LabelText.Text);
        AutomationProperties.SetName(PasswordInput, LabelText.Text);
    }

    private bool _submitted;

    private void SendTextChanged(string value)
    {
        if (_submitted) return; // Skip LostFocus after Enter-submit (re-render clears field)
        _onAction?.Invoke(ActionJson.TextChanged(_componentId, value));
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            _submitted = true;
            // Send the current value first so core has the latest text
            string currentValue = PasswordInput.Visibility == Visibility.Visible
                ? PasswordInput.Password
                : InputBox.Text;
            _onAction?.Invoke(ActionJson.TextChanged(_componentId, currentValue));
            // Then send a submit action — core handles "submit_<id>" for custom groups, etc.
            _onAction?.Invoke(ActionJson.ActionPressed($"submit_{_componentId}"));
        }
    }
}
