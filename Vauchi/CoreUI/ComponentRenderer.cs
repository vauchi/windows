// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
#if !UNIT_TEST_BUILD
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vauchi.CoreUI.Components;
#endif

namespace Vauchi.CoreUI;

/// <summary>
/// Static dispatch from JSON component variant to XAML component.
/// Handles serde externally-tagged enum format:
/// Object variant: {"Text": {"id": ...}} — first property is the discriminator
/// String variant: "Divider" — the string value is the discriminator
/// </summary>
public static class ComponentRenderer
{
    /// <summary>
    /// Extract variant name and inner data from an externally-tagged serde enum element.
    /// </summary>
    public static (string? variantName, JsonElement? data) ExtractVariant(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return (element.GetString(), null);

        if (element.ValueKind == JsonValueKind.Object)
        {
            using var enumerator = element.EnumerateObject();
            if (enumerator.MoveNext())
                return (enumerator.Current.Name, enumerator.Current.Value);
        }

        return (null, null);
    }

#if !UNIT_TEST_BUILD
    /// <summary>
    /// Creates the appropriate UI component based on the serde variant name.
    /// Returns null for unknown variants (forward compatibility).
    /// </summary>
    public static UIElement? CreateComponent(JsonElement component, Action<string>? onAction = null)
    {
        var (variantName, data) = ExtractVariant(component);
        if (variantName == null) return null;

        return variantName switch
        {
            "Text" => CreateAndRender<TextComponent>(data!.Value, onAction),
            "TextInput" => CreateAndRender<TextInputComponent>(data!.Value, onAction),
            "ToggleList" => CreateAndRender<ToggleListComponent>(data!.Value, onAction),
            "FieldList" => CreateAndRender<FieldListComponent>(data!.Value, onAction),
            "Preview" => CreateAndRender<PreviewComponent>(data!.Value, onAction),
            "InfoPanel" => CreateAndRender<InfoPanelComponent>(data!.Value, onAction),
            "List" => CreateAndRender<ListComponent>(data!.Value, onAction),
            "SettingsGroup" => CreateAndRender<SettingsGroupComponent>(data!.Value, onAction),
            "ActionList" => CreateAndRender<ActionListComponent>(data!.Value, onAction),
            "SectionedActionList" => CreateAndRender<SectionedActionListComponent>(data!.Value, onAction),
            "StatusIndicator" => CreateAndRender<StatusIndicatorComponent>(data!.Value, onAction),
            "Indicator" => CreateAndRender<IndicatorComponent>(data!.Value, onAction),
            "PinInput" => CreateAndRender<PinInputComponent>(data!.Value, onAction),
            "QrCode" => CreateAndRender<QrCodeComponent>(data!.Value, onAction),
            "ConfirmationDialog" => CreateAndRender<ConfirmationDialogComponent>(data!.Value, onAction),
            "ShowToast" => CreateAndRender<ShowToastComponent>(data!.Value, onAction),
            "InlineConfirm" => CreateAndRender<InlineConfirmComponent>(data!.Value, onAction),
            "EditableText" => CreateAndRender<EditableTextComponent>(data!.Value, onAction),
            "Banner" => CreateAndRender<BannerComponent>(data!.Value, onAction),
            "AvatarPreview" => CreateAndRender<AvatarPreviewComponent>(data!.Value, onAction),
            "Slider" => CreateAndRender<SliderComponent>(data!.Value, onAction),
            "Dropdown" => CreateAndRender<DropdownComponent>(data!.Value, onAction),
            "Row" => CreateRow(data!.Value, onAction),
            "Divider" => new DividerComponent(),
            _ => null,
        };
    }

    /// <summary>
    /// Builds a horizontal container from a Row component:
    /// {"Row": {"id": ..., "items": [&lt;component&gt;, &lt;component&gt;]}}.
    /// Children render left-to-right. Every child is placed in its own
    /// equal-star Grid column so a child that internally stretches to its
    /// max width (e.g. an ActionList) fills only its weighted slice instead
    /// of overflowing and overlapping its siblings (e.g. a camera preview).
    /// Mirrors android ScreenRenderer.kt's weighted Row.
    /// </summary>
    private static UIElement? CreateRow(JsonElement data, Action<string>? onAction)
    {
        var grid = new Grid();

        if (!data.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
            return grid;

        int column = 0;
        foreach (var child in items.EnumerateArray())
        {
            var control = CreateComponent(child, onAction);
            if (control == null)
                continue; // unknown variant — skip, keep columns contiguous

            // Equal-star column so each child is width-bounded.
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });

            // Constrain the child to its own column; never let a stretched
            // child bleed across the column boundary.
            if (control is FrameworkElement fe)
            {
                fe.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            Grid.SetColumn(control, column);
            grid.Children.Add(control);
            column++;
        }

        return grid;
    }

    private static UIElement CreateAndRender<T>(JsonElement data, Action<string>? onAction)
        where T : UIElement, IRenderable, new()
    {
        var comp = new T();
        comp.Render(data, onAction);
        return comp;
    }
#endif
}

/// <summary>
/// Interface for components that can render from JSON data.
/// </summary>
public interface IRenderable
{
    void Render(JsonElement data, Action<string>? onAction);
}
