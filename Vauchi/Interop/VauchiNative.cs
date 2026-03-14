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

    /// <summary>Default relay URL — matches all other Vauchi frontends.</summary>
    public const string DefaultRelayUrl = "wss://relay.vauchi.app";

    // ── String free ──────────────────────────────────────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_string_free")]
    private static partial void NativeStringFree(IntPtr ptr);

    // ── Workflow bindings (legacy) ───────────────────────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_create", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr WorkflowCreate(string workflowType);

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_destroy")]
    public static partial void WorkflowDestroy(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_current_screen")]
    private static partial IntPtr WorkflowCurrentScreenRaw(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_workflow_handle_action", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr WorkflowHandleActionRaw(IntPtr handle, string actionJson);

    public static string? WorkflowCurrentScreen(IntPtr handle)
    {
        IntPtr ptr = WorkflowCurrentScreenRaw(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        NativeStringFree(ptr);
        return result;
    }

    public static string? WorkflowHandleAction(IntPtr handle, string actionJson)
    {
        IntPtr ptr = WorkflowHandleActionRaw(handle, actionJson);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        NativeStringFree(ptr);
        return result;
    }

    public static JsonDocument? GetCurrentScreen(IntPtr handle)
    {
        string? json = WorkflowCurrentScreen(handle);
        return json != null ? JsonDocument.Parse(json) : null;
    }

    // ── AppEngine bindings ───────────────────────────────────────

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create")]
    private static partial IntPtr NativeAppCreate();

    [LibraryImport(LibName, EntryPoint = "vauchi_app_create_with_relay", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr NativeAppCreateWithRelay(string relayUrl);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_destroy")]
    private static partial void NativeAppDestroy(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_current_screen")]
    private static partial IntPtr NativeAppCurrentScreen(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_handle_action", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr NativeAppHandleAction(IntPtr handle, string actionJson);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_navigate_to", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr NativeAppNavigateTo(IntPtr handle, string screenName);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_available_screens")]
    private static partial IntPtr NativeAppAvailableScreens(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "vauchi_app_default_screen")]
    private static partial IntPtr NativeAppDefaultScreen(IntPtr handle);

    // ── AppEngine managed wrappers ───────────────────────────────

    /// <summary>
    /// Creates a new AppEngine instance with default configuration.
    /// </summary>
    public static IntPtr AppCreate()
    {
        return NativeAppCreate();
    }

    /// <summary>
    /// Creates a new AppEngine instance with a specific relay URL.
    /// </summary>
    public static IntPtr AppCreateWithRelay(string relayUrl)
    {
        return NativeAppCreateWithRelay(relayUrl);
    }

    /// <summary>
    /// Destroys an AppEngine instance and frees its resources.
    /// </summary>
    public static void AppDestroy(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            NativeAppDestroy(handle);
        }
    }

    /// <summary>
    /// Returns the current screen JSON from the AppEngine.
    /// </summary>
    public static string? AppCurrentScreen(IntPtr handle)
    {
        IntPtr ptr = NativeAppCurrentScreen(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        NativeStringFree(ptr);
        return result;
    }

    /// <summary>
    /// Sends an action JSON string to the AppEngine and returns the result.
    /// </summary>
    public static string? AppHandleAction(IntPtr handle, string actionJson)
    {
        IntPtr ptr = NativeAppHandleAction(handle, actionJson);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        NativeStringFree(ptr);
        return result;
    }

    /// <summary>
    /// Navigates the AppEngine to a named screen and returns the result.
    /// </summary>
    public static string? AppNavigateTo(IntPtr handle, string screenName)
    {
        IntPtr ptr = NativeAppNavigateTo(handle, screenName);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        NativeStringFree(ptr);
        return result;
    }

    /// <summary>
    /// Returns JSON array of available screen names.
    /// </summary>
    public static string? AppAvailableScreens(IntPtr handle)
    {
        IntPtr ptr = NativeAppAvailableScreens(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        NativeStringFree(ptr);
        return result;
    }

    /// <summary>
    /// Returns the default screen name.
    /// </summary>
    public static string? AppDefaultScreen(IntPtr handle)
    {
        IntPtr ptr = NativeAppDefaultScreen(handle);
        if (ptr == IntPtr.Zero) return null;
        string result = Marshal.PtrToStringUTF8(ptr)!;
        NativeStringFree(ptr);
        return result;
    }
}
