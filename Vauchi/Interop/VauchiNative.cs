// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices;

namespace Vauchi.Interop;

/// <summary>
/// P/Invoke bindings for vauchi-cabi native library.
/// </summary>
public static partial class VauchiNative
{
    private const string LibName = "vauchi_cabi";

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create_with_relay", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr AppCreateWithRelay(string? relayUrl);

    /// <summary>Default relay URL — matches all other Vauchi frontends.</summary>
    public const string DefaultRelayUrl = "wss://relay.vauchi.app";

    [LibraryImport(LibName, EntryPoint = "vauchi_string_free")]
    private static partial void StringFree(IntPtr ptr);

    [LibraryImport(LibName, EntryPoint = "vauchi_presentation_contract_fixture")]
    private static partial IntPtr PresentationContractFixtureRaw();

    /// <summary>Shared JSON contract replayed by every native shell.</summary>
    public static string? PresentationContractFixture()
    {
        IntPtr ptr = PresentationContractFixtureRaw();
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    // i18n (internationalization) — see core/vauchi-cabi/src/i18n.rs

    [LibraryImport(LibName, EntryPoint = "vauchi_i18n_init", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int I18nInit(string resourceDir);

    [LibraryImport(LibName, EntryPoint = "vauchi_i18n_is_initialized")]
    public static partial int I18nIsInitialized();

    [LibraryImport(LibName, EntryPoint = "vauchi_i18n_get_string", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr I18nGetStringRaw(string localeCode, string key);

    [LibraryImport(LibName, EntryPoint = "vauchi_i18n_available_locales")]
    private static partial IntPtr I18nAvailableLocalesRaw();

    /// <summary>
    /// Look up a translated string via the CABI i18n helper.
    /// Returns null when the locale or key is unknown, or the native
    /// lookup failed. Caller code is expected to fall back to the key
    /// itself (see <see cref="Services.Localizer"/>).
    /// </summary>
    public static string? I18nGetString(string localeCode, string key)
    {
        IntPtr ptr = I18nGetStringRaw(localeCode, key);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    /// <summary>
    /// JSON array of BCP-47 locale codes loaded by core.
    /// </summary>
    public static string? I18nAvailableLocales()
    {
        IntPtr ptr = I18nAvailableLocalesRaw();
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
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

    [LibraryImport(LibName, EntryPoint = "vauchi_app_initial_commands")]
    private static partial IntPtr AppInitialCommandsRaw(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_dispatch", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr AppDispatchRaw(IntPtr handle, string eventJson);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_on_wakeup")]
    private static partial IntPtr AppOnWakeupRaw(IntPtr handle);

    /// <summary>
    /// Wake-up tick: run due work and return the next OS notifications plus
    /// any commands emitted (in practice the next <c>Command::ScheduleWakeup</c>).
    /// Returns a JSON-encoded <c>{"notifications": [...], "commands": [...]}</c>
    /// object, or null on error.
    /// </summary>
    public static string? AppOnWakeup(IntPtr handle)
    {
        IntPtr ptr = AppOnWakeupRaw(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? AppInitialCommands(IntPtr handle)
    {
        IntPtr ptr = AppInitialCommandsRaw(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    public static string? AppDispatch(IntPtr handle, string eventJson)
    {
        IntPtr ptr = AppDispatchRaw(handle, eventJson);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        StringFree(ptr);
        return result;
    }

    // ── Identity (testing) ───────────────────────────────────────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_app_has_identity")]
    public static partial int AppHasIdentity(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create_identity", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AppCreateIdentity(IntPtr handle, string? displayName);

    // ── Audio (ultrasonic proximity, behind 'audio' feature) ────────────

    // ── Event callback (Phase 2E) ────────────────────────────────────

    /// <summary>
    /// Callback signature matching VauchiEventCallback in vauchi.h.
    /// Called by core on background threads when screen data changes.
    /// <c>screenIdsJson</c> is a JSON array of opaque screen ids, e.g. <c>["screen_a","screen_b"]</c>.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VauchiEventCallback(IntPtr screenIdsJson, IntPtr userData);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_set_event_callback")]
    public static partial void AppSetEventCallback(IntPtr handle, VauchiEventCallback? callback, IntPtr userData);

    // ── Notification drain ────────────────────────────────────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_app_drain_notifications")]
    private static partial IntPtr AppDrainNotificationsRaw(IntPtr handle);

    public static string? AppDrainNotifications(IntPtr handle)
    {
        IntPtr ptr = AppDrainNotificationsRaw(handle);
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
