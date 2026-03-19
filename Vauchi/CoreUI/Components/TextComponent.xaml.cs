// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextComponent : UserControl, IRenderable
{
    public TextComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        ContentText.Text = data.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";

        string style = data.TryGetProperty("style", out var s) ? s.GetString() ?? "Body" : "Body";
        ContentText.Style = style switch
        {
            "Title" => (Style)Application.Current.Resources["TitleTextBlockStyle"],
            "Subtitle" => (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            "Caption" => (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            _ => (Style)Application.Current.Resources["BodyTextBlockStyle"],
        };
    }
}
