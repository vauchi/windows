// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using System;
using System.Text.Json;
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
    public static UIElement? CreateComponent(JsonElement component, Action<string>? onAction = null)
    {
        if (!component.TryGetProperty("type", out var typeProp))
            return null;

        string? type = typeProp.GetString();

        return type switch
        {
            "text" => CreateAndRender<TextComponent>(component, onAction),
            "text_input" => CreateAndRender<TextInputComponent>(component, onAction),
            "toggle_list" => CreateAndRender<ToggleListComponent>(component, onAction),
            "field_list" => CreateAndRender<FieldListComponent>(component, onAction),
            "card_preview" => CreateAndRender<CardPreviewComponent>(component, onAction),
            "info_panel" => CreateAndRender<InfoPanelComponent>(component, onAction),
            "contact_list" => CreateAndRender<ContactListComponent>(component, onAction),
            "settings_group" => CreateAndRender<SettingsGroupComponent>(component, onAction),
            "action_list" => CreateAndRender<ActionListComponent>(component, onAction),
            "status_indicator" => CreateAndRender<StatusIndicatorComponent>(component, onAction),
            "pin_input" => CreateAndRender<PinInputComponent>(component, onAction),
            "qr_code" => CreateAndRender<QrCodeComponent>(component, onAction),
            "confirmation_dialog" => CreateAndRender<ConfirmationDialogComponent>(component, onAction),
            "divider" => CreateAndRender<DividerComponent>(component, onAction),
            "editable_text" => CreateAndRender<EditableTextComponent>(component, onAction),
            "inline_confirm" => CreateAndRender<InlineConfirmComponent>(component, onAction),
            "show_toast" => CreateAndRender<ShowToastComponent>(component, onAction),
            _ => null,
        };
    }

    private static UIElement CreateAndRender<T>(JsonElement data, Action<string>? onAction)
        where T : UIElement, IRenderable, new()
    {
        var component = new T();
        component.Render(data, onAction);
        return component;
    }
}

/// <summary>
/// Interface for components that can render from JSON data.
/// </summary>
public interface IRenderable
{
    void Render(JsonElement data, Action<string>? onAction);
}
