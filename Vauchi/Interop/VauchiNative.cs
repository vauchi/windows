// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Vauchi.Interop;

/// <summary>
/// P/Invoke bindings for vauchi-cabi native library.
/// </summary>
public static partial class VauchiNative
{
    private const string LibName = "vauchi_cabi";

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_create", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr WorkflowCreate(string workflowType);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create_with_relay", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr AppCreateWithRelay(string? relayUrl);

    /// <summary>Default relay URL — matches all other Vauchi frontends.</summary>
    public const string DefaultRelayUrl = "wss://relay.vauchi.app";

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_destroy")]
    public static partial void WorkflowDestroy(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_current_screen")]
    private static partial IntPtr WorkflowCurrentScreenRaw(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_handle_action", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr WorkflowHandleActionRaw(IntPtr handle, string actionJson);

    [LibraryImport(LibName, EntryPoint = "vauchi_string_free")]
    private static partial void StringFree(IntPtr ptr);

    public static string? WorkflowCurrentScreen(IntPtr handle)
    {
        IntPtr ptr = WorkflowCurrentScreenRaw(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? WorkflowHandleAction(IntPtr handle, string actionJson)
    {
        IntPtr ptr = WorkflowHandleActionRaw(handle, actionJson);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static JsonDocument? GetCurrentScreen(IntPtr handle)
    {
        string? json = WorkflowCurrentScreen(handle);
        return json != null ? JsonDocument.Parse(json) : null;
    }

    // ── App API (ADR-030/031) ───────────────────────────────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create_with_config", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr AppCreateWithConfig(string dataDir, string? relayUrl);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create_with_key", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr AppCreateWithKey(
        string dataDir,
        string? relayUrl,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] keyBytes,
        int keyLen);

    // ── Config builder API ─────────────────────────────────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_config_new", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr ConfigNew(string dataDir, string? relayUrl);

    [LibraryImport(LibName, EntryPoint = "vauchi_config_set_storage_key")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ConfigSetStorageKey(
        IntPtr config,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] key,
        nuint keyLen);

    [LibraryImport(LibName, EntryPoint = "vauchi_config_enable_ble")]
    public static partial void ConfigEnableBle(IntPtr config, [MarshalAs(UnmanagedType.U1)] bool enabled);

    [LibraryImport(LibName, EntryPoint = "vauchi_config_enable_audio")]
    public static partial void ConfigEnableAudio(IntPtr config, [MarshalAs(UnmanagedType.U1)] bool enabled);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create_from_config")]
    public static partial IntPtr AppCreateFromConfig(IntPtr config);

    [LibraryImport(LibName, EntryPoint = "vauchi_config_free")]
    public static partial void ConfigFree(IntPtr config);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_destroy")]
    public static partial void AppDestroy(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_current_screen")]
    private static partial IntPtr AppCurrentScreenRaw(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_handle_action", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr AppHandleActionRaw(IntPtr handle, string actionJson);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_handle_hardware_event", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr AppHandleHardwareEventRaw(IntPtr handle, string eventJson);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_navigate_to", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr AppNavigateToRaw(IntPtr handle, string screenName);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_available_screens")]
    private static partial IntPtr AppAvailableScreensRaw(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_default_screen")]
    private static partial IntPtr AppDefaultScreenRaw(IntPtr handle);

    public static string? AppCurrentScreen(IntPtr handle)
    {
        IntPtr ptr = AppCurrentScreenRaw(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? AppHandleAction(IntPtr handle, string actionJson)
    {
        IntPtr ptr = AppHandleActionRaw(handle, actionJson);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? AppHandleHardwareEvent(IntPtr handle, string eventJson)
    {
        IntPtr ptr = AppHandleHardwareEventRaw(handle, eventJson);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? AppNavigateTo(IntPtr handle, string screenName)
    {
        IntPtr ptr = AppNavigateToRaw(handle, screenName);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? AppAvailableScreens(IntPtr handle)
    {
        IntPtr ptr = AppAvailableScreensRaw(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? AppDefaultScreen(IntPtr handle)
    {
        IntPtr ptr = AppDefaultScreenRaw(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    // ── Audio (ultrasonic proximity, behind 'audio' feature) ────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_audio_is_available")]
    public static partial int AudioIsAvailable();

    [LibraryImport(LibName, EntryPoint = "vauchi_audio_emit")]
    public static partial int AudioEmit(
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data, nuint dataLen);

    [LibraryImport(LibName, EntryPoint = "vauchi_audio_listen")]
    private static partial IntPtr AudioListenRaw(ulong timeoutMs);

    public static string? AudioListen(ulong timeoutMs)
    {
        IntPtr ptr = AudioListenRaw(timeoutMs);
        if (ptr == IntPtr.Zero) return null;
        string result = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    [LibraryImport(LibName, EntryPoint = "vauchi_audio_stop")]
    public static partial void AudioStop();
}
