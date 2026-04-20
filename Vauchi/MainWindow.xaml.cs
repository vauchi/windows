// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Vauchi.Helpers;
using Vauchi.Interop;
using Vauchi.Platform;
using Vauchi.Services;

namespace Vauchi;

public sealed partial class MainWindow : Window
{
    private IntPtr _appHandle;
    private bool _navUpdating;
    private SystemTrayManager? _tray;
    private DispatcherTimer? _toastTimer;
    private BleExchangeService? _ble;
    // Prevent GC collection of the event callback delegate (P/Invoke requirement)
    private VauchiNative.VauchiEventCallback? _eventCallback;

    // 5-tab navigation model (frontend abstraction, matches TUI/macOS).
    // Labels resolve through Localizer — each entry stores the i18n key and
    // the visible text is fetched at build-menu time so switching locales
    // doesn't require rebuilding the table.
    private static readonly (string screenId, string labelKey, Symbol icon)[] NavTabs =
    [
        ("my_info",  "nav.myCard",   Symbol.ContactInfo),
        ("contacts", "nav.contacts", Symbol.People),
        ("exchange", "nav.exchange", Symbol.Send),
        ("groups",   "nav.groups",   Symbol.People),
        ("settings", "nav.more",     Symbol.More),  // "More" defaults to settings screen
    ];

    private DispatcherTimer _notificationTimer;

    public MainWindow()
    {
        InitializeComponent();
        Title = Localizer.T("app.name");

        Renderer.ActionRequested += OnActionRequested;

        // Async init with optional Windows Hello gate
        _ = InitializeAsync();
        Activated += OnActivated;

        // Setup notification polling timer (E)
        _notificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _notificationTimer.Tick += (s, e) => PollNotifications();
        _notificationTimer.Start();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            // Trigger auto-lock if enabled when app loses focus (C1)
            if (_appHandle != IntPtr.Zero)
            {
                string? resultJson = VauchiNative.AppHandleAppBackgrounded(_appHandle);
                if (resultJson != null) HandleActionResult(resultJson);
            }
        }
        else
        {
            // Poll for notifications when app is activated (E)
            PollNotifications();
        }
    }

    private void PollNotifications()
    {
        if (_appHandle == IntPtr.Zero) return;

        try
        {
            string? json = VauchiNative.AppPollNotifications(_appHandle);
            if (string.IsNullOrEmpty(json)) return;

            using JsonDocument doc = JsonDocument.Parse(json);
            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                string title = element.GetProperty("title").GetString() ?? "Vauchi";
                string body = element.GetProperty("body").GetString() ?? "";

                var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(body)
                    .BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PollNotifications error: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        var diagPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vauchi", "diag.log");
        try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(diagPath)!); } catch { }
        try { System.IO.File.AppendAllText(diagPath, $"{DateTime.Now}: InitializeAsync entered\n"); } catch { }

        try {
        // Windows Hello gate (if enabled)
        if (SecureStorageService.IsHelloEnabled)
        {
            bool authenticated = await SecureStorageService.AuthenticateAsync();
            if (!authenticated)
            {
                // Show locked state — user can retry
                var dialog = new ContentDialog
                {
                    Title = Localizer.T("auth.required_title"),
                    Content = Localizer.T("auth.windows_hello_required"),
                    PrimaryButtonText = Localizer.T("action.retry"),
                    CloseButtonText = Localizer.T("action.quit"),
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

        var diagLog = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vauchi", "diag.log");
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(diagLog)!);
            System.IO.File.AppendAllText(diagLog, $"{DateTime.Now}: InitializeApp starting\n");
            InitializeApp();
            System.IO.File.AppendAllText(diagLog, $"{DateTime.Now}: InitializeApp OK, handle={_appHandle}\n");
            string? screenJson = VauchiNative.AppCurrentScreen(_appHandle);
            System.IO.File.AppendAllText(diagLog, $"{DateTime.Now}: screen={screenJson?.Substring(0, Math.Min(200, screenJson?.Length ?? 0))}\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(diagLog, $"{DateTime.Now}: FATAL: {ex}\n");
        }
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

        } catch (Exception ex) {
            try { System.IO.File.AppendAllText(diagPath, $"{DateTime.Now}: FATAL: {ex}\n"); } catch { }
        }
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

        var config = VauchiNative.ConfigNew(dataDir, null);
        VauchiNative.ConfigSetStorageKey(config, key, (nuint)key.Length);
        Array.Clear(key);
        VauchiNative.ConfigEnableBle(config, true);
        VauchiNative.ConfigEnableAudio(config, true);
        _appHandle = VauchiNative.AppCreateFromConfig(config);

        if (_appHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to initialize Vauchi storage. The database may be corrupted or inaccessible.");
        }

#if DEBUG
        // --reset-for-testing: create a test identity so the app skips onboarding.
        if (Array.Exists(Environment.GetCommandLineArgs(), a => a == "--reset-for-testing"))
        {
            if (VauchiNative.AppHasIdentity(_appHandle) != 1)
            {
                int rc = VauchiNative.AppCreateIdentity(_appHandle, "Test User");
                if (rc != 0)
                    System.Diagnostics.Debug.WriteLine("[Vauchi] --reset-for-testing: failed to create identity");
            }
        }
#endif

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

        // Register event callback for async core notifications (Phase 2E).
        // Background operations (sync, delivery, device link) fire on arbitrary
        // threads. Marshal to the UI thread via DispatcherQueue before touching UI.
        _eventCallback = OnCoreEvent;
        VauchiNative.AppSetEventCallback(_appHandle, _eventCallback, IntPtr.Zero);
    }

    private void OnCoreEvent(IntPtr screenIdsJsonPtr, IntPtr userData)
    {
        if (_appHandle == IntPtr.Zero || screenIdsJsonPtr == IntPtr.Zero) return;

        // Marshal to UI thread — callback fires on core's background thread
        DispatcherQueue.TryEnqueue(() =>
        {
            RefreshScreen();
            DrainAndShowNotifications();
        });
    }

    private void DrainAndShowNotifications()
    {
        if (_appHandle == IntPtr.Zero || _tray == null) return;

        string? json = VauchiNative.AppDrainNotifications(_appHandle);
        if (json == null || json == "[]") return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var notif in doc.RootElement.EnumerateArray())
            {
                string title = notif.GetProperty("title").GetString() ?? "Vauchi";
                string body = notif.GetProperty("body").GetString() ?? "";
                _tray.ShowNotification(title, body);
            }
        }
        catch (JsonException) { }
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

        foreach (var (screenId, labelKey, icon) in NavTabs)
        {
            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = Localizer.T(labelKey),
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
            or "contact_duplicates" or "contact_merge" or "contact_limit"
            or "archived_contacts" => 1,
        "exchange" => 2,
        "groups" or "group_detail" => 3,
        _ => 4, // settings, help, backup, recovery, sync, etc. → More
    };

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_navUpdating || _appHandle == IntPtr.Zero) return;
        if (args.SelectedItem is not NavigationViewItem item) return;

        string screenId = item.Tag as string ?? "";
        // If on a modal screen (FormDialog), navigate back first to clear it.
        // Core's AppEngine stacks modals — tab switches must pop them.
        string? currentJson = VauchiNative.AppCurrentScreen(_appHandle);
        if (currentJson != null && currentJson.Contains("\"form_dialog\""))
        {
            VauchiNative.AppHandleAction(_appHandle, ActionJson.ActionPressed("cancel"));
        }
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
                // After onboarding completes, core returns NavigateTo (not Complete).
                // Rebuild nav tabs if they're not visible yet (idempotent).
                if (!NavView.IsPaneVisible && _appHandle != IntPtr.Zero)
                    ExitOnboardingMode();
                SyncNavSelection();
                RefreshScreen();
                // Device link: when UI transitions to Syncing, run the protocol handshake
                if (_deviceLinkInitiator != IntPtr.Zero &&
                    resultJson.Contains("\"link_syncing\""))
                {
                    _ = CompleteDeviceLinkAsync();
                }
                break;

            case ActionResultKind.Complete:
            case ActionResultKind.WipeComplete:
                CleanupDeviceLink();
                ExitOnboardingMode();
                string? defaultScreen = VauchiNative.AppDefaultScreen(_appHandle);
                if (defaultScreen != null)
                    VauchiNative.AppNavigateTo(_appHandle, defaultScreen);
                SyncNavSelection();
                RefreshScreen();
                break;

            case ActionResultKind.OpenContact:
                NavigateToParamScreen(resultJson, "OpenContact", "contact_id", "contact_detail");
                break;

            case ActionResultKind.EditContact:
                NavigateToParamScreen(resultJson, "EditContact", "contact_id", "contact_edit");
                break;

            case ActionResultKind.OpenEntryDetail:
                NavigateToParamScreen(resultJson, "OpenEntryDetail", "field_id", "entry_detail");
                break;

            case ActionResultKind.StartBackupImport:
                HandleBackupImport();
                break;

            case ActionResultKind.StartDeviceLink:
                StartDeviceLinkFlow();
                break;

            case ActionResultKind.ExchangeCommands:
                var commands = ExchangeCommandParser.ParseFromActionResult(resultJson);
                HandleExchangeCommands(commands);
                RefreshScreen();
                break;

            case ActionResultKind.ShowAlert:
                ShowAlert(resultJson);
                break;

            case ActionResultKind.OpenUrl:
                OpenUrlAsync(resultJson);
                break;

            case ActionResultKind.ShowToast:
                ShowFloatingToast(resultJson);
                break;

            case ActionResultKind.BackupExportComplete:
                HandleBackupExportComplete(resultJson);
                RefreshScreen();
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

    /// <summary>
    /// Extract a parameter from an action result and navigate to a parameterized screen.
    /// </summary>
    private void NavigateToParamScreen(string resultJson, string variant, string paramField, string screenName)
    {
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            if (doc.RootElement.TryGetProperty(variant, out var inner) &&
                inner.TryGetProperty(paramField, out var paramEl))
            {
                string? param = paramEl.GetString();
                if (param != null)
                {
                    VauchiNative.AppNavigateToParam(_appHandle, screenName, param);
                }
            }
        }
        catch (JsonException) { }

        SyncNavSelection();
        RefreshScreen();
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

    private void ShowAlert(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        if (!doc.RootElement.TryGetProperty("ShowAlert", out var alert)) return;
        string title = alert.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
        string message = alert.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        AlertBar.Title = title;
        AlertBar.Message = message;
        AlertBar.IsOpen = true;
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
        string errorMsg = Localizer.T("error.unknown");
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            if (doc.RootElement.TryGetProperty("error", out var err))
                errorMsg = err.GetString() ?? errorMsg;
        }
        catch { }

        var dialog = new ContentDialog
        {
            Title = Localizer.T("app.error_title"),
            Content = Localizer.T("error.unrecoverable_body", new Dictionary<string, string> { ["message"] = errorMsg }),
            CloseButtonText = Localizer.T("action.ok"),
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
            // Unregister event callback before destroying the app handle
            VauchiNative.AppSetEventCallback(_appHandle, null, IntPtr.Zero);
            _eventCallback = null;

            _ble?.Dispose();
            VauchiNative.AppDestroy(_appHandle);
            _appHandle = IntPtr.Zero;
        }
    }
}
