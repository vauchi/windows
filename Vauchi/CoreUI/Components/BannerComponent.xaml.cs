// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class BannerComponent : UserControl, IRenderable
{
    public BannerComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        if (data.TryGetProperty("text", out var text))
        {
            Banner.Message = text.GetString() ?? "";
        }

        Banner.ActionButton = null;
        if (data.TryGetProperty("action_label", out var labelEl)
            && data.TryGetProperty("action_id", out var idEl)
            && onAction != null)
        {
            string? label = labelEl.GetString();
            string? actionId = idEl.GetString();
            if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(actionId))
            {
                string capturedId = actionId;
                var button = new Button { Content = label };
                button.Click += (_, _) =>
                    onAction(ActionJson.ActionPressed(capturedId));
                Banner.ActionButton = button;
            }
        }

        if (data.TryGetProperty("a11y", out var a11yElem))
        {
            if (a11yElem.TryGetProperty("label", out var labelElem))
            {
                var a11yLabel = labelElem.GetString();
                if (!string.IsNullOrEmpty(a11yLabel))
                    AutomationProperties.SetName(this, a11yLabel);
            }
            if (a11yElem.TryGetProperty("hint", out var hintElem))
            {
                var hint = hintElem.GetString();
                if (!string.IsNullOrEmpty(hint))
                    AutomationProperties.SetHelpText(this, hint);
            }
        }
    }
}
