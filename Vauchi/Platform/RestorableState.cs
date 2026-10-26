// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO;
using System.Text;

namespace Vauchi.Platform;

/// <summary>
/// Core's opaque restorable state, kept in the data directory between
/// starts (vauchi/private#814). The shell never reads the value; Core
/// validates whatever comes back.
/// </summary>
public static class RestorableState
{
    private const string FileName = "restorable_state";

    // Core caps the value far below this; the bound only stops a damaged
    // or planted file from being read whole.
    private const int MaxBytes = 4096;

    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static void Keep(string dataDirectory, string state)
    {
        Directory.CreateDirectory(dataDirectory);
        string kept = Path.Combine(dataDirectory, FileName);
        string partial = kept + ".partial";
        File.WriteAllText(partial, state, StrictUtf8);
        // A move replaces the kept value whole, so a crash mid-write leaves
        // the previous one.
        File.Move(partial, kept, overwrite: true);
    }

    public static string? Kept(string dataDirectory)
    {
        try
        {
            var file = new FileInfo(Path.Combine(dataDirectory, FileName));
            if (!file.Exists || file.Length > MaxBytes)
                return null;
            return StrictUtf8.GetString(File.ReadAllBytes(file.FullName));
        }
        catch (IOException)
        {
            return null;
        }
        catch (System.UnauthorizedAccessException)
        {
            return null;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }
}
