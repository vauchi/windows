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
}
