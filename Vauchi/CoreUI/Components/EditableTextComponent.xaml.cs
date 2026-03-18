// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Vauchi.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class EditableTextComponent : UserControl, IRenderable
{
    private bool _eventWired;

    public EditableTextComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";

        if (data.TryGetProperty("value", out var value))
        {
            EditableBox.Text = value.GetString() ?? "";
        }
        if (data.TryGetProperty("placeholder", out var placeholder))
        {
            EditableBox.PlaceholderText = placeholder.GetString() ?? "";
        }

        if (!_eventWired && onAction != null && componentId.Length > 0)
        {
            EditableBox.TextChanged += (_, _) =>
                onAction(ActionJson.TextChanged(componentId, EditableBox.Text));
            _eventWired = true;
        }
    }
}
