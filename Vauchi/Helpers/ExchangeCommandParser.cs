// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;

namespace Vauchi.Helpers;

public enum ExchangeCommandKind
{
    QrDisplay,
    QrRequestScan,
    BleStartAdvertising,
    BleStartScanning,
    BleConnect,
    BleWriteCharacteristic,
    BleReadCharacteristic,
    BleDisconnect,
    NfcActivate,
    NfcDeactivate,
    AudioEmitChallenge,
    AudioListenForResponse,
    AudioStop,
    DirectSend,
    ImagePickFromFile,
    ImageCaptureFromCamera,
    ImagePickFromLibrary,
    // Phase 2b screen-presentation commands. Windows desktop has no
    // programmatic brightness control and the OS owns idle-timer /
    // sleep behaviour; the dispatcher answers HardwareUnavailable.
    SetScreenBrightness,
    SetIdleTimerDisabled,
    // Phase 0 additions: ShowShareSheet has no Windows equivalent
    // (apps copy/paste URLs); SwitchCamera is mobile-only (front/rear).
    ShowShareSheet,
    SwitchCamera,
    Unknown,
}

public readonly struct ExchangeCommand
{
    public ExchangeCommandKind Kind { get; }
    private readonly JsonElement _payload;
    private readonly bool _hasPayload;

    internal ExchangeCommand(ExchangeCommandKind kind, JsonElement payload)
    {
        Kind = kind;
        _payload = payload;
        _hasPayload = true;
    }

    internal ExchangeCommand(ExchangeCommandKind kind)
    {
        Kind = kind;
        _payload = default;
        _hasPayload = false;
    }

    public string? GetString(string field)
    {
        if (!_hasPayload) return null;
        return _payload.TryGetProperty(field, out var v) ? v.GetString() : null;
    }

    public byte[]? GetBytes(string field)
    {
        if (!_hasPayload || !_payload.TryGetProperty(field, out var arr))
            return null;
        if (arr.ValueKind != JsonValueKind.Array)
            return null;

        var result = new byte[arr.GetArrayLength()];
        int i = 0;
        foreach (var el in arr.EnumerateArray())
            result[i++] = (byte)el.GetInt32();
        return result;
    }

    public long GetLong(string field)
    {
        if (!_hasPayload || !_payload.TryGetProperty(field, out var v))
            return 0;
        return v.GetInt64();
    }

    public bool GetBool(string field)
    {
        if (!_hasPayload || !_payload.TryGetProperty(field, out var v))
            return false;
        return v.GetBoolean();
    }
}

public static class ExchangeCommandParser
{
    private static readonly string[] KnownVariants =
    [
        "QrDisplay", "QrRequestScan",
        "BleStartAdvertising", "BleStartScanning", "BleConnect",
        "BleWriteCharacteristic", "BleReadCharacteristic", "BleDisconnect",
        "NfcActivate", "NfcDeactivate",
        "AudioEmitChallenge", "AudioListenForResponse", "AudioStop",
        "DirectSend",
        "ImagePickFromFile", "ImageCaptureFromCamera", "ImagePickFromLibrary",
        "SetScreenBrightness", "SetIdleTimerDisabled",
        "ShowShareSheet", "SwitchCamera",
    ];

    /// <summary>
    /// Parse a single ExchangeCommand from a JsonElement.
    /// Clones the payload so the result survives JsonDocument disposal.
    /// </summary>
    public static ExchangeCommand Parse(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            string? name = element.GetString();
            return new ExchangeCommand(NameToKind(name));
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in KnownVariants)
            {
                if (element.TryGetProperty(name, out var payload))
                    return new ExchangeCommand(NameToKind(name), payload.Clone());
            }
        }

        return new ExchangeCommand(ExchangeCommandKind.Unknown);
    }

    public static ExchangeCommand[] ParseAll(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;
        if (arr.ValueKind != JsonValueKind.Array)
            return [];

        var result = new ExchangeCommand[arr.GetArrayLength()];
        int i = 0;
        foreach (var el in arr.EnumerateArray())
            result[i++] = Parse(el);
        return result;
    }

    public static ExchangeCommand[] ParseFromActionResult(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        // ActionResult variant key was renamed `ExchangeCommands` → `Commands`
        // in core 0.47.0 (Phase 0 of
        // 2026-05-04-exchange-command-screen-presentation).
        if (doc.RootElement.TryGetProperty("Commands", out var outer)
            && outer.TryGetProperty("commands", out var cmds))
        {
            var result = new ExchangeCommand[cmds.GetArrayLength()];
            int i = 0;
            foreach (var el in cmds.EnumerateArray())
                result[i++] = Parse(el);
            return result;
        }
        return [];
    }

    private static ExchangeCommandKind NameToKind(string? name) => name switch
    {
        "QrDisplay" => ExchangeCommandKind.QrDisplay,
        "QrRequestScan" => ExchangeCommandKind.QrRequestScan,
        "BleStartAdvertising" => ExchangeCommandKind.BleStartAdvertising,
        "BleStartScanning" => ExchangeCommandKind.BleStartScanning,
        "BleConnect" => ExchangeCommandKind.BleConnect,
        "BleWriteCharacteristic" => ExchangeCommandKind.BleWriteCharacteristic,
        "BleReadCharacteristic" => ExchangeCommandKind.BleReadCharacteristic,
        "BleDisconnect" => ExchangeCommandKind.BleDisconnect,
        "NfcActivate" => ExchangeCommandKind.NfcActivate,
        "NfcDeactivate" => ExchangeCommandKind.NfcDeactivate,
        "AudioEmitChallenge" => ExchangeCommandKind.AudioEmitChallenge,
        "AudioListenForResponse" => ExchangeCommandKind.AudioListenForResponse,
        "AudioStop" => ExchangeCommandKind.AudioStop,
        "DirectSend" => ExchangeCommandKind.DirectSend,
        "ImagePickFromFile" => ExchangeCommandKind.ImagePickFromFile,
        "ImageCaptureFromCamera" => ExchangeCommandKind.ImageCaptureFromCamera,
        "ImagePickFromLibrary" => ExchangeCommandKind.ImagePickFromLibrary,
        "SetScreenBrightness" => ExchangeCommandKind.SetScreenBrightness,
        "SetIdleTimerDisabled" => ExchangeCommandKind.SetIdleTimerDisabled,
        "ShowShareSheet" => ExchangeCommandKind.ShowShareSheet,
        "SwitchCamera" => ExchangeCommandKind.SwitchCamera,
        _ => ExchangeCommandKind.Unknown,
    };
}
