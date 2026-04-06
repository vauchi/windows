// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vauchi.Interop;

namespace Vauchi;

/// <summary>
/// Device link orchestration — relay listener, protocol handshake,
/// and UI engine transitions for the device linking flow.
/// </summary>
public sealed partial class MainWindow
{
    private IntPtr _deviceLinkInitiator;
    private CancellationTokenSource? _deviceLinkCts;

    /// <summary>
    /// Starts the full device link initiator flow:
    /// 1. Creates an initiator (generates QR)
    /// 2. Navigates to the device_linking screen
    /// 3. Starts a background relay listener
    /// 4. On peer connect: prepares confirmation, transitions UI to VerifyCode
    /// </summary>
    private void StartDeviceLinkFlow()
    {
        // Create protocol-level initiator
        _deviceLinkInitiator = VauchiNative.DeviceLinkStart(_appHandle);
        if (_deviceLinkInitiator == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("[Vauchi] DeviceLink: failed to create initiator");
            return;
        }

        // Navigate UI engine to device_linking screen (shows QR)
        VauchiNative.AppNavigateTo(_appHandle, "device_linking");
        SyncNavSelection();
        RefreshScreen();

        // Start background relay listener
        _deviceLinkCts = new CancellationTokenSource();
        _ = ListenForDeviceLinkRequestAsync(_deviceLinkCts.Token);
    }

    /// <summary>
    /// Background task that polls the relay for a device link request.
    /// When a peer connects, prepares confirmation and transitions the UI.
    /// </summary>
    private async Task ListenForDeviceLinkRequestAsync(CancellationToken ct)
    {
        try
        {
            // Blocking relay call — run on thread pool
            var listenResult = await Task.Run(() =>
                VauchiNative.DeviceLinkListen(_appHandle, 300), ct);

            if (ct.IsCancellationRequested || listenResult == null)
                return;

            var listenJson = JsonDocument.Parse(listenResult);
            if (listenJson.RootElement.TryGetProperty("error", out var errorProp))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Vauchi] DeviceLink listen error: {errorProp.GetString()}");
                return;
            }

            var encryptedPayloadB64 = listenJson.RootElement
                .GetProperty("encrypted_payload").GetString()!;
            var senderToken = listenJson.RootElement
                .GetProperty("sender_token").GetString()!;

            // Prepare confirmation (decrypt request, get verification code)
            var confirmResult = VauchiNative.DeviceLinkPrepareConfirmation(
                _deviceLinkInitiator, encryptedPayloadB64);
            if (confirmResult == null)
                return;

            var confirmJson = JsonDocument.Parse(confirmResult);
            if (confirmJson.RootElement.TryGetProperty("error", out var confirmError))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Vauchi] DeviceLink prepare error: {confirmError.GetString()}");
                return;
            }

            var verificationCode = confirmJson.RootElement
                .GetProperty("confirmation_code").GetString()!;

            // Store sender token for the confirm step
            _deviceLinkSenderToken = senderToken;

            // Transition UI to VerifyCode screen (must run on UI thread)
            DispatcherQueue.TryEnqueue(() =>
            {
                VauchiNative.AppDeviceLinkPeerConnected(_appHandle, verificationCode);
                RefreshScreen();
            });
        }
        catch (OperationCanceledException)
        {
            // Flow cancelled — expected
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] DeviceLink listener error: {ex.Message}");
        }
    }

    private string? _deviceLinkSenderToken;

    /// <summary>
    /// Called when the user confirms the verification code on the VerifyCode screen.
    /// Completes the protocol handshake and sends the response via relay.
    /// </summary>
    private async Task CompleteDeviceLinkAsync()
    {
        if (_deviceLinkInitiator == IntPtr.Zero || _deviceLinkSenderToken == null)
            return;

        try
        {
            // Get the confirmation code from the current screen
            var screenJson = VauchiNative.AppCurrentScreen(_appHandle);
            if (screenJson == null) return;

            var screen = JsonDocument.Parse(screenJson);
            string? code = null;

            // The code is in the Text component with id "code"
            if (screen.RootElement.TryGetProperty("components", out var components))
            {
                foreach (var component in components.EnumerateArray())
                {
                    if (component.TryGetProperty("Text", out var text) &&
                        text.TryGetProperty("id", out var id) &&
                        id.GetString() == "code")
                    {
                        code = text.GetProperty("content").GetString();
                        break;
                    }
                }
            }

            if (code == null) return;

            var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Confirm link with manual verification (compute HMAC in Rust)
            var confirmResult = await Task.Run(() =>
                VauchiNative.DeviceLinkConfirmManual(_deviceLinkInitiator, code, now));

            if (confirmResult == null) return;

            var confirmJson = JsonDocument.Parse(confirmResult);
            if (confirmJson.RootElement.TryGetProperty("error", out var error))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Vauchi] DeviceLink confirm error: {error.GetString()}");
                return;
            }

            var encryptedResponseB64 = confirmJson.RootElement
                .GetProperty("encrypted_response").GetString()!;

            // Send response via relay
            await Task.Run(() =>
                VauchiNative.DeviceLinkSendResponse(
                    _appHandle, _deviceLinkSenderToken, encryptedResponseB64));

            // Transition UI to Complete
            DispatcherQueue.TryEnqueue(() =>
            {
                VauchiNative.AppDeviceLinkSyncComplete(_appHandle);
                RefreshScreen();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] DeviceLink confirm error: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up device link state (initiator handle, cancellation token).
    /// Called when the flow completes or is cancelled.
    /// </summary>
    private void CleanupDeviceLink()
    {
        _deviceLinkCts?.Cancel();
        _deviceLinkCts?.Dispose();
        _deviceLinkCts = null;

        if (_deviceLinkInitiator != IntPtr.Zero)
        {
            VauchiNative.DeviceLinkInitiatorDestroy(_deviceLinkInitiator);
            _deviceLinkInitiator = IntPtr.Zero;
        }

        _deviceLinkSenderToken = null;
    }
}
