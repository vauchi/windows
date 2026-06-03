// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

/// <summary>
/// Renders <c>Component::Indicator</c> — a chrome-positioned status chip
/// (icon dot + label). When <c>action_id</c> is present the chip is
/// tappable and emits <c>UserAction::ActionPressed</c>. The chip color
/// derives from <c>IndicatorKind</c> (Active/Error/Neutral/Busy), mapped
/// to the theme palette. Distinct from <c>StatusIndicatorComponent</c>,
/// which renders screen-body progress of in-flight operations.
/// </summary>
public sealed partial class IndicatorComponent : UserControl, IRenderable
{
    public IndicatorComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string label = data.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : "";
        string kind = data.TryGetProperty("kind", out var k) ? k.GetString() ?? "Neutral" : "Neutral";
        string? actionId = data.TryGetProperty("action_id", out var act) && act.ValueKind == JsonValueKind.String
            ? act.GetString()
            : null;

        LabelText.Text = label;

        KindDot.Fill = new SolidColorBrush(kind switch
        {
            "Active" => ThemeColors.Success,
            "Error" => ThemeColors.Destructive,
            "Busy" => ThemeColors.Info,
            _ => ThemeColors.Neutral, // Neutral and unknown
        });

        AutomationProperties.SetName(this, $"{label}: {kind}");

        if (actionId != null && onAction != null)
        {
            string capturedId = actionId;
            Chip.Tapped += (_, _) => onAction(ActionJson.ActionPressed(capturedId));
        }
    }
}
