// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextInputComponent : UserControl, IRenderable
{
    private DispatcherTimer? _debounce;
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

            PasswordInput.PasswordChanged += (_, _) => DebouncedAction();
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

            InputBox.TextChanged += (_, _) => DebouncedAction();
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

    private void DebouncedAction()
    {
        _debounce?.Stop();
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _debounce.Tick += (_, _) =>
        {
            _debounce?.Stop();
            string currentValue = PasswordInput.Visibility == Visibility.Visible
                ? PasswordInput.Password
                : InputBox.Text;
            _onAction?.Invoke(ActionJson.TextChanged(_componentId, currentValue));
        };
        _debounce.Start();
    }
}
