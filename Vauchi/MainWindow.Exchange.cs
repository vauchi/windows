// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Vauchi.Helpers;
using Vauchi.Interop;

namespace Vauchi;

/// <summary>
/// Exchange hardware command dispatch (ADR-031).
/// Handles BLE, audio, NFC, and QR commands from core, routes hardware
/// events back via AppHandleHardwareEvent.
/// </summary>
public sealed partial class MainWindow
{
    private void HandleExchangeCommands(ExchangeCommand[] commands)
    {
        foreach (var cmd in commands)
        {
            switch (cmd.Kind)
            {
                case ExchangeCommandKind.QrDisplay:
                case ExchangeCommandKind.QrRequestScan:
                    // QrCodeComponent handles display/scan modes via screen JSON.
                    // RefreshScreen() is called after this method, which
                    // re-renders the exchange screen with the updated QR component.
                    break;

                case ExchangeCommandKind.BleStartAdvertising:
                case ExchangeCommandKind.BleStartScanning:
                case ExchangeCommandKind.BleConnect:
                case ExchangeCommandKind.BleWriteCharacteristic:
                case ExchangeCommandKind.BleReadCharacteristic:
                case ExchangeCommandKind.BleDisconnect:
                    HandleBleCommand(cmd);
                    break;

                case ExchangeCommandKind.AudioEmitChallenge:
                case ExchangeCommandKind.AudioListenForResponse:
                case ExchangeCommandKind.AudioStop:
                    HandleAudioCommand(cmd);
                    break;

                case ExchangeCommandKind.NfcActivate:
                case ExchangeCommandKind.NfcDeactivate:
                    // NFC not available on desktop — report unavailable
                    SendHardwareUnavailable("NFC");
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine(
                        $"[Vauchi] Unknown exchange command: {cmd.Kind}");
                    break;
            }
        }
    }

    private void OnBleHardwareEvent(string eventJson)
    {
        // BLE events arrive on background threads — dispatch to UI thread
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_appHandle == IntPtr.Zero) return;
            string? resultJson = VauchiNative.AppHandleHardwareEvent(_appHandle, eventJson);
            if (resultJson != null) HandleActionResult(resultJson);
        });
    }

    private void HandleBleCommand(ExchangeCommand cmd)
    {
        if (_ble == null || !_ble.IsAvailable)
        {
            SendHardwareUnavailable("BLE");
            return;
        }

        switch (cmd.Kind)
        {
            case ExchangeCommandKind.BleStartScanning:
                _ble.StartScanning(cmd.GetString("service_uuid") ?? "");
                break;
            case ExchangeCommandKind.BleStartAdvertising:
                _ble.StartAdvertising(
                    cmd.GetString("service_uuid") ?? "",
                    cmd.GetBytes("payload") ?? Array.Empty<byte>());
                break;
            case ExchangeCommandKind.BleConnect:
                _ = _ble.ConnectAsync(cmd.GetString("device_id") ?? "");
                break;
            case ExchangeCommandKind.BleWriteCharacteristic:
                _ = _ble.WriteCharacteristicAsync(
                    cmd.GetString("uuid") ?? "",
                    cmd.GetBytes("data") ?? Array.Empty<byte>());
                break;
            case ExchangeCommandKind.BleReadCharacteristic:
                _ = _ble.ReadCharacteristicAsync(cmd.GetString("uuid") ?? "");
                break;
            case ExchangeCommandKind.BleDisconnect:
                _ble.Disconnect();
                break;
        }
    }

    private void HandleAudioCommand(ExchangeCommand cmd)
    {
        // Audio commands block — run on background thread
        switch (cmd.Kind)
        {
            case ExchangeCommandKind.AudioEmitChallenge:
                var emitData = cmd.GetBytes("data") ?? Array.Empty<byte>();
                System.Threading.Tasks.Task.Run(() =>
                {
                    int ok = VauchiNative.AudioEmit(emitData, (nuint)emitData.Length);
                    if (ok != 1)
                    {
                        DispatcherQueue.TryEnqueue(() => SendHardwareUnavailable("Audio"));
                    }
                });
                break;

            case ExchangeCommandKind.AudioListenForResponse:
                long t = cmd.GetLong("timeout_ms");
                ulong timeoutMs = t > 0 ? (ulong)t : 5000;
                System.Threading.Tasks.Task.Run(() =>
                {
                    string? json = VauchiNative.AudioListen(timeoutMs);
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_appHandle == IntPtr.Zero) return;
                        if (json != null)
                        {
                            // Parse data array from {"data":[1,2,...]}
                            try
                            {
                                using var doc = JsonDocument.Parse(json);
                                if (doc.RootElement.TryGetProperty("data", out var arr))
                                {
                                    byte[] bytes = new byte[arr.GetArrayLength()];
                                    int i = 0;
                                    foreach (var elem in arr.EnumerateArray())
                                        bytes[i++] = (byte)elem.GetInt32();
                                    string eventJson = ExchangeHardwareEventJson.AudioResponseReceived(bytes);
                                    string? resultJson = VauchiNative.AppHandleHardwareEvent(_appHandle, eventJson);
                                    if (resultJson != null) HandleActionResult(resultJson);
                                }
                            }
                            catch (Exception ex)
                            {
                                SendHardwareUnavailable("Audio");
                                System.Diagnostics.Debug.WriteLine(
                                    $"[Vauchi] Audio response parse error: {ex.Message}");
                            }
                        }
                    });
                });
                break;

            case ExchangeCommandKind.AudioStop:
                System.Threading.Tasks.Task.Run(() => VauchiNative.AudioStop());
                break;
        }
    }

    private void SendHardwareUnavailable(string transport)
    {
        if (_appHandle == IntPtr.Zero) return;
        string eventJson = ExchangeHardwareEventJson.HardwareUnavailable(transport);
        string? resultJson = VauchiNative.AppHandleHardwareEvent(_appHandle, eventJson);
        if (resultJson != null) HandleActionResult(resultJson);
    }
}
