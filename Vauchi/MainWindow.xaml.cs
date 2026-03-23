// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;
using Vauchi.Interop;
using Vauchi.Platform;

namespace Vauchi;

public sealed partial class MainWindow : Window
{
    private IntPtr _appHandle;
    private bool _navUpdating;
    private SystemTrayManager? _tray;
    private DispatcherTimer? _toastTimer;
    private BleExchangeService? _ble;

    // 5-tab navigation model (frontend abstraction, matches TUI/macOS)
    private static readonly (string screenId, string label, Symbol icon)[] NavTabs =
    [
        ("my_info",  "My Card",  Symbol.ContactInfo),
        ("contacts", "Contacts", Symbol.People),
        ("exchange", "Exchange", Symbol.Send),
        ("groups",   "Groups",   Symbol.People),
        ("settings", "More",     Symbol.More),  // "More" defaults to settings screen
    ];

    public MainWindow()
    {
        InitializeComponent();
        Title = "Vauchi";

        Renderer.ActionRequested += OnActionRequested;

        // Async init with optional Windows Hello gate
        _ = InitializeAsync();
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        // Windows Hello gate (if enabled)
        if (SecureStorageService.IsHelloEnabled)
        {
            bool authenticated = await SecureStorageService.AuthenticateAsync();
            if (!authenticated)
            {
                // Show locked state — user can retry
                var dialog = new ContentDialog
                {
                    Title = "Authentication Required",
                    Content = "Windows Hello authentication is required to access Vauchi.",
                    PrimaryButtonText = "Retry",
                    CloseButtonText = "Quit",
                    XamlRoot = Content.XamlRoot,
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await InitializeAsync(); // Retry
                    return;
                }
                else
                {
                    Application.Current.Exit();
                    return;
                }
            }
        }

        InitializeApp();
        RestoreWindowState();

        _tray = new SystemTrayManager(this, screenName =>
        {
            if (_appHandle == IntPtr.Zero) return;
            VauchiNative.AppNavigateTo(_appHandle, screenName);
            SyncNavSelection();
            RefreshScreen();
        });
        _tray.Initialize();

        var shortcuts = new KeyboardShortcuts();
        shortcuts.NavigateRequested += screenName =>
        {
            if (_appHandle == IntPtr.Zero) return;
            VauchiNative.AppNavigateTo(_appHandle, screenName);
            SyncNavSelection();
            RefreshScreen();
        };
        shortcuts.BackRequested += () =>
        {
            if (_appHandle == IntPtr.Zero) return;
            string? resultJson = VauchiNative.AppHandleAction(_appHandle, ActionJson.ActionPressed("back"));
            if (resultJson != null) HandleActionResult(resultJson);
        };
        shortcuts.SearchFocusRequested += () =>
        {
            // TODO: focus search field when search UI is implemented
        };
        shortcuts.PrimaryActionRequested += () =>
        {
            if (_appHandle == IntPtr.Zero) return;
            string? resultJson = VauchiNative.AppHandleAction(_appHandle, ActionJson.ActionPressed("primary"));
            if (resultJson != null) HandleActionResult(resultJson);
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

        // Check if onboarding needed
        bool isOnboarding = false;
        string? availableJson = VauchiNative.AppAvailableScreens(_appHandle);
        if (availableJson != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(availableJson);
                var arr = doc.RootElement;
                if (arr.GetArrayLength() == 1 && arr[0].GetString() == "onboarding")
                    isOnboarding = true;
            }
            catch (JsonException) { }
        }

        if (isOnboarding)
        {
            EnterOnboardingMode();
            VauchiNative.AppNavigateTo(_appHandle, "onboarding");
        }
        else
        {
            ExitOnboardingMode();
            string? defaultScreen = VauchiNative.AppDefaultScreen(_appHandle);
            if (defaultScreen != null)
                VauchiNative.AppNavigateTo(_appHandle, defaultScreen);
        }

        RefreshScreen();

        _ble = new BleExchangeService(OnBleHardwareEvent);
        _ = _ble.CheckAvailabilityAsync();
    }

    private void EnterOnboardingMode()
    {
        NavView.IsPaneVisible = false;
        NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
        NavView.MenuItems.Clear();
    }

    private void ExitOnboardingMode()
    {
        NavView.IsPaneVisible = true;
        NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Auto;
        BuildNavTabs();
    }

    private void BuildNavTabs()
    {
        _navUpdating = true;
        NavView.MenuItems.Clear();

        foreach (var (screenId, label, icon) in NavTabs)
        {
            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = label,
                Icon = new SymbolIcon(icon),
                Tag = screenId,
            });
        }

        _navUpdating = false;
        SyncNavSelection();
    }

    /// <summary>
    /// After any screen change, highlight the correct tab based on current screen_id.
    /// </summary>
    private void SyncNavSelection()
    {
        _navUpdating = true;

        string? screenJson = VauchiNative.AppCurrentScreen(_appHandle);
        string screenId = "";
        if (screenJson != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(screenJson);
                screenId = doc.RootElement.TryGetProperty("screen_id", out var sid)
                    ? sid.GetString() ?? "" : "";
            }
            catch (JsonException) { }
        }

        int tabIndex = MapScreenToTab(screenId);
        if (tabIndex >= 0 && tabIndex < NavView.MenuItems.Count)
            NavView.SelectedItem = NavView.MenuItems[tabIndex];

        _navUpdating = false;
    }

    /// <summary>
    /// Map screen_id to tab index (0-4). Matches TUI nav_index() mapping.
    /// </summary>
    private static int MapScreenToTab(string screenId) => screenId switch
    {
        "my_info" or "entry_detail" => 0,
        "contacts" or "contact_detail" or "contact_edit" or "contact_visibility"
            or "contact_duplicates" or "contact_merge" or "contact_limit" => 1,
        "exchange" => 2,
        "groups" or "group_detail" => 3,
        _ => 4, // settings, help, backup, recovery, sync, etc. → More
    };

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_navUpdating || _appHandle == IntPtr.Zero) return;
        if (args.SelectedItem is not NavigationViewItem item) return;

        string screenId = item.Tag as string ?? "";
        VauchiNative.AppNavigateTo(_appHandle, screenId);
        RefreshScreen();
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (_appHandle == IntPtr.Zero) return;
        string? resultJson = VauchiNative.AppHandleAction(_appHandle, ActionJson.ActionPressed("back"));
        if (resultJson != null) HandleActionResult(resultJson);
    }

    private void OnActionRequested(object? sender, string actionJson)
    {
        if (_appHandle == IntPtr.Zero) return;

        System.Diagnostics.Debug.WriteLine($"[Vauchi] Action: {JsonSanitizer.SafeType(actionJson)}");

        // ADR-031: Hardware events (QR scanned, BLE data, audio response) go through
        // a separate API that routes to engine.handle_hardware_event().
        string? resultJson = ActionRouter.IsHardwareEvent(actionJson)
            ? VauchiNative.AppHandleHardwareEvent(_appHandle, actionJson)
            : VauchiNative.AppHandleAction(_appHandle, actionJson);

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
                SyncNavSelection();
                RefreshScreen();
                break;

            case ActionResultKind.Complete:
            case ActionResultKind.WipeComplete:
                // Onboarding complete or wipe — rebuild nav, refresh
                ExitOnboardingMode();
                string? defaultScreen = VauchiNative.AppDefaultScreen(_appHandle);
                if (defaultScreen != null)
                    VauchiNative.AppNavigateTo(_appHandle, defaultScreen);
                SyncNavSelection();
                RefreshScreen();
                break;

            case ActionResultKind.OpenContact:
            case ActionResultKind.EditContact:
            case ActionResultKind.OpenEntryDetail:
            case ActionResultKind.StartDeviceLink:
            case ActionResultKind.StartBackupImport:
                SyncNavSelection();
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

            case ActionResultKind.RequestCamera:
                // Legacy path: core requested camera without an active ExchangeSession.
                // Navigate to exchange screen — the screen model will include a QrCode
                // component in Scan mode, and QrCodeComponent handles camera internally.
                VauchiNative.AppNavigateTo(_appHandle, "exchange");
                SyncNavSelection();
                RefreshScreen();
                break;

            case ActionResultKind.Error:
                ShowFatalErrorAsync(resultJson);
                break;

            default:
                RefreshScreen();
                break;
        }
    }

    // Exchange command dispatch (BLE, audio, NFC, QR) is in MainWindow.Exchange.cs

    private void RefreshScreen()
    {
        string? screenJson = VauchiNative.AppCurrentScreen(_appHandle);
        if (screenJson != null)
        {
            Renderer.RenderFromJson(screenJson);
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
        _toastTimer?.Stop();

        using var doc = JsonDocument.Parse(resultJson);
        if (!doc.RootElement.TryGetProperty("ShowToast", out var toast)) return;

        string message = toast.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        FloatingToast.Message = message;
        FloatingToast.IsOpen = true;

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer?.Stop();
            FloatingToast.IsOpen = false;
        };
        _toastTimer.Start();
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

    private void SaveWindowState()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var appWindow = this.AppWindow;
            if (appWindow != null)
            {
                settings.Values["WindowWidth"] = appWindow.Size.Width;
                settings.Values["WindowHeight"] = appWindow.Size.Height;
                settings.Values["WindowX"] = appWindow.Position.X;
                settings.Values["WindowY"] = appWindow.Position.Y;
            }
        }
        catch { }
    }

    private void RestoreWindowState()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (settings.Values["WindowWidth"] is int w && settings.Values["WindowHeight"] is int h
                && w >= 720 && h >= 480)
            {
                AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
            }
            if (settings.Values["WindowX"] is int x && settings.Values["WindowY"] is int y
                && x >= 0 && y >= 0)
            {
                AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }
        }
        catch { }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        SaveWindowState();
        Renderer.ActionRequested -= OnActionRequested;
        _tray?.Dispose();
        _toastTimer?.Stop();

        if (_appHandle != IntPtr.Zero)
        {
            _ble?.Dispose();
            VauchiNative.AppDestroy(_appHandle);
            _appHandle = IntPtr.Zero;
        }
    }
}
