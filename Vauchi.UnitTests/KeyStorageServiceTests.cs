// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Vauchi.Platform;
using Xunit;

namespace Vauchi.Tests;

public class KeyStorageServiceTests
{
    [Fact]
    public void RoundTrip_StoreAndRetrieve_ReturnsSameBytes()
    {
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
        KeyStorageService.DeleteKey();

        byte[]? retrieved = KeyStorageService.RetrieveKey();
        Assert.Null(retrieved);
    }

    [Fact]
    public void DeleteKey_RemovesStoredKey()
    {
        var key = new byte[32];
        new Random(99).NextBytes(key);

        KeyStorageService.StoreKey(key);
        KeyStorageService.DeleteKey();

        byte[]? retrieved = KeyStorageService.RetrieveKey();
        Assert.Null(retrieved);
    }
}
