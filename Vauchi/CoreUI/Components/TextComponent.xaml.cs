// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public TextComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        if (data.TryGetProperty("content", out var content))
        {
            ContentText.Text = content.GetString() ?? "";
        }

        if (data.TryGetProperty("style", out var style))
        {
            var styleKey = style.GetString() switch
            {
                "Title" => "TitleTextBlockStyle",
                "Subtitle" => "SubtitleTextBlockStyle",
                "Body" => "BodyTextBlockStyle",
                "Caption" => "CaptionTextBlockStyle",
                _ => "BodyTextBlockStyle"
            };

            if (Application.Current.Resources.TryGetValue(styleKey, out var resourceStyle)
                && resourceStyle is Style winuiStyle)
            {
                ContentText.Style = winuiStyle;
            }
        }
    }
}
