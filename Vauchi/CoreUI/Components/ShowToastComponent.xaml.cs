// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class ShowToastComponent : UserControl, IRenderable
{
    private DispatcherTimer? _dismissTimer;

    public ShowToastComponent()
    {
        InitializeComponent();
        Unloaded += (_, _) =>
        {
            _dismissTimer?.Stop();
            _dismissTimer = null;
        };
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        _dismissTimer?.Stop();

        if (data.TryGetProperty("message", out var message))
        {
            Toast.Message = message.GetString() ?? "";
        }

        // Severity is not part of the ShowToast data model — always use Informational
        Toast.Severity = InfoBarSeverity.Informational;

        // Wire undo button if undo_action_id is present
        Toast.ActionButton = null;
        if (data.TryGetProperty("undo_action_id", out var undoEl) && onAction != null)
        {
            string? undoId = undoEl.ValueKind == JsonValueKind.String ? undoEl.GetString() : null;
            if (!string.IsNullOrEmpty(undoId))
            {
                string capturedUndoId = undoId;
                var undoButton = new Button { Content = "Undo" };
                undoButton.Click += (_, _) =>
                    onAction(ActionJson.UndoPressed(capturedUndoId));
                Toast.ActionButton = undoButton;
            }
        }

        Toast.IsOpen = true;

        // Use duration_ms from data (default 4000 ms if absent)
        int durationMs = data.TryGetProperty("duration_ms", out var durEl)
            ? durEl.GetInt32()
            : 4000;

        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        _dismissTimer.Tick += (_, _) =>
        {
            _dismissTimer?.Stop();
            Toast.IsOpen = false;
        };
        _dismissTimer.Start();
    }
}
