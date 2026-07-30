// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.IO;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Helpers;
using Vauchi.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Vauchi;

public sealed partial class MainWindow
{
    private void ExecuteNativeEffect(JsonElement command)
    {
        switch (PresentationState.CommandName(command))
        {
            case "ScheduleWakeup":
                ArmWakeupTimer(command.GetProperty("ScheduleWakeup"));
                return;
            case "ExportFile":
                ExportFile(command.GetProperty("ExportFile").GetProperty("file"));
                return;
            case "PostNotification":
                PostNotification(
                    command.GetProperty("PostNotification").GetProperty("notification"));
                return;
            case "ResetApplication":
                return;
        }

        ExchangeCommand effect = ExchangeCommandParser.Parse(command);
        if (effect.Kind != ExchangeCommandKind.Unknown)
        {
            _exchange?.Handle(new[] { effect });
            return;
        }
        System.Diagnostics.Debug.WriteLine(
            $"[Vauchi] Unsupported Core effect: {PresentationState.CommandName(command)}");
    }

    private void RunWakeupTick()
    {
        if (_appHandle == IntPtr.Zero)
            return;
        try
        {
            string? envelope = VauchiNative.AppOnWakeup(_appHandle);
            if (string.IsNullOrEmpty(envelope))
                return;
            Presentation.ApplyEnvelope(envelope);

            using JsonDocument document = JsonDocument.Parse(envelope);
            if (document.RootElement.TryGetProperty(
                    "notifications",
                    out JsonElement notifications)
                && notifications.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement notification in notifications.EnumerateArray())
                    PostNotification(notification);
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] Wakeup failed: {exception.Message}");
        }
    }

    private void ArmWakeupTimer(JsonElement schedule)
    {
        int earliest = schedule.TryGetProperty(
            "earliest_secs",
            out JsonElement earliestValue)
            && earliestValue.TryGetInt32(out int parsedEarliest)
                ? parsedEarliest
                : 0;
        int minimum = schedule.TryGetProperty(
            "min_interval_secs",
            out JsonElement minimumValue)
            && minimumValue.TryGetInt32(out int parsedMinimum)
                ? parsedMinimum
                : 0;
        int seconds = Math.Max(earliest, minimum);
        if (seconds <= 0)
            return;

        _wakeupTimer?.Stop();
        _wakeupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(seconds),
        };
        _wakeupTimer.Tick += (_, _) => RunWakeupTick();
        _wakeupTimer.Start();
    }

    private async void ExportFile(JsonElement file)
    {
        byte[]? bytes = file.TryGetProperty("data", out JsonElement data)
            ? PresentationJson.Bytes(data)
            : null;
        if (bytes is null)
            return;

        string suggestedName =
            file.GetProperty("suggested_name").GetString() ?? "export.bin";
        string extension = Path.GetExtension(suggestedName);
        if (extension.Length == 0)
            extension = ".bin";
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedName),
        };
        picker.FileTypeChoices.Add(
            file.GetProperty("mime_type").GetString() ?? "File",
            new[] { extension });
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        StorageFile? destination = await picker.PickSaveFileAsync();
        if (destination is not null)
            await FileIO.WriteBytesAsync(destination, bytes);
    }

    private static void PostNotification(JsonElement notification)
    {
        string title = notification.TryGetProperty("title", out JsonElement titleValue)
            ? titleValue.GetString() ?? "Vauchi"
            : "Vauchi";
        string body = notification.TryGetProperty("body", out JsonElement bodyValue)
            ? bodyValue.GetString() ?? ""
            : "";
        var native = new AppNotificationBuilder()
            .AddText(title)
            .AddText(body)
            .BuildNotification();
        AppNotificationManager.Default.Show(native);
    }

    private void DrainAndShowNotifications()
    {
        if (_appHandle == IntPtr.Zero)
            return;
        string? json = VauchiNative.AppDrainNotifications(_appHandle);
        if (string.IsNullOrEmpty(json) || json == "[]")
            return;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            foreach (JsonElement notification in document.RootElement.EnumerateArray())
                PostNotification(notification);
        }
        catch (JsonException exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] Notification parse failed: {exception.Message}");
        }
    }
}
