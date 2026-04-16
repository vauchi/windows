// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Vauchi;

/// <summary>
/// Backup export (file save) and import (file open) operations.
/// </summary>
public sealed partial class MainWindow
{
    private async void HandleBackupExportComplete(string resultJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            string hexData = root.GetProperty("BackupExportComplete")
                                 .GetProperty("data")
                                 .GetString() ?? "";

            byte[] raw = Convert.FromHexString(hexData);

            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"vauchi-backup-{DateTime.Now:yyyy-MM-dd}";
            picker.FileTypeChoices.Add("Vauchi Backup", new[] { ".vbk" });

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file == null) return;

            await FileIO.WriteBytesAsync(file, raw);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Vauchi] Backup export failed: {ex.Message}");
        }
    }

    private void HandleBackupImport()
    {
        if (_appHandle == IntPtr.Zero) return;

        Interop.VauchiNative.AppNavigateTo(_appHandle, "backup");
        SyncNavSelection();
        RefreshScreen();
    }
}
