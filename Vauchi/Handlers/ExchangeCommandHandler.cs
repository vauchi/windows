// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Vauchi.Helpers;
using Vauchi.Interop;
using Vauchi.Platform;
using Vauchi.Services;
using Windows.Storage.Pickers;

namespace Vauchi.Handlers;

/// <summary>
/// Generic <c>ExchangeCommand</c> dispatcher (ADR-031).
///
/// Translates core-emitted <see cref="ExchangeCommand"/>s into native
/// Windows side-effects (BLE start/scan, audio emit/listen, file
/// pickers, etc.) and routes the resulting hardware events back to
/// core via the injected <c>sendHardwareEvent</c> callback.
///
/// The handler is the Humble Object on the Windows side of ADR-031:
/// it owns no business logic, just the platform plumbing. All
/// transports that don't exist on Windows desktop (NFC, camera
/// capture / library, share-sheet, screen-brightness, idle-timer,
/// orientation-lock, switch-camera) answer
/// <c>HardwareUnavailable</c>; core uses this signal to fall back
/// to another transport without retrying.
/// </summary>
public sealed class ExchangeCommandHandler : IDisposable
{
    private readonly Action<string> _sendHardwareEvent;
    private readonly DispatcherQueue _dispatcher;
    private readonly Window _window;
    private BleExchangeService? _ble;

    /// <summary>
    /// Creates the handler and starts its BLE adapter availability
    /// probe. The dispatcher is used to marshal background-thread
    /// hardware events (BLE / audio listen) onto the UI thread before
    /// they reach <paramref name="sendHardwareEvent"/>.
    /// </summary>
    /// <param name="sendHardwareEvent">
    /// Callback that takes a serialized
    /// <see cref="ExchangeHardwareEventJson"/> payload, forwards it
    /// to <c>VauchiNative.AppHandleHardwareEvent</c>, and routes the
    /// returned <c>ActionResult</c> JSON. The callback is responsible
    /// for the <c>_appHandle == IntPtr.Zero</c> guard so this class
    /// stays free of CABI-handle awareness.
    /// </param>
    /// <param name="dispatcher">UI-thread dispatcher.</param>
    /// <param name="window">
    /// The hosting <see cref="Window"/> — used by WinUI 3 file
    /// pickers to attach to the parent HWND.
    /// </param>
    public ExchangeCommandHandler(
        Action<string> sendHardwareEvent,
        DispatcherQueue dispatcher,
        Window window)
    {
        _sendHardwareEvent = sendHardwareEvent;
        _dispatcher = dispatcher;
        _window = window;
        _ble = new BleExchangeService(OnBleHardwareEvent);
        _ = _ble.CheckAvailabilityAsync();
    }

    /// <summary>True iff the underlying BLE adapter is present and
    /// the user has granted permission. Mirrors
    /// <see cref="BleExchangeService.IsAvailable"/>.</summary>
    public bool IsBleAvailable => _ble?.IsAvailable ?? false;

    /// <summary>
    /// Dispatch a batch of <see cref="ExchangeCommand"/>s. The caller
    /// is expected to <c>RefreshScreen()</c> after this returns so QR
    /// display / scan re-renders pick up the latest screen state.
    /// </summary>
    public void Handle(ExchangeCommand[] commands)
    {
        foreach (var cmd in commands)
        {
            switch (cmd.Kind)
            {
                case ExchangeCommandKind.QrDisplay:
                case ExchangeCommandKind.QrRequestScan:
                    // QrCodeComponent handles display/scan modes via screen JSON.
                    // The caller's RefreshScreen() picks up the updated component.
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
                    SendHardwareUnavailable("NFC");
                    break;

                case ExchangeCommandKind.DirectSend:
                    HandleDirectSend(cmd);
                    break;

                case ExchangeCommandKind.ImagePickFromFile:
                    HandleImagePickFromFile();
                    break;

                case ExchangeCommandKind.ImageCaptureFromCamera:
                    SendHardwareUnavailable("Camera");
                    break;

                case ExchangeCommandKind.ImagePickFromLibrary:
                    SendHardwareUnavailable("PhotoLibrary");
                    break;

                // Phase 2b screen-presentation lifecycle commands.
                // Windows desktop has no programmatic brightness
                // control (user owns it via system settings) and the
                // OS owns idle-timer / sleep behaviour. Answer
                // HardwareUnavailable so core does not retry; the
                // command/event protocol treats this as "request
                // honoured at platform default."
                case ExchangeCommandKind.SetScreenBrightness:
                    SendHardwareUnavailable("screen_brightness");
                    break;

                case ExchangeCommandKind.SetIdleTimerDisabled:
                    SendHardwareUnavailable("idle_timer");
                    break;

                // ShowShareSheet has no Windows equivalent (apps
                // copy/paste URLs); SwitchCamera is mobile-only
                // (front/rear distinction).
                case ExchangeCommandKind.ShowShareSheet:
                    SendHardwareUnavailable("share_sheet");
                    break;

                case ExchangeCommandKind.SwitchCamera:
                    SendHardwareUnavailable("camera_switch");
                    break;

                // Orientation lock is a mobile concept — desktop
                // windows are user-resizable and don't rotate.
                case ExchangeCommandKind.SetOrientationLock:
                    SendHardwareUnavailable("orientation_lock");
                    break;

                // ADR-031 file-picker (vCard / backup import). Bytes
                // flow back via FilePickedFromUser; cancellation via
                // FilePickCancelledByUser. Core takes the bytes from
                // there (`AppEngine::handle_file_picked`).
                case ExchangeCommandKind.FilePickFromUser:
                    HandleFilePickFromUser(cmd);
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine(
                        $"[Vauchi] Unknown exchange command: {cmd.Kind}");
                    break;
            }
        }
    }

    public void Dispose()
    {
        _ble?.Dispose();
        _ble = null;
    }

    private void OnBleHardwareEvent(string eventJson)
    {
        // BLE events arrive on background threads — dispatch to UI thread
        _dispatcher.TryEnqueue(() => _sendHardwareEvent(eventJson));
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
                        _dispatcher.TryEnqueue(() => SendHardwareUnavailable("Audio"));
                    }
                });
                break;

            case ExchangeCommandKind.AudioListenForResponse:
                long t = cmd.GetLong("timeout_ms");
                ulong timeoutMs = t > 0 ? (ulong)t : 5000;
                System.Threading.Tasks.Task.Run(() =>
                {
                    string? json = VauchiNative.AudioListen(timeoutMs);
                    _dispatcher.TryEnqueue(() =>
                    {
                        if (json == null) return;
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
                                _sendHardwareEvent(
                                    ExchangeHardwareEventJson.AudioResponseReceived(bytes));
                            }
                        }
                        catch (Exception ex)
                        {
                            SendHardwareUnavailable("Audio");
                            System.Diagnostics.Debug.WriteLine(
                                $"[Vauchi] Audio response parse error: {ex.Message}");
                        }
                    });
                });
                break;

            case ExchangeCommandKind.AudioStop:
                System.Threading.Tasks.Task.Run(() => VauchiNative.AudioStop());
                break;
        }
    }

    private async void HandleImagePickFromFile()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".webp");

            // WinUI 3 requires initializing the picker with the window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                _sendHardwareEvent(ExchangeHardwareEventJson.ImagePickCancelled());
                return;
            }

            byte[] imageBytes;
            using (var stream = await file.OpenReadAsync())
            {
                imageBytes = new byte[stream.Size];
                using var reader = new Windows.Storage.Streams.DataReader(stream);
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(imageBytes);
            }

            _sendHardwareEvent(ExchangeHardwareEventJson.ImageReceived(imageBytes));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] Image pick failed: {ex.Message}");
            SendHardwareUnavailable("FilePicker");
        }
    }

    private async void HandleFilePickFromUser(ExchangeCommand cmd)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            string[] mimeTypes = cmd.GetStringArray("accepted_mime_types");
            foreach (string ext in MimeTypeMapper.ToFileExtensions(mimeTypes))
                picker.FileTypeFilter.Add(ext);
            if (picker.FileTypeFilter.Count == 0)
                picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                _sendHardwareEvent(ExchangeHardwareEventJson.FilePickCancelledByUser());
                return;
            }

            byte[] bytes;
            using (var stream = await file.OpenReadAsync())
            {
                bytes = new byte[stream.Size];
                using var reader = new Windows.Storage.Streams.DataReader(stream);
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(bytes);
            }

            _sendHardwareEvent(ExchangeHardwareEventJson.FilePickedFromUser(bytes, file.Name));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] File pick failed: {ex.Message}");
            SendHardwareUnavailable("file_picker");
        }
    }

    private async void HandleDirectSend(ExchangeCommand cmd)
    {
        var payload = cmd.GetBytes("payload") ?? Array.Empty<byte>();
        var isInitiator = cmd.GetBool("is_initiator");
        var service = new DirectSendService();

        service.OnPayloadReceived += eventJson =>
        {
            _dispatcher.TryEnqueue(() => _sendHardwareEvent(eventJson));
        };

        service.OnError += (transport, error) =>
        {
            _dispatcher.TryEnqueue(() =>
                _sendHardwareEvent(ExchangeHardwareEventJson.HardwareError(transport, error)));
        };

        var address = $"127.0.0.1:{DirectSendService.DefaultPort}";
        await service.ExchangeAsync(address, payload, isInitiator);
    }

    private void SendHardwareUnavailable(string transport) =>
        _sendHardwareEvent(ExchangeHardwareEventJson.HardwareUnavailable(transport));
}
