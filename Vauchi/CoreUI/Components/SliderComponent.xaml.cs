// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class SliderComponent : UserControl, IRenderable
{
    public SliderComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

        double min = data.TryGetProperty("min", out var minEl) ? minEl.GetDouble() : 0;
        double max = data.TryGetProperty("max", out var maxEl) ? maxEl.GetDouble() : 1000;
        double value = data.TryGetProperty("value", out var valEl) ? valEl.GetDouble() : min;
        double step = data.TryGetProperty("step", out var stepEl) ? stepEl.GetDouble() : 1;

        SliderControl.Minimum = min;
        SliderControl.Maximum = max;
        SliderControl.Value = value;
        SliderControl.StepFrequency = step;

        if (data.TryGetProperty("label", out var labelEl))
        {
            string? label = labelEl.GetString();
            if (!string.IsNullOrEmpty(label))
            {
                SliderLabel.Text = label;
                SliderLabel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                AutomationProperties.SetName(SliderControl, label);
            }
        }

        if (onAction != null && componentId.Length > 0)
        {
            string capturedId = componentId;
            SliderControl.ValueChanged += (_, args) =>
            {
                int valueMilli = (int)(args.NewValue * 1000);
                onAction(ActionJson.SliderChanged(capturedId, valueMilli));
            };
        }

        if (data.TryGetProperty("a11y", out var a11yElem))
        {
            if (a11yElem.TryGetProperty("label", out var a11yLabelEl))
            {
                var a11yLabel = a11yLabelEl.GetString();
                if (!string.IsNullOrEmpty(a11yLabel))
                    AutomationProperties.SetName(SliderControl, a11yLabel);
            }
        }
    }
}
