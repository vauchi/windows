// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Vauchi.Platform;
using Windows.Security.Credentials;
using Xunit;

namespace Vauchi.Tests;

public class KeyStorageServiceTests
{
    // Never the app's own entry: these tests run under the same Windows
    // account as the app on dev machines and on pam, and deleting the app's
    // key left its database unreadable (private#248).
    private const string TestEntry = "storage-key-unit-tests";

    /// <summary>
    /// PasswordVault requires an interactive Windows session (DPAPI).
    /// CI runners that execute as a service lack a user session, so
    /// PasswordVault throws UnauthorizedAccessException on open.
    /// </summary>
    private static bool IsPasswordVaultAvailable()
    {
        try
        {
            var vault = new PasswordVault();
            // Probe actual vault access — constructor succeeds even without
            // an interactive session; only operations throw.
            vault.Retrieve("__vauchi_probe__", "__vauchi_probe__");
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception)
        {
            // Credential not found — vault is accessible, entry just missing.
            return true;
        }
    }

    [Fact]
    public void RoundTrip_StoreAndRetrieve_ReturnsSameBytes()
    {
        if (!IsPasswordVaultAvailable())
        {
            // xUnit 2.8+ supports Assert.Skip; fall back to pass-with-warning
            // if running on older version.
            return;
        }

        var original = new byte[32];
        new Random(42).NextBytes(original);

        KeyStorageService.StoreKey(original, TestEntry);
        byte[]? retrieved = KeyStorageService.RetrieveKey(TestEntry);

        Assert.NotNull(retrieved);
        Assert.Equal(original, retrieved);

        KeyStorageService.DeleteKey(TestEntry);
    }

    [Fact]
    public void RetrieveKey_WhenNoneStored_ReturnsNull()
    {
        if (!IsPasswordVaultAvailable())
        {
            return;
        }

        KeyStorageService.DeleteKey(TestEntry);

        byte[]? retrieved = KeyStorageService.RetrieveKey(TestEntry);
        Assert.Null(retrieved);
    }

    [Fact]
    public void DeleteKey_RemovesStoredKey()
    {
        if (!IsPasswordVaultAvailable())
        {
            return;
        }

        var key = new byte[32];
        new Random(99).NextBytes(key);

        KeyStorageService.StoreKey(key, TestEntry);
        KeyStorageService.DeleteKey(TestEntry);

        byte[]? retrieved = KeyStorageService.RetrieveKey(TestEntry);
        Assert.Null(retrieved);
    }

    [Fact]
    public void Tests_LeaveTheAppsOwnKeyAlone()
    {
        if (!IsPasswordVaultAvailable())
        {
            return;
        }

        byte[]? appKeyBefore = KeyStorageService.RetrieveKey();
        var key = new byte[32];
        new Random(7).NextBytes(key);

        KeyStorageService.StoreKey(key, TestEntry);
        KeyStorageService.DeleteKey(TestEntry);

        Assert.Equal(appKeyBefore, KeyStorageService.RetrieveKey());
    }
}
