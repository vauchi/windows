// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class PinInputComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";
    private readonly List<TextBox> _digitBoxes = new();
    private bool _masked;

    public PinInputComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        if (data.TryGetProperty("id", out var id))
        {
            _componentId = id.GetString() ?? "";
        }

        if (data.TryGetProperty("label", out var label))
        {
            PinLabel.Text = label.GetString() ?? "Enter PIN";
        }

        _masked = true;
        if (data.TryGetProperty("masked", out var masked))
        {
            _masked = masked.GetBoolean();
        }

        var length = 6;
        if (data.TryGetProperty("length", out var lengthEl))
        {
            length = lengthEl.GetInt32();
        }

        var filled = 0;
        if (data.TryGetProperty("filled", out var filledEl))
        {
            filled = filledEl.GetInt32();
        }

        // Rebuild digit boxes if length changed
        if (_digitBoxes.Count != length)
        {
            DigitContainer.Children.Clear();
            _digitBoxes.Clear();

            for (var i = 0; i < length; i++)
            {
                var digitBox = new TextBox
                {
                    Width = 48,
                    MaxLength = 1,
                    TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                    FontFamily = new FontFamily("Cascadia Mono"),
                    FontSize = 18,
                    InputScope = CreateNumberScope()
                };

                var index = i;
                digitBox.TextChanged += (s, e) => OnDigitChanged(index);

                _digitBoxes.Add(digitBox);
                DigitContainer.Children.Add(digitBox);
            }
        }

        // Apply filled state (show bullets for filled positions if masked)
        for (var i = 0; i < _digitBoxes.Count; i++)
        {
            if (i < filled && string.IsNullOrEmpty(_digitBoxes[i].Text))
            {
                _digitBoxes[i].Text = _masked ? "\u2022" : "";
            }
        }

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

    private void OnDigitChanged(int index)
    {
        var box = _digitBoxes[index];
        var text = box.Text;

        // Mask the character after entry
        if (_masked && text.Length == 1 && text != "\u2022")
        {
            // Store the actual digit in the Tag for aggregation
            box.Tag = text;
            box.Text = "\u2022";
            // Move cursor to end after masking
            box.SelectionStart = 1;
        }
        else if (!_masked && text.Length == 1)
        {
            box.Tag = text;
        }

        // Auto-advance to next digit
        if (text.Length > 0 && index < _digitBoxes.Count - 1)
        {
            _digitBoxes[index + 1].Focus(FocusState.Programmatic);
        }

        EmitPinValue();
    }

    private void EmitPinValue()
    {
        var pin = "";
        foreach (var box in _digitBoxes)
        {
            // Use Tag (actual digit) if available, otherwise the displayed text
            var digit = box.Tag as string ?? box.Text;
            if (digit == "\u2022") digit = ""; // Bullet means no real digit stored
            pin += digit;
        }

        var escapedId = JsonEncodedText.Encode(_componentId);
        var escapedValue = JsonEncodedText.Encode(pin);

        var action = $"{{\"TextChanged\":{{\"component_id\":\"{escapedId}\",\"value\":\"{escapedValue}\"}}}}";
        ActionRequested?.Invoke(this, action);
    }

    private static InputScope CreateNumberScope()
    {
        var scope = new InputScope();
        scope.Names.Add(new InputScopeName(InputScopeNameValue.Number));
        return scope;
    }
}
