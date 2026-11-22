// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.IO;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Hardware;
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


    /// <summary>
    /// Brings the hardware shell up and tells Core what this machine has.
    /// From that report on Core sends the shell's commands in a batch's
    /// hardware field (ADR-077).
    /// </summary>
    private async System.Threading.Tasks.Task StartHardwareAsync()
    {
        WindowsHardware hardware;
        try
        {
            hardware = await WindowsHardware.StartAsync(
                eventJson => DispatcherQueue.TryEnqueue(() =>
                {
                    if (_appHandle != IntPtr.Zero)
                        Presentation.DispatchPlatformEvent(eventJson);
                }),
                // Queued, not run in place: the batch it came in is still being
                // handed to the display, and an effect may dispatch an event.
                command => DispatcherQueue.TryEnqueue(() =>
                {
                    if (_appHandle != IntPtr.Zero)
                        ExecuteNativeEffect(command);
                }));
        }
        catch (Exception exception)
        {
            // Without a report Core plans no hardware; the type alone is logged (no PII).
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] Hardware shell failed to start: {exception.GetType().Name}");
            return;
        }
        if (_appHandle == IntPtr.Zero || _engine is null)
        {
            // The window closed while the machine was being probed.
            await hardware.DisposeAsync();
            return;
        }
        _hardware = hardware;
        _engine.Hardware = hardware.Bridge;
        Presentation.DispatchPlatformEvent(hardware.Bridge.Capabilities);
    }

    private void StopHardware()
    {
        WindowsHardware? hardware = _hardware;
        _hardware = null;
        if (_engine is not null)
            _engine.Hardware = null;
        if (hardware is null)
            return;
        // Bounded by the shell's own shutdown grace; nothing in it needs this thread.
        hardware.DisposeAsync().AsTask().GetAwaiter().GetResult();
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
            _engine?.Hardware?.Route(envelope);
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
        int earliestSecs = schedule.TryGetProperty(
            "earliest_secs",
            out JsonElement earliestValue)
            && PresentationJson.Int32(earliestValue) is int parsedEarliestSecs
                ? parsedEarliestSecs
                : 0;
        int deadlineSecs = schedule.TryGetProperty(
            "deadline_secs",
            out JsonElement deadlineValue)
            && PresentationJson.Int32(deadlineValue) is int parsedDeadlineSecs
                ? parsedDeadlineSecs
                : 0;
        int? earliestMillis = schedule.TryGetProperty(
            "earliest_millis",
            out JsonElement earliestMillisValue)
            && PresentationJson.Int32(earliestMillisValue) is int parsedEarliestMillis
                ? parsedEarliestMillis
                : null;
        int delayMilliseconds = WakeupTiming.DelayMillisecondsFor(
            earliestSecs,
            deadlineSecs,
            earliestMillis);
        if (delayMilliseconds <= 0)
            return;

        _wakeupTimer?.Stop();
        _wakeupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(delayMilliseconds),
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
