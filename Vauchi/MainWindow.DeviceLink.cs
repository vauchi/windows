// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Threading.Tasks;
using Vauchi.Interop;

namespace Vauchi;

/// <summary>
/// Device link orchestration — thin adapter over <see cref="DeviceLinkBridge"/>.
/// The relay-poll loop, QR-expiry deadline, confirmation prompt timing, and
/// protocol clock all live in core's <c>DeviceLinkSession</c>; this file
/// subscribes to bridge events and forwards UI transitions onto the
/// <see cref="Microsoft.UI.Xaml.Window.DispatcherQueue"/>-based engine.
/// </summary>
public sealed partial class MainWindow
{
    private DeviceLinkBridge? _deviceLinkBridge;
    private string? _deviceLinkVerificationCode;

    /// <summary>
    /// Starts the device link initiator flow:
    /// 1. Creates a session via the CABI bridge (core builds the QR + cycle thread).
    /// 2. Navigates to the device_linking screen so the QR is visible.
    /// 3. Subscribes to bridge events to drive UI transitions.
    /// 4. Calls <c>session.start()</c> to spawn the cycle thread.
    /// </summary>
    private void StartDeviceLinkFlow()
    {
        _deviceLinkBridge = DeviceLinkBridge.Create(_appHandle, DispatcherQueue);
        if (_deviceLinkBridge == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "[Vauchi] DeviceLink: failed to create session (no identity / storage key)");
            return;
        }

        // QR data does not need to flow through here — core renders it via
        // its own ScreenModel for the device_linking screen. We just route
        // the UI to that screen and start the cycle thread.
        VauchiNative.AppNavigateTo(_appHandle, "device_linking");
        SyncNavSelection();
        RefreshScreen();

        _deviceLinkBridge.QrReady += OnDeviceLinkQrReady;
        _deviceLinkBridge.ConfirmationRequired += OnDeviceLinkConfirmationRequired;
        _deviceLinkBridge.Completed += OnDeviceLinkCompleted;
        _deviceLinkBridge.Failed += OnDeviceLinkFailed;
        _deviceLinkBridge.SessionEnded += OnDeviceLinkSessionEnded;

        _deviceLinkBridge.Start();
    }

    private void OnDeviceLinkQrReady(string qrData, ulong expiresAtUnix)
    {
        // The QR is already rendered by core's ScreenModel when we navigate
        // to device_linking. The QR-ready callback exists so future UI
        // could (e.g.) show a countdown to expires_at_unix; today we just
        // log it for diagnostics.
        System.Diagnostics.Debug.WriteLine(
            $"[Vauchi] DeviceLink QR ready (expires_at={expiresAtUnix})");
    }

    private void OnDeviceLinkConfirmationRequired(DeviceLinkConfirmationArgs args)
    {
        // Remember the verification code so the UI's "Codes Match" button
        // can call ConfirmManual without scraping it back out of the
        // rendered ScreenModel.
        _deviceLinkVerificationCode = args.ConfirmationCode;

        VauchiNative.AppDeviceLinkPeerConnected(_appHandle, args.ConfirmationCode);
        RefreshScreen();
    }

    private void OnDeviceLinkCompleted(string deviceName, uint deviceIndex)
    {
        VauchiNative.AppDeviceLinkSyncComplete(_appHandle);
        RefreshScreen();
    }

    private void OnDeviceLinkFailed(string reason)
    {
        System.Diagnostics.Debug.WriteLine($"[Vauchi] DeviceLink failed: {reason}");
        // UI surfacing of failure reasons is tracked by the device-link
        // ScreenModel — core decides what message to render for each
        // stable reason ("qr_expired", "user_denied", etc.).
        RefreshScreen();
    }

    private void OnDeviceLinkSessionEnded()
    {
        // Always-last callback; safe place to release the bridge handle.
        // CleanupDeviceLink is also called from the UI when the user
        // closes the screen — guard there ensures it's idempotent.
        CleanupDeviceLink();
    }

    /// <summary>
    /// Called when the user confirms the verification code on the
    /// VerifyCode screen. The cycle thread takes it from here.
    /// </summary>
    private Task CompleteDeviceLinkAsync()
    {
        if (_deviceLinkBridge == null || _deviceLinkVerificationCode == null)
            return Task.CompletedTask;

        var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _deviceLinkBridge.ConfirmManual(_deviceLinkVerificationCode, now);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up the device link bridge. Idempotent — safe to call from
    /// both the SessionEnded callback and from explicit cancel.
    /// </summary>
    private void CleanupDeviceLink()
    {
        if (_deviceLinkBridge != null)
        {
            _deviceLinkBridge.QrReady -= OnDeviceLinkQrReady;
            _deviceLinkBridge.ConfirmationRequired -= OnDeviceLinkConfirmationRequired;
            _deviceLinkBridge.Completed -= OnDeviceLinkCompleted;
            _deviceLinkBridge.Failed -= OnDeviceLinkFailed;
            _deviceLinkBridge.SessionEnded -= OnDeviceLinkSessionEnded;
            _deviceLinkBridge.Dispose();
            _deviceLinkBridge = null;
        }
        _deviceLinkVerificationCode = null;
    }
}
