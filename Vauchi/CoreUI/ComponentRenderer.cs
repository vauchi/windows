// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Microsoft.UI.Xaml;
using Vauchi.CoreUI.Components;

namespace Vauchi.CoreUI;

/// <summary>
/// Static dispatch from JSON component type to XAML component.
/// </summary>
public static class ComponentRenderer
{
    /// <summary>
    /// Creates the appropriate UI component based on the JSON "type" field.
    /// </summary>
    public static UIElement? CreateComponent(JsonElement component)
    {
        if (!component.TryGetProperty("type", out var typeProp))
            return null;

        string? type = typeProp.GetString();

        return type switch
        {
            "text" => CreateAndRender<TextComponent>(component),
            "text_input" => CreateAndRender<TextInputComponent>(component),
            "toggle_list" => CreateAndRender<ToggleListComponent>(component),
            "field_list" => CreateAndRender<FieldListComponent>(component),
            "card_preview" => CreateAndRender<CardPreviewComponent>(component),
            "info_panel" => CreateAndRender<InfoPanelComponent>(component),
            "contact_list" => CreateAndRender<ContactListComponent>(component),
            "settings_group" => CreateAndRender<SettingsGroupComponent>(component),
            "action_list" => CreateAndRender<ActionListComponent>(component),
            "status_indicator" => CreateAndRender<StatusIndicatorComponent>(component),
            "pin_input" => CreateAndRender<PinInputComponent>(component),
            "qr_code" => CreateAndRender<QrCodeComponent>(component),
            "confirmation_dialog" => CreateAndRender<ConfirmationDialogComponent>(component),
            "divider" => CreateAndRender<DividerComponent>(component),
            _ => null,
        };
    }

    private static UIElement CreateAndRender<T>(JsonElement data)
        where T : IRenderable, new()
    {
        var component = new T();
        component.Render(data);
        return (UIElement)component;
    }
}

/// <summary>
/// Interface for components that can render from JSON data.
/// </summary>
public interface IRenderable
{
    void Render(JsonElement data);
}
