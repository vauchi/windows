// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Vauchi.CoreUI.Components;

namespace Vauchi.CoreUI;

/// <summary>
/// Static dispatch from JSON component type to XAML component.
/// Handles serde's externally-tagged enum format:
///   - String variant: "Divider"
///   - Object variant: {"Text": {"id": "...", "content": "..."}}
/// </summary>
public static class ComponentRenderer
{
    /// <summary>
    /// Creates the appropriate UI component from an externally-tagged JSON element.
    /// </summary>
    public static UIElement? CreateComponent(JsonElement component)
    {
        // String variant (e.g. "Divider")
        if (component.ValueKind == JsonValueKind.String)
        {
            string? variant = component.GetString();
            return DispatchVariant(variant, default);
        }

        // Object variant (e.g. {"Text": {"id": "...", "content": "..."}})
        if (component.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in component.EnumerateObject())
            {
                return DispatchVariant(property.Name, property.Value);
            }
        }

        return null;
    }

    private static UIElement? DispatchVariant(string? variantName, JsonElement data)
    {
        return variantName switch
        {
            "Text" => CreateAndRender<TextComponent>(data),
            "TextInput" => CreateAndRender<TextInputComponent>(data),
            "ToggleList" => CreateAndRender<ToggleListComponent>(data),
            "FieldList" => CreateAndRender<FieldListComponent>(data),
            "CardPreview" => CreateAndRender<CardPreviewComponent>(data),
            "InfoPanel" => CreateAndRender<InfoPanelComponent>(data),
            "ContactList" => CreateAndRender<ContactListComponent>(data),
            "SettingsGroup" => CreateAndRender<SettingsGroupComponent>(data),
            "ActionList" => CreateAndRender<ActionListComponent>(data),
            "StatusIndicator" => CreateAndRender<StatusIndicatorComponent>(data),
            "PinInput" => CreateAndRender<PinInputComponent>(data),
            "QrCode" => CreateAndRender<QrCodeComponent>(data),
            "ConfirmationDialog" => CreateAndRender<ConfirmationDialogComponent>(data),
            "Divider" => CreateAndRender<DividerComponent>(data),
            "ShowToast" => CreateAndRender<ShowToastComponent>(data),
            "InlineConfirm" => CreateAndRender<InlineConfirmComponent>(data),
            "EditableText" => CreateAndRender<EditableTextComponent>(data),
            _ => null,
        };
    }

    private static UIElement CreateAndRender<T>(JsonElement data)
        where T : UIElement, IRenderable, new()
    {
        var component = new T();
        component.Render(data);
        return component;
    }
}

/// <summary>
/// Interface for components that can render from JSON data.
/// </summary>
public interface IRenderable
{
    void Render(JsonElement data);

    /// <summary>
    /// Raised when the component triggers a user action (e.g. button click)
    /// that should be forwarded to the core engine.
    /// </summary>
    event EventHandler<string>? ActionRequested;
}
