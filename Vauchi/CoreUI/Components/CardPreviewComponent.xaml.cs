// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class CardPreviewComponent : UserControl, IRenderable
{
    public CardPreviewComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        // TODO: Render contact card preview from data
        if (data.TryGetProperty("name", out var name))
        {
            Placeholder.Text = name.GetString() ?? "[CardPreview]";
        }
    }
}
