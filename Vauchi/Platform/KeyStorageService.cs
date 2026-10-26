// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Windows.Security.Credentials;

namespace Vauchi.Platform;

/// <summary>
/// Stores and retrieves the database encryption key via Windows PasswordVault.
/// Keys are Base64-encoded because PasswordVault only stores strings.
/// The PasswordVault is encrypted at rest by Windows DPAPI.
/// </summary>
public static class KeyStorageService
{
    private const string Resource = "vauchi";
    internal const string AppEntry = "storage-key";

    /// <summary>
    /// Generate a new 32-byte cryptographic key.
    /// </summary>
    public static byte[] GenerateKey()
    {
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    /// Store a key in PasswordVault. Overwrites any existing key.
    /// </summary>
    public static void StoreKey(byte[] key) => StoreKey(key, AppEntry);

    internal static void StoreKey(byte[] key, string entry)
    {
        var vault = new PasswordVault();
        try
        {
            var existing = vault.Retrieve(Resource, entry);
            vault.Remove(existing);
        }
        catch (Exception) { /* not found — ok */ }

        string base64 = Convert.ToBase64String(key);
        vault.Add(new PasswordCredential(Resource, entry, base64));
    }

    /// <summary>
    /// Retrieve the key from PasswordVault. Returns null if not found.
    /// </summary>
    public static byte[]? RetrieveKey() => RetrieveKey(AppEntry);

    internal static byte[]? RetrieveKey(string entry)
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(Resource, entry);
            credential.RetrievePassword();
            return Convert.FromBase64String(credential.Password);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Delete the key from PasswordVault.
    /// </summary>
    public static void DeleteKey() => DeleteKey(AppEntry);

    internal static void DeleteKey(string entry)
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(Resource, entry);
            vault.Remove(credential);
        }
        catch (Exception) { /* not found — ok */ }
    }
}
