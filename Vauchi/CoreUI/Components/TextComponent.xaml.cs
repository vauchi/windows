// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextComponent : UserControl, IRenderable
{
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
        // TODO: Apply style from data["style"]
    }
}
