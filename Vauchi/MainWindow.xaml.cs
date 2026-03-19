// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Vauchi.Helpers;
using Vauchi.Interop;
using Vauchi.Platform;

namespace Vauchi;

/// <summary>
/// Main window with sidebar navigation, powered by vauchi-cabi App API.
/// </summary>
public sealed partial class MainWindow : Window
{
    private IntPtr _appHandle;
    private List<string> _screenIds = new();
    private bool _sidebarUpdating;
    private SystemTrayManager? _tray;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Vauchi";

        Renderer.ActionRequested += OnActionRequested;
        InitializeApp();

        _tray = new SystemTrayManager(this);
        _tray.Initialize();

        var shortcuts = new KeyboardShortcuts();
        shortcuts.NavigateRequested += screenName =>
        {
            if (_appHandle == IntPtr.Zero) return;
            VauchiNative.AppNavigateTo(_appHandle, screenName);
            RefreshSidebar();
            RefreshScreen();
        };
        shortcuts.Register(Content as UIElement ?? throw new InvalidOperationException("Content not set"));
    }

    private void InitializeApp()
    {
        string dataDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vauchi");

        byte[]? key = KeyStorageService.RetrieveKey();
        if (key == null)
        {
            key = KeyStorageService.GenerateKey();
            KeyStorageService.StoreKey(key);
        }

        _appHandle = VauchiNative.AppCreateWithKey(dataDir, null, key, key.Length);
        Array.Clear(key);

        if (_appHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to initialize Vauchi storage. The database may be corrupted or inaccessible.");
        }

        string? defaultScreen = VauchiNative.AppDefaultScreen(_appHandle);

        // Check if onboarding is needed (identity not yet created)
        string? startScreen = defaultScreen;
        string? availableJson = VauchiNative.AppAvailableScreens(_appHandle);
        if (availableJson != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(availableJson);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.GetString() == "onboarding") { startScreen = "onboarding"; break; }
                }
            }
            catch (JsonException)
            {
                System.Diagnostics.Debug.WriteLine("[Vauchi] Failed to parse available screens");
            }
        }
        if (startScreen != null)
        {
            VauchiNative.AppNavigateTo(_appHandle, startScreen);
        }

        RefreshSidebar();
        RefreshScreen();
    }

    private void RefreshSidebar()
    {
        _sidebarUpdating = true;

        string? json = VauchiNative.AppAvailableScreens(_appHandle);
        _screenIds.Clear();
        Sidebar.Items.Clear();

        if (json != null)
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                string id = el.GetString() ?? "unknown";
                _screenIds.Add(id);
                Sidebar.Items.Add(FormatScreenName(id));
            }
        }

        _sidebarUpdating = false;
    }

    internal static string FormatScreenName(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        string display = id.Replace('_', ' ');
        return char.ToUpper(display[0]) + display[1..];
    }

    private void OnSidebarSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sidebarUpdating || _appHandle == IntPtr.Zero) return;

        int index = Sidebar.SelectedIndex;
        if (index < 0 || index >= _screenIds.Count) return;

        string screenName = _screenIds[index];
        VauchiNative.AppNavigateTo(_appHandle, screenName);
        RefreshScreen();
    }

    private void OnActionRequested(object? sender, string actionJson)
    {
        if (_appHandle == IntPtr.Zero) return;

        System.Diagnostics.Debug.WriteLine($"[Vauchi] Action: {JsonSanitizer.SafeType(actionJson)}");

        string? resultJson = VauchiNative.AppHandleAction(_appHandle, actionJson);
        System.Diagnostics.Debug.WriteLine($"[Vauchi] Result: {JsonSanitizer.SafeType(resultJson)}");
        if (resultJson == null) return;

        HandleActionResult(resultJson);
    }

    private void HandleActionResult(string resultJson)
    {
        var kind = ActionResultParser.Classify(resultJson);
        System.Diagnostics.Debug.WriteLine($"[Vauchi] ResultKind: {kind}");

        switch (kind)
        {
            case ActionResultKind.UpdateScreen:
            case ActionResultKind.NavigateTo:
            case ActionResultKind.ValidationError:
            case ActionResultKind.Complete:
            case ActionResultKind.WipeComplete:
                RefreshSidebar();
                RefreshScreen();
                break;

            case ActionResultKind.ExchangeCommands:
                var commands = ExchangeCommandParser.ParseFromActionResult(resultJson);
                HandleExchangeCommands(commands);
                RefreshScreen();
                break;

            case ActionResultKind.ShowAlert:
                ShowAlertAsync(resultJson);
                break;

            case ActionResultKind.OpenUrl:
                OpenUrlAsync(resultJson);
                break;

            case ActionResultKind.ShowToast:
                ShowFloatingToast(resultJson);
                break;

            case ActionResultKind.OpenContact:
            case ActionResultKind.EditContact:
            case ActionResultKind.OpenEntryDetail:
            case ActionResultKind.StartDeviceLink:
            case ActionResultKind.StartBackupImport:
                RefreshSidebar();
                RefreshScreen();
                break;

            case ActionResultKind.RequestCamera:
                System.Diagnostics.Debug.WriteLine("[Vauchi] Camera requested — not yet implemented");
                break;

            case ActionResultKind.Error:
                ShowFatalErrorAsync(resultJson);
                break;

            default:
                RefreshScreen();
                break;
        }
    }

    private async void ShowAlertAsync(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        if (!doc.RootElement.TryGetProperty("ShowAlert", out var alert)) return;
        string title = alert.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
        string message = alert.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private async void OpenUrlAsync(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        if (!doc.RootElement.TryGetProperty("OpenUrl", out var urlEl)) return;
        string? url = urlEl.TryGetProperty("url", out var u) ? u.GetString() : null;
        if (url != null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private void ShowFloatingToast(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        if (!doc.RootElement.TryGetProperty("ShowToast", out var toast)) return;
        string message = toast.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        string? undoId = toast.TryGetProperty("undo_action_id", out var uid) ? uid.GetString() : null;

        // Log toast for now — floating InfoBar will be wired in Task 9 (NavigationView)
        System.Diagnostics.Debug.WriteLine($"[Vauchi] Toast: {message}{(undoId != null ? $" (undo: {undoId})" : "")}");
    }

    private async void ShowFatalErrorAsync(string resultJson)
    {
        string errorMsg = "Unknown error";
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            if (doc.RootElement.TryGetProperty("error", out var err))
                errorMsg = err.GetString() ?? errorMsg;
        }
        catch { }

        var dialog = new ContentDialog
        {
            Title = "Vauchi Error",
            Content = $"An unrecoverable error occurred:\n\n{errorMsg}\n\nThe app may need to be restarted.",
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private void HandleExchangeCommands(ExchangeCommand[] commands)
    {
        foreach (var cmd in commands)
        {
            switch (cmd.Kind)
            {
                case ExchangeCommandKind.QrDisplay:
                    break;
                case ExchangeCommandKind.QrRequestScan:
                    ShowQrScanDialog();
                    break;
                default:
                    break;
            }
        }
    }

    private async void ShowQrScanDialog()
    {
        var input = new TextBox
        {
            PlaceholderText = "Paste scanned QR data here...",
            AcceptsReturn = false,
        };

        var dialog = new ContentDialog
        {
            Title = "Scan QR Code",
            Content = input,
            PrimaryButtonText = "Submit",
            CloseButtonText = "Cancel",
            XamlRoot = Content.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            string scanned = input.Text?.Trim() ?? "";
            if (scanned.Length > 0 && _appHandle != IntPtr.Zero)
            {
                string eventJson = ExchangeHardwareEventJson.QrScanned(scanned);
                string? resultJson = VauchiNative.AppHandleHardwareEvent(_appHandle, eventJson);
                if (resultJson != null)
                {
                    HandleActionResult(resultJson);
                }
            }
        }
    }

    private void RefreshScreen()
    {
        string? screenJson = VauchiNative.AppCurrentScreen(_appHandle);
        if (screenJson != null)
        {
            Renderer.RenderFromJson(screenJson);
        }
    }

    private void OnQuitClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnAboutClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "About Vauchi",
            Content = "Vauchi — Privacy-focused updatable contact cards.\n\nVersion 0.5.0\nLicense: GPL-3.0-or-later",
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        Renderer.ActionRequested -= OnActionRequested;
        _tray?.Dispose();

        if (_appHandle != IntPtr.Zero)
        {
            VauchiNative.AppDestroy(_appHandle);
            _appHandle = IntPtr.Zero;
        }
    }
}
