// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Vauchi.Interop;

namespace Vauchi.Handlers;

/// <summary>
/// Device-link orchestration on the Windows side. Thin adapter over
/// <see cref="DeviceLinkBridge"/> — the relay-poll loop, QR-expiry
/// deadline, confirmation prompt timing, and protocol clock all live
/// in core's <c>DeviceLinkSession</c>. This handler subscribes to
/// bridge events and forwards UI transitions through the injected
/// callbacks, plus relays the user's "Codes Match" tap back into the
/// cycle thread via <see cref="ConfirmAsync"/>.
/// </summary>
public sealed class DeviceLinkHandler : IDisposable
{
    private readonly IntPtr _appHandle;
    private readonly DispatcherQueue _dispatcher;
    private readonly Action<string> _navigateToScreen;
    private readonly Action _refreshScreen;
    private DeviceLinkBridge? _bridge;
    private string? _verificationCode;

    /// <param name="appHandle">
    /// CABI app handle. Captured at construction time; the handler
    /// is created after MainWindow has its handle and disposed
    /// before the handle is destroyed, so it never sees
    /// <c>IntPtr.Zero</c>.
    /// </param>
    /// <param name="dispatcher">
    /// UI-thread dispatcher passed through to
    /// <see cref="DeviceLinkBridge.Create"/> for callback marshalling.
    /// </param>
    /// <param name="navigateToScreen">
    /// Fired by <see cref="Start"/> after creating the bridge:
    /// invokes <c>VauchiNative.AppNavigateTo</c> + nav-selection
    /// sync + screen refresh as a unit. Caller decides how to
    /// compose those three steps.
    /// </param>
    /// <param name="refreshScreen">
    /// Bare screen refresh — fired after each device-link event
    /// (peer-connected, sync-complete, failed) so the device-link
    /// ScreenModel re-renders.
    /// </param>
    public DeviceLinkHandler(
        IntPtr appHandle,
        DispatcherQueue dispatcher,
        Action<string> navigateToScreen,
        Action refreshScreen)
    {
        _appHandle = appHandle;
        _dispatcher = dispatcher;
        _navigateToScreen = navigateToScreen;
        _refreshScreen = refreshScreen;
    }

    /// <summary>True iff a device-link cycle thread is currently
    /// active. Callers should gate
    /// <see cref="ConfirmAsync"/> on this to avoid stale taps after
    /// the session has ended.</summary>
    public bool IsActive => _bridge != null;

    /// <summary>
    /// Starts the device-link initiator flow:
    /// <list type="number">
    ///   <item>Creates a session via the CABI bridge (core builds
    ///         the QR + cycle thread).</item>
    ///   <item>Navigates to the device_linking screen so the QR is
    ///         visible.</item>
    ///   <item>Subscribes to bridge events to drive UI transitions.</item>
    ///   <item>Calls <see cref="DeviceLinkBridge.Start"/> to spawn the
    ///         cycle thread.</item>
    /// </list>
    /// </summary>
    public void Start()
    {
        _bridge = DeviceLinkBridge.Create(_appHandle, _dispatcher);
        if (_bridge == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "[Vauchi] DeviceLink: failed to create session (no identity / storage key)");
            return;
        }

        // QR data does not need to flow through here — core renders it via
        // its own ScreenModel for the device_linking screen. We just route
        // the UI to that screen and start the cycle thread.
        _navigateToScreen("device_linking");

        _bridge.QrReady += OnQrReady;
        _bridge.ConfirmationRequired += OnConfirmationRequired;
        _bridge.Completed += OnCompleted;
        _bridge.Failed += OnFailed;
        _bridge.SessionEnded += OnSessionEnded;

        _bridge.Start();
    }

    /// <summary>
    /// Called when the user confirms the verification code on the
    /// VerifyCode screen. The cycle thread takes it from there.
    /// No-op when there is no active session.
    /// </summary>
    public Task ConfirmAsync()
    {
        if (_bridge == null || _verificationCode == null)
            return Task.CompletedTask;

        var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _bridge.ConfirmManual(_verificationCode, now);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up the device-link bridge. Idempotent — safe to call
    /// from both the <see cref="DeviceLinkBridge.SessionEnded"/>
    /// callback and from explicit cancel.
    /// </summary>
    public void Cleanup()
    {
        if (_bridge != null)
        {
            _bridge.QrReady -= OnQrReady;
            _bridge.ConfirmationRequired -= OnConfirmationRequired;
            _bridge.Completed -= OnCompleted;
            _bridge.Failed -= OnFailed;
            _bridge.SessionEnded -= OnSessionEnded;
            _bridge.Dispose();
            _bridge = null;
        }
        _verificationCode = null;
    }

    public void Dispose() => Cleanup();

    private void OnQrReady(string qrData, ulong expiresAtUnix)
    {
        // The QR is already rendered by core's ScreenModel when we navigate
        // to device_linking. The QR-ready callback exists so future UI
        // could (e.g.) show a countdown to expires_at_unix; today we just
        // log it for diagnostics.
        System.Diagnostics.Debug.WriteLine(
            $"[Vauchi] DeviceLink QR ready (expires_at={expiresAtUnix})");
    }

    private void OnConfirmationRequired(DeviceLinkConfirmationArgs args)
    {
        // Remember the verification code so the UI's "Codes Match"
        // button can call ConfirmManual without scraping it back out
        // of the rendered ScreenModel.
        _verificationCode = args.ConfirmationCode;

        VauchiNative.AppDeviceLinkPeerConnected(_appHandle, args.ConfirmationCode);
        _refreshScreen();
    }

    private void OnCompleted(string deviceName, uint deviceIndex)
    {
        VauchiNative.AppDeviceLinkSyncComplete(_appHandle);
        _refreshScreen();
    }

    private void OnFailed(string reason)
    {
        System.Diagnostics.Debug.WriteLine($"[Vauchi] DeviceLink failed: {reason}");
        // UI surfacing of failure reasons is tracked by the device-link
        // ScreenModel — core decides what message to render for each
        // stable reason ("qr_expired", "user_denied", etc.).
        _refreshScreen();
    }

    private void OnSessionEnded()
    {
        // Always-last callback; safe place to release the bridge.
        // Cleanup is also called from MainWindow when the window
        // closes — guard inside ensures it's idempotent.
        Cleanup();
    }
}
