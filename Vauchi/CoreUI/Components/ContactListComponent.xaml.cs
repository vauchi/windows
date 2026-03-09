// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class ContactListComponent : UserControl, IRenderable
{
    public ContactListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        // TODO: Build contact list items from data["contacts"]
        if (data.TryGetProperty("title", out var title))
        {
            Placeholder.Text = title.GetString() ?? "[ContactList]";
        }
    }
}
