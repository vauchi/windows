// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Vauchi.Platform;

/// <summary>
/// Manages app-level lock gate using Windows Hello.
/// When enabled, the user must authenticate via Windows Hello
/// (PIN, fingerprint, or face) before the app window is shown.
/// </summary>
public static class SecureStorageService
{
    /// <summary>
    /// Checks whether Windows Hello is available on this device.
    /// </summary>
    public static async Task<bool> IsHelloAvailableAsync()
    {
        var result = await KeyCredentialManager.IsSupportedAsync();
        return result;
    }

    /// <summary>
    /// Prompts the user to authenticate via Windows Hello.
    /// Returns true if authentication succeeded, or if Hello is unavailable
    /// (no gate when hardware doesn't support it).
    /// </summary>
    public static async Task<bool> AuthenticateAsync()
    {
        if (!await IsHelloAvailableAsync())
            return true; // No Hello = no gate, allow access

        var result = await KeyCredentialManager.RequestCreateAsync(
            "vauchi",
            KeyCredentialCreationOption.ReplaceExisting);

        return result.Status == KeyCredentialStatus.Success;
    }

    /// <summary>
    /// Gets or sets whether the Windows Hello lock gate is enabled.
    /// Stored in LocalSettings (survives app updates).
    /// </summary>
    public static bool IsHelloEnabled
    {
        get => ApplicationData.Current.LocalSettings
            .Values["HelloEnabled"] is bool b && b;
        set => ApplicationData.Current.LocalSettings
            .Values["HelloEnabled"] = value;
    }
}
