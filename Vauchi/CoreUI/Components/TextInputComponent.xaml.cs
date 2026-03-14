// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextInputComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";
    private readonly DispatcherTimer _debounceTimer;
    private bool _isPasswordMode;
    private bool _eventsWired;

    public TextInputComponent()
    {
        InitializeComponent();
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _debounceTimer.Tick += OnDebounceTimerTick;
    }

    public void Render(JsonElement data)
    {
        if (data.TryGetProperty("id", out var id))
        {
            _componentId = id.GetString() ?? "";
        }

        if (data.TryGetProperty("label", out var label)
            && label.ValueKind != JsonValueKind.Null)
        {
            var labelStr = label.GetString() ?? "";
            if (!string.IsNullOrEmpty(labelStr))
            {
                InputLabel.Text = labelStr;
                InputLabel.Visibility = Visibility.Visible;
            }
            else
            {
                InputLabel.Visibility = Visibility.Collapsed;
            }
        }

        // Determine input type
        var inputType = "Text";
        if (data.TryGetProperty("input_type", out var inputTypeEl))
        {
            inputType = inputTypeEl.GetString() ?? "Text";
        }

        _isPasswordMode = inputType == "Password";

        if (_isPasswordMode)
        {
            InputBox.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;

            if (data.TryGetProperty("placeholder", out var pwPlaceholder))
            {
                PasswordInput.PlaceholderText = pwPlaceholder.GetString() ?? "";
            }

            if (data.TryGetProperty("value", out var pwValue))
            {
                PasswordInput.Password = pwValue.GetString() ?? "";
            }

            if (data.TryGetProperty("max_length", out var pwMaxLen))
            {
                PasswordInput.MaxLength = pwMaxLen.GetInt32();
            }

            if (!_eventsWired)
            {
                PasswordInput.PasswordChanged += OnPasswordChanged;
            }
        }
        else
        {
            InputBox.Visibility = Visibility.Visible;
            PasswordInput.Visibility = Visibility.Collapsed;

            if (data.TryGetProperty("placeholder", out var placeholder))
            {
                InputBox.PlaceholderText = placeholder.GetString() ?? "";
            }

            if (data.TryGetProperty("value", out var value))
            {
                InputBox.Text = value.GetString() ?? "";
            }

            if (data.TryGetProperty("max_length", out var maxLen))
            {
                InputBox.MaxLength = maxLen.GetInt32();
            }

            // Set input scope for non-password types
            if (inputType is "Phone" or "Email")
            {
                var scope = new InputScope();
                var nameValue = inputType switch
                {
                    "Phone" => InputScopeNameValue.TelephoneNumber,
                    "Email" => InputScopeNameValue.EmailNameOrAddress,
                    _ => InputScopeNameValue.Default
                };
                scope.Names.Add(new InputScopeName(nameValue));
                InputBox.InputScope = scope;
            }

            if (!_eventsWired)
            {
                InputBox.TextChanged += OnTextChanged;
            }
        }

        _eventsWired = true;

        // Validation error
        if (data.TryGetProperty("validation_error", out var error)
            && error.ValueKind != JsonValueKind.Null)
        {
            ErrorText.Text = error.GetString() ?? "";
            ErrorText.Visibility = Visibility.Visible;
        }
        else
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceTimerTick(object? sender, object e)
    {
        _debounceTimer.Stop();

        var currentValue = _isPasswordMode ? PasswordInput.Password : InputBox.Text;
        var escapedId = JsonEncodedText.Encode(_componentId);
        var escapedValue = JsonEncodedText.Encode(currentValue);

        var action = $"{{\"TextChanged\":{{\"component_id\":\"{escapedId}\",\"value\":\"{escapedValue}\"}}}}";
        ActionRequested?.Invoke(this, action);
    }
}
