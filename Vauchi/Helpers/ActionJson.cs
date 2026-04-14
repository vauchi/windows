// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;

namespace Vauchi.Helpers;

/// <summary>
/// Builds UserAction JSON strings matching the vauchi-cabi serde format.
/// All values are properly JSON-escaped via System.Text.Json.
/// </summary>
public static class ActionJson
{
    public static string ActionPressed(string actionId)
    {
        return JsonSerializer.Serialize(new
        {
            ActionPressed = new { action_id = actionId }
        });
    }

    public static string TextChanged(string componentId, string value)
    {
        return JsonSerializer.Serialize(new
        {
            TextChanged = new { component_id = componentId, value }
        });
    }

    public static string ItemToggled(string componentId, string itemId)
    {
        return JsonSerializer.Serialize(new
        {
            ItemToggled = new { component_id = componentId, item_id = itemId }
        });
    }

    public static string ListItemSelected(string componentId, string itemId)
    {
        return JsonSerializer.Serialize(new
        {
            ListItemSelected = new { component_id = componentId, item_id = itemId }
        });
    }

    public static string SettingsToggled(string componentId, string itemId)
    {
        return JsonSerializer.Serialize(new
        {
            SettingsToggled = new { component_id = componentId, item_id = itemId }
        });
    }

    public static string FieldVisibilityChanged(string fieldId, string? groupId, bool visible)
    {
        return JsonSerializer.Serialize(new
        {
            FieldVisibilityChanged = new { field_id = fieldId, group_id = groupId, visible }
        });
    }

    public static string GroupViewSelected(string? groupName)
    {
        return JsonSerializer.Serialize(new
        {
            GroupViewSelected = new { group_name = groupName }
        });
    }

    public static string SearchChanged(string componentId, string query)
    {
        return JsonSerializer.Serialize(new
        {
            SearchChanged = new { component_id = componentId, query }
        });
    }

    public static string UndoPressed(string actionId)
    {
        return JsonSerializer.Serialize(new
        {
            UndoPressed = new { action_id = actionId }
        });
    }

    public static string SliderChanged(string componentId, int valueMilli)
    {
        return JsonSerializer.Serialize(new
        {
            SliderChanged = new { component_id = componentId, value_milli = valueMilli }
        });
    }
}
