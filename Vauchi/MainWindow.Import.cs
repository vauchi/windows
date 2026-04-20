// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
using Vauchi.Interop;
using Vauchi.Services;
using Windows.Storage.Pickers;

namespace Vauchi;

/// <summary>
/// Contact import via .vcf file picker (external contact import).
/// </summary>
public sealed partial class MainWindow
{
    private async void HandleContactImport()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".vcf");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null || _appHandle == IntPtr.Zero) return;

            byte[] vcfData;
            using (var stream = await file.OpenReadAsync())
            {
                vcfData = new byte[stream.Size];
                using var reader = new Windows.Storage.Streams.DataReader(stream);
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(vcfData);
            }

            string? resultJson = VauchiNative.AppImportContactsFromVcf(_appHandle, vcfData);
            if (resultJson == null) return;

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var err))
            {
                await ShowImportResultDialog(
                    Localizer.T("import_contacts.result_failed"),
                    err.GetString() ?? Localizer.T("error.unknown")
                );
                return;
            }

            int imported = root.TryGetProperty("imported", out var imp) ? imp.GetInt32() : 0;
            int skipped = root.TryGetProperty("skipped", out var skip) ? skip.GetInt32() : 0;

            var message = new StringBuilder();
            message.Append(Localizer.T(
                "import_contacts.result_imported",
                new Dictionary<string, string> { ["count"] = imported.ToString() }
            ));
            if (skipped > 0)
            {
                message.Append(", ");
                message.Append(Localizer.T(
                    "import_contacts.result_skipped",
                    new Dictionary<string, string> { ["count"] = skipped.ToString() }
                ));
            }

            if (root.TryGetProperty("warnings", out var warnings) &&
                warnings.GetArrayLength() > 0)
            {
                message.Append("\n\n");
                foreach (var w in warnings.EnumerateArray())
                {
                    message.Append('\n');
                    message.Append('\u2022');
                    message.Append(' ');
                    message.Append(w.GetString());
                }
            }

            await ShowImportResultDialog(Localizer.T("import_contacts.result_complete"), message.ToString());

            RefreshScreen();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Vauchi] Import failed: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task ShowImportResultDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = Localizer.T("action.ok"),
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}
