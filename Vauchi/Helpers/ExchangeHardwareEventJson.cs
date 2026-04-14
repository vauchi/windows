// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO;
using System.Text.Json;

namespace Vauchi.Helpers;

/// <summary>
/// Builds ExchangeHardwareEvent JSON matching the vauchi-cabi serde format (ADR-031).
/// Format: {"VariantName":{"field":"value"}} — externally-tagged Rust enum.
/// byte[] fields are written as int arrays [1,2,3], NOT Base64 (Rust Vec&lt;u8&gt; serde).
/// </summary>
public static class ExchangeHardwareEventJson
{
    public static string QrScanned(string data) =>
        JsonSerializer.Serialize(new { QrScanned = new { data } });

    public static string BleDeviceDiscovered(string id, short rssi, byte[] advData)
    {
        using var stream = new MemoryStream();
        using var w = new Utf8JsonWriter(stream);
        w.WriteStartObject();
        w.WriteStartObject("BleDeviceDiscovered");
        w.WriteString("id", id);
        w.WriteNumber("rssi", rssi);
        WriteByteArray(w, "adv_data", advData);
        w.WriteEndObject();
        w.WriteEndObject();
        w.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string BleConnected(string deviceId) =>
        JsonSerializer.Serialize(new { BleConnected = new { device_id = deviceId } });

    public static string BleCharacteristicRead(string uuid, byte[] data) =>
        WriteVariantWithUuidAndData("BleCharacteristicRead", uuid, data);

    public static string BleCharacteristicNotified(string uuid, byte[] data) =>
        WriteVariantWithUuidAndData("BleCharacteristicNotified", uuid, data);

    public static string BleDisconnected(string reason) =>
        JsonSerializer.Serialize(new { BleDisconnected = new { reason } });

    public static string NfcDataReceived(byte[] data) =>
        WriteVariantWithData("NfcDataReceived", data);

    public static string AudioResponseReceived(byte[] data) =>
        WriteVariantWithData("AudioResponseReceived", data);

    public static string DirectPayloadReceived(byte[] data) =>
        WriteVariantWithData("DirectPayloadReceived", data);

    public static string HardwareError(string transport, string error) =>
        JsonSerializer.Serialize(new { HardwareError = new { transport, error } });

    public static string HardwareUnavailable(string transport) =>
        JsonSerializer.Serialize(new { HardwareUnavailable = new { transport } });

    public static string ImageReceived(byte[] data) =>
        WriteVariantWithData("ImageReceived", data);

    public static string ImagePickCancelled() => "\"ImagePickCancelled\"";

    private static string WriteVariantWithData(string variant, byte[] data)
    {
        using var stream = new MemoryStream();
        using var w = new Utf8JsonWriter(stream);
        w.WriteStartObject();
        w.WriteStartObject(variant);
        WriteByteArray(w, "data", data);
        w.WriteEndObject();
        w.WriteEndObject();
        w.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string WriteVariantWithUuidAndData(string variant, string uuid, byte[] data)
    {
        using var stream = new MemoryStream();
        using var w = new Utf8JsonWriter(stream);
        w.WriteStartObject();
        w.WriteStartObject(variant);
        w.WriteString("uuid", uuid);
        WriteByteArray(w, "data", data);
        w.WriteEndObject();
        w.WriteEndObject();
        w.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteByteArray(Utf8JsonWriter w, string name, byte[] bytes)
    {
        w.WriteStartArray(name);
        foreach (byte b in bytes)
            w.WriteNumberValue(b);
        w.WriteEndArray();
    }
}
