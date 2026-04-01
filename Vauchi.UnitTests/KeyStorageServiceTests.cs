// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Vauchi.Platform;
using Windows.Security.Credentials;
using Xunit;

namespace Vauchi.Tests;

public class KeyStorageServiceTests
{
    /// <summary>
    /// PasswordVault requires an interactive Windows session (DPAPI).
    /// CI runners that execute as a service lack a user session, so
    /// PasswordVault throws UnauthorizedAccessException on open.
    /// </summary>
    private static bool IsPasswordVaultAvailable()
    {
        try
        {
            _ = new PasswordVault();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
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

        KeyStorageService.StoreKey(original);
        byte[]? retrieved = KeyStorageService.RetrieveKey();

        Assert.NotNull(retrieved);
        Assert.Equal(original, retrieved);

        KeyStorageService.DeleteKey();
    }

    [Fact]
    public void RetrieveKey_WhenNoneStored_ReturnsNull()
    {
        if (!IsPasswordVaultAvailable())
        {
            return;
        }

        KeyStorageService.DeleteKey();

        byte[]? retrieved = KeyStorageService.RetrieveKey();
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

        KeyStorageService.StoreKey(key);
        KeyStorageService.DeleteKey();

        byte[]? retrieved = KeyStorageService.RetrieveKey();
        Assert.Null(retrieved);
    }
}
