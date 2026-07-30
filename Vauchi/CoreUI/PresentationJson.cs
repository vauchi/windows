// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;

namespace Vauchi.CoreUI;

public static class PresentationJson
{
    public static (string? Variant, JsonElement? Payload) Variant(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
            return (value.GetString(), null);
        if (value.ValueKind != JsonValueKind.Object)
            return (null, null);

        JsonElement.ObjectEnumerator properties = value.EnumerateObject();
        if (!properties.MoveNext())
            return (null, null);
        return (properties.Current.Name, properties.Current.Value);
    }

    public static byte[]? Bytes(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
            return null;

        var result = new byte[value.GetArrayLength()];
        int index = 0;
        foreach (JsonElement element in value.EnumerateArray())
        {
            if (!element.TryGetInt32(out int number) || number is < 0 or > 255)
                return null;
            result[index++] = (byte)number;
        }
        return result;
    }
}
