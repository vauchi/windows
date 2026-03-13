// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class QrCodeComponent : UserControl, IRenderable
{
    public QrCodeComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        // TODO: Render QR code image from data["payload"]
        if (data.TryGetProperty("payload", out var payload))
        {
            Placeholder.Text = $"[QR: {(payload.GetString() ?? "").Substring(0, System.Math.Min(20, (payload.GetString() ?? "").Length))}...]";
        }
    }
}
