// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Vauchi.Interop;

namespace Vauchi;

/// <summary>
/// C#-side wrapper around the CABI device-link orchestrator session.
/// Translates the static-trampoline callbacks (each one fired from
/// the core <c>vauchi-device-link-cycle</c> thread) into instance
/// events marshalled onto the configured <see cref="DispatcherQueue"/>
/// so subscribers can mutate UI state safely.
/// </summary>
/// <remarks>
/// <para>Lifecycle: <see cref="Create"/> on the UI thread,
/// register listener via the constructor, optionally subscribe to
/// events, then <see cref="Start"/>. After a terminal callback
/// (<see cref="Completed"/> or <see cref="Failed"/>, always followed
/// by <see cref="SessionEnded"/>) call <see cref="Dispose"/>.</para>
///
/// <para>The bridge owns a <see cref="GCHandle"/> on itself for the
/// duration of its lifetime so the CABI side can pass it back through
/// <c>user_data</c>. The static <see cref="UnmanagedCallersOnlyAttribute"/>
/// trampolines resolve the bridge instance from that handle.</para>
/// </remarks>
public sealed class DeviceLinkBridge : IDisposable
{
    private readonly DispatcherQueue _ui;
    private IntPtr _session;
    private GCHandle _selfHandle;
    private bool _disposed;

    public event Action<string, ulong>? QrReady;
    public event Action<DeviceLinkConfirmationArgs>? ConfirmationRequired;
    public event Action<string>? RequestSent;
    public event Action<string, uint>? Completed;
    public event Action<string>? Failed;
    public event Action? SessionEnded;

    private DeviceLinkBridge(IntPtr session, DispatcherQueue ui)
    {
        _session = session;
        _ui = ui;
        _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
    }

    /// <summary>
    /// Create a session against the supplied app handle and return a
    /// bridge ready to receive listener callbacks. Returns
    /// <see langword="null"/> if the CABI factory rejected the handle
    /// (no identity, missing storage key, or panic).
    /// </summary>
    public static DeviceLinkBridge? Create(IntPtr appHandle, DispatcherQueue ui)
    {
        IntPtr session = VauchiNative.DeviceLinkSessionCreate(appHandle);
        if (session == IntPtr.Zero) return null;

        var bridge = new DeviceLinkBridge(session, ui);
        bridge.RegisterListener();
        return bridge;
    }

    private void RegisterListener()
    {
        unsafe
        {
            var listener = new VauchiNative.VauchiDeviceLinkListener
            {
                OnQrReady = (IntPtr)(delegate* unmanaged<IntPtr, ulong, IntPtr, void>)&OnQrReadyTrampoline,
                OnConfirmationRequired = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, nuint, IntPtr, void>)&OnConfirmationRequiredTrampoline,
                OnRequestSent = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&OnRequestSentTrampoline,
                OnCompleted = (IntPtr)(delegate* unmanaged<IntPtr, uint, IntPtr, void>)&OnCompletedTrampoline,
                OnFailed = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&OnFailedTrampoline,
                OnSessionEnded = (IntPtr)(delegate* unmanaged<IntPtr, void>)&OnSessionEndedTrampoline,
                UserData = GCHandle.ToIntPtr(_selfHandle),
            };
            VauchiNative.DeviceLinkSessionSetListener(_session, listener);
        }
    }

    /// <summary>Spawn the cycle thread. Idempotent on the CABI side.</summary>
    public void Start()
    {
        if (_session != IntPtr.Zero)
        {
            VauchiNative.DeviceLinkSessionStart(_session);
        }
    }

    /// <summary>User confirmed the codes match. Returns 0 on success.</summary>
    public int ConfirmManual(string confirmationCode, ulong confirmedAt)
    {
        if (_session == IntPtr.Zero) return -1;
        return VauchiNative.DeviceLinkSessionConfirmManual(_session, confirmationCode, confirmedAt);
    }

    /// <summary>Submit ultrasonic challenge response (16 bytes).
    /// Returns 0 on success, -2 on length validation failure.</summary>
    public int ConfirmUltrasonic(byte[] challengeResponse, ulong verifiedAt)
    {
        if (_session == IntPtr.Zero) return -1;
        return VauchiNative.DeviceLinkSessionConfirmUltrasonic(
            _session, challengeResponse, (nuint)challengeResponse.Length, verifiedAt);
    }

    /// <summary>User denied the link.</summary>
    public void Deny()
    {
        if (_session != IntPtr.Zero)
        {
            VauchiNative.DeviceLinkSessionDeny(_session);
        }
    }

    /// <summary>Cancel and join the cycle thread. Idempotent.</summary>
    public void Cancel()
    {
        if (_session != IntPtr.Zero)
        {
            VauchiNative.DeviceLinkSessionCancel(_session);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Cancel + destroy the CABI session first so the cycle thread
        // joins before we free the GCHandle. Once the session is gone
        // no further callbacks can fire — only then is it safe to
        // release the user_data slot.
        if (_session != IntPtr.Zero)
        {
            VauchiNative.DeviceLinkSessionDestroy(_session);
            _session = IntPtr.Zero;
        }
        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    // ── Static trampolines (UnmanagedCallersOnly) ─────────────────────────
    //
    // .NET 5+ requires every native callback to be a static method with a
    // stable address. We resolve the DeviceLinkBridge instance from the
    // user_data GCHandle, then post the dispatcher-bound event invocation.

    private static DeviceLinkBridge? FromUserData(IntPtr userData)
    {
        if (userData == IntPtr.Zero) return null;
        var handle = GCHandle.FromIntPtr(userData);
        return handle.IsAllocated ? handle.Target as DeviceLinkBridge : null;
    }

    [UnmanagedCallersOnly]
    private static void OnQrReadyTrampoline(IntPtr qrData, ulong expiresAtUnix, IntPtr userData)
    {
        var bridge = FromUserData(userData);
        if (bridge == null) return;
        string qr = Marshal.PtrToStringUTF8(qrData) ?? string.Empty;
        bridge._ui.TryEnqueue(() => bridge.QrReady?.Invoke(qr, expiresAtUnix));
    }

    [UnmanagedCallersOnly]
    private static void OnConfirmationRequiredTrampoline(
        IntPtr deviceName,
        IntPtr confirmationCode,
        IntPtr identityFingerprint,
        IntPtr proximityChallenge,
        nuint proximityChallengeLen,
        IntPtr userData)
    {
        var bridge = FromUserData(userData);
        if (bridge == null) return;

        string name = Marshal.PtrToStringUTF8(deviceName) ?? string.Empty;
        string code = Marshal.PtrToStringUTF8(confirmationCode) ?? string.Empty;
        string fp = Marshal.PtrToStringUTF8(identityFingerprint) ?? string.Empty;

        // Copy the proximity challenge bytes — the buffer is only valid
        // for the duration of the trampoline call (CABI documents this).
        byte[] challenge = new byte[(int)proximityChallengeLen];
        if (proximityChallengeLen > 0)
        {
            Marshal.Copy(proximityChallenge, challenge, 0, (int)proximityChallengeLen);
        }

        var args = new DeviceLinkConfirmationArgs(name, code, fp, challenge);
        bridge._ui.TryEnqueue(() => bridge.ConfirmationRequired?.Invoke(args));
    }

    [UnmanagedCallersOnly]
    private static void OnRequestSentTrampoline(IntPtr confirmationCode, IntPtr userData)
    {
        var bridge = FromUserData(userData);
        if (bridge == null) return;
        string code = Marshal.PtrToStringUTF8(confirmationCode) ?? string.Empty;
        bridge._ui.TryEnqueue(() => bridge.RequestSent?.Invoke(code));
    }

    [UnmanagedCallersOnly]
    private static void OnCompletedTrampoline(IntPtr deviceName, uint deviceIndex, IntPtr userData)
    {
        var bridge = FromUserData(userData);
        if (bridge == null) return;
        string name = Marshal.PtrToStringUTF8(deviceName) ?? string.Empty;
        bridge._ui.TryEnqueue(() => bridge.Completed?.Invoke(name, deviceIndex));
    }

    [UnmanagedCallersOnly]
    private static void OnFailedTrampoline(IntPtr reason, IntPtr userData)
    {
        var bridge = FromUserData(userData);
        if (bridge == null) return;
        string r = Marshal.PtrToStringUTF8(reason) ?? string.Empty;
        bridge._ui.TryEnqueue(() => bridge.Failed?.Invoke(r));
    }

    [UnmanagedCallersOnly]
    private static void OnSessionEndedTrampoline(IntPtr userData)
    {
        var bridge = FromUserData(userData);
        if (bridge == null) return;
        bridge._ui.TryEnqueue(() => bridge.SessionEnded?.Invoke());
    }
}

/// <summary>
/// Snapshot of the values delivered by the CABI
/// <c>on_confirmation_required</c> callback. The
/// <see cref="ProximityChallenge"/> bytes are a copy — the original
/// CABI buffer is only valid during the trampoline invocation.
/// </summary>
public sealed record DeviceLinkConfirmationArgs(
    string DeviceName,
    string ConfirmationCode,
    string IdentityFingerprint,
    byte[] ProximityChallenge);
