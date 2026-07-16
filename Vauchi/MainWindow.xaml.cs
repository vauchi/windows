// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Vauchi.Handlers;
using Vauchi.Helpers;
using Vauchi.Interop;
using Vauchi.Platform;
using Vauchi.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Vauchi;

public sealed partial class MainWindow : Window
{
    private IntPtr _appHandle;
    private bool _navUpdating;
    private SystemTrayManager? _tray;
    private DispatcherTimer? _toastTimer;
    private string? _toastUndoActionId;
    private ExchangeCommandHandler? _exchange;
    // Prevent GC collection of the event callback delegate (P/Invoke requirement)
    private VauchiNative.VauchiEventCallback? _eventCallback;

    private DispatcherTimer? _wakeupTimer;

    public MainWindow()
    {
        InitializeComponent();
        Title = Localizer.T("app.name");

        // Opt the window out of screen capture (matches iOS / Android
        // behaviour). DEBUG builds are exempt for UI-automation tooling.
        ScreenCaptureProtection.Enable(this);

        Renderer.ActionRequested += OnActionRequested;
        Renderer.BackRequested += () =>
        {
            if (_appHandle == IntPtr.Zero) return;
            string? resultJson = VauchiNative.AppHandleAction(_appHandle, ActionJson.NavigateBack());
            if (resultJson != null) HandleActionResult(resultJson);
        };

        // Async init with optional Windows Hello gate
        _ = InitializeAsync();
        Activated += OnActivated;
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
            // Re-fetch the current screen on background→foreground.
            // Listener events cover most state changes during
            // backgrounding, but a missed event would leave the UI
            // stale until the next user action.
            RefreshScreen();
            // Run a core-scheduled wakeup tick when the app is activated
            // (ADR-044 Am2a replaces the fixed 30 s poll loop).
            RunWakeupTick();
        }
    }

    /// <summary>
    /// Runs a core wakeup tick via <see cref="VauchiNative.AppOnWakeup"/>,
    /// posts any OS notifications returned, and re-arms the next timer from
    /// the <c>ScheduleWakeup</c> command core emits.
    /// </summary>
    private void RunWakeupTick()
    {
        if (_appHandle == IntPtr.Zero) return;

        try
        {
            string? json = VauchiNative.AppOnWakeup(_appHandle);
            if (string.IsNullOrEmpty(json)) return;

            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("notifications", out var notifications)
                && notifications.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement element in notifications.EnumerateArray())
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

            if (root.TryGetProperty("commands", out var commands)
                && commands.ValueKind == JsonValueKind.Array)
            {
                ArmWakeupTimer(commands);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunWakeupTick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the first <c>Command::ScheduleWakeup</c> in the command batch
    /// and arms the foreground timer from its <c>earliest_secs</c>.
    /// </summary>
    private void ArmWakeupTimer(JsonElement commands)
    {
        foreach (JsonElement command in commands.EnumerateArray())
        {
            if (command.ValueKind == JsonValueKind.Object
                && command.TryGetProperty("ScheduleWakeup", out var schedule)
                && schedule.ValueKind == JsonValueKind.Object
                && schedule.TryGetProperty("earliest_secs", out var earliest)
                && earliest.TryGetInt32(out int earliestSecs)
                && earliestSecs > 0)
            {
                int intervalSecs = earliestSecs;
                if (schedule.TryGetProperty("min_interval_secs", out var minInterval)
                    && minInterval.TryGetInt32(out int minIntervalSecs)
                    && intervalSecs < minIntervalSecs)
                {
                    intervalSecs = minIntervalSecs;
                }

                _wakeupTimer?.Stop();
                _wakeupTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(intervalSecs)
                };
                _wakeupTimer.Tick += (s, e) => RunWakeupTick();
                _wakeupTimer.Start();
                break;
            }
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
            // Core-driven back: forward the OS back gesture unconditionally;
            // core decides whether to pop nav state or emit PerformNativeBack
            // (ADR-044 Amendment 2a).
            string? resultJson = VauchiNative.AppHandleAction(_appHandle, ActionJson.NavigateBack());
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

// TODO(HUMBLE): D — test-only identity creation gate in production code; drive test setup via core testing hook (see _private/docs/problems/2026-07-06-desktop-tui-web-domain-shell-violations).
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

        // Window chrome only — no explicit navigate. Core's startup
        // decision (Onboarding / Lock / MyInfo) is already set by
        // `vauchi_app_create*` and surfaces through
        // `AppCurrentScreen` in `RefreshScreen()`. An explicit
        // `AppDefaultScreen()` + `AppNavigateTo()` here would bypass
        // the Lock state for password-protected installs.
        RefreshScreen();
        SyncNavChrome();

        _exchange = new ExchangeCommandHandler(SendHardwareEventToCore, DispatcherQueue, this);

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

    /// <summary>
    /// Show or hide the sidebar chrome based on core-provided
    /// <c>nav_tab_id</c>. A non-null tab id means the screen belongs to
    /// the post-auth tab set; null means onboarding/lock/transient.
    /// Replaces hardcoded <c>is_home</c>/<c>is_bootstrap</c> gates
    /// (ADR-044 Am2a).
    /// </summary>
    private void SyncNavChrome()
    {
        string? navTabId = ReadNavTabId();
        bool hasTab = !string.IsNullOrEmpty(navTabId);

        NavView.IsPaneVisible = hasTab;
        NavView.IsBackButtonVisible = hasTab
            ? NavigationViewBackButtonVisible.Auto
            : NavigationViewBackButtonVisible.Collapsed;

        if (hasTab && NavView.MenuItems.Count == 0)
        {
            BuildNavTabs();
        }
    }

    private string? ReadNavTabId()
    {
        string? screenJson = VauchiNative.AppCurrentScreen(_appHandle);
        if (screenJson == null) return null;
        try
        {
            using var doc = JsonDocument.Parse(screenJson);
            if (doc.RootElement.TryGetProperty("nav_tab_id", out var tabId)
                && tabId.ValueKind == JsonValueKind.String)
            {
                return tabId.GetString();
            }
        }
        catch (JsonException) { }
        return null;
    }

    private void BuildNavTabs()
    {
        _navUpdating = true;
        NavView.MenuItems.Clear();

        // Core owns the screen set and localized labels — Windows only
        // picks the native SymbolIcon per screen_id. Sidebar_items
        // returns the broader desktop set (14 top-level screens
        // post-identity, or just Onboarding before).
        string? json = VauchiNative.AppSidebarItems(_appHandle, Localizer.CurrentLocale);
        if (json is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (JsonElement tab in doc.RootElement.EnumerateArray())
                {
                    string screenId = tab.GetProperty("id").GetString() ?? "";
                    string actionId = tab.TryGetProperty("action_id", out var aid)
                        ? aid.GetString() ?? screenId
                        : screenId;
                    string label = tab.GetProperty("label").GetString() ?? screenId;
                    Symbol icon = NavIconCatalogue.For(screenId);
                    NavView.MenuItems.Add(new NavigationViewItem
                    {
                        Content = label,
                        Icon = new SymbolIcon(icon),
                        // Tag carries the two roles core keeps distinct (ADR-043
                        // Am4): Id for selection equality, ActionId as the opaque
                        // nav token forwarded on tap.
                        Tag = new NavTab(screenId, actionId),
                    });
                }
            }
            catch (JsonException) { }
        }

        _navUpdating = false;
        SyncNavSelection();
    }

    /// <summary>
    /// After any screen change, highlight the correct tab based on
    /// core-provided screen metadata. Reads `parent_screen_id` (the
    /// sidebar tab a sub-screen belongs to, surfaced by
    /// `2026-05-01-screen-id-metadata-in-core` G1) and falls back to
    /// `screen_id` for top-level tabs that are their own parent.
    /// </summary>
    private void SyncNavSelection()
    {
        _navUpdating = true;

        string parentId = ReadParentTabId();
        for (int i = 0; i < NavView.MenuItems.Count; i++)
        {
            if (NavView.MenuItems[i] is NavigationViewItem item
                && item.Tag is NavTab tab
                && tab.Id == parentId)
            {
                NavView.SelectedItem = item;
                break;
            }
        }

        _navUpdating = false;
    }

    /// <summary>
    /// Returns the sidebar tab id that should be highlighted for the
    /// current screen. Reads core-provided `parent_screen_id` first,
    /// then <c>nav_tab_id</c>, then <c>screen_id</c> so existing tabs
    /// select themselves (ADR-044 Am2a).
    /// </summary>
    private string ReadParentTabId()
    {
        string? screenJson = VauchiNative.AppCurrentScreen(_appHandle);
        if (screenJson == null) return "";
        try
        {
            using var doc = JsonDocument.Parse(screenJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("parent_screen_id", out var pid)
                && pid.ValueKind == JsonValueKind.String)
            {
                return pid.GetString() ?? "";
            }
            if (root.TryGetProperty("nav_tab_id", out var navTabId)
                && navTabId.ValueKind == JsonValueKind.String)
            {
                return navTabId.GetString() ?? "";
            }
            return root.TryGetProperty("screen_id", out var sid)
                && sid.ValueKind == JsonValueKind.String
                ? sid.GetString() ?? "" : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    /// <summary>
    /// Returns true when core's current screen is presented as a modal
    /// (e.g. form dialog, confirmation). Reads the typed
    /// `presentation_kind` field surfaced by
    /// `2026-05-01-screen-id-metadata-in-core` G2 — replaces the
    /// previous `screen_id == "form_dialog"` substring check.
    /// </summary>
    private bool IsCurrentScreenModal()
    {
        string? currentJson = VauchiNative.AppCurrentScreen(_appHandle);
        if (currentJson == null) return false;
        try
        {
            using var doc = JsonDocument.Parse(currentJson);
            return doc.RootElement.TryGetProperty("presentation_kind", out var kind)
                && kind.ValueKind == JsonValueKind.String
                && kind.GetString() == "Modal";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sidebar tab identity. <see cref="Id"/> (the canonical screen_id) drives
    /// selection equality against core's <c>parent_screen_id</c>;
    /// <see cref="ActionId"/> is the opaque token core minted on
    /// <c>TabInfo.action_id</c>, forwarded verbatim as a <c>NavigateToTab</c>
    /// action. Kept distinct per ADR-043 Amendment 4 — the renderer never
    /// parses or branches on either.
    /// </summary>
    private sealed record NavTab(string Id, string ActionId);

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_navUpdating || _appHandle == IntPtr.Zero) return;
        if (args.SelectedItem is not NavigationViewItem item) return;
        if (item.Tag is not NavTab tab) return;

        // Core's AppEngine stacks modals — tab switches must pop them.
        if (IsCurrentScreenModal())
        {
            VauchiNative.AppHandleAction(_appHandle, ActionJson.ActionPressed("cancel"));
        }
        // ADR-043 Am4 / Tier-1: forward the opaque action_id core minted on the
        // tab. Core routes UserAction::NavigateToTab to a NavigateTo result; the
        // renderer never constructs a navigation target. RefreshScreen() re-reads
        // the now-current screen.
        VauchiNative.AppHandleAction(_appHandle, ActionJson.NavigateToTab(tab.ActionId));
        RefreshScreen();
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (_appHandle == IntPtr.Zero) return;
        string? resultJson = VauchiNative.AppHandleAction(_appHandle, ActionJson.NavigateBack());
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

    /// <summary>
    /// Wires <see cref="ExchangeCommandHandler"/> hardware events back
    /// into core: forwards to <c>AppHandleHardwareEvent</c> and routes
    /// the resulting <c>ActionResult</c> JSON through
    /// <see cref="HandleActionResult"/>. Centralises the
    /// <c>_appHandle == IntPtr.Zero</c> guard so the handler stays
    /// CABI-handle-agnostic.
    /// </summary>
    private void SendHardwareEventToCore(string eventJson)
    {
        if (_appHandle == IntPtr.Zero) return;
        string? resultJson = VauchiNative.AppHandleHardwareEvent(_appHandle, eventJson);
        if (resultJson != null) HandleActionResult(resultJson);
    }

    /// <summary>
    /// Native navigation helper used by handlers that need to route
    /// the UI to a specific screen. Combines
    /// <c>AppNavigateTo</c> + nav-selection sync + screen refresh
    /// into one call.
    /// </summary>
    private void NavigateToScreen(string screenId)
    {
        if (_appHandle == IntPtr.Zero) return;
        VauchiNative.AppNavigateTo(_appHandle, screenId);
        SyncNavSelection();
        RefreshScreen();
    }

    /// <summary>
    /// Handles <c>ActionResult::BackupExportComplete</c> — the legacy
    /// (non-<c>ExchangeCommand</c>) path core uses to ship encrypted
    /// backup bytes to the frontend for native file-save. The data
    /// arrives as a hex-encoded string in the action result; we
    /// decode, then surface a <c>FileSavePicker</c> with a default
    /// <c>vauchi-backup-YYYY-MM-DD.vbk</c> filename and write the
    /// raw bytes on selection.
    /// </summary>
    // TODO(HUMBLE): T — frontend assembles backup export filename/filter from BackupExportComplete data; core should issue SaveFileToUser Command with metadata (see _private/docs/problems/2026-07-06-desktop-tui-web-domain-shell-violations).
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

    private void HandleActionResult(string resultJson)
    {
        var kind = ActionResultParser.Classify(resultJson);
        System.Diagnostics.Debug.WriteLine($"[Vauchi] ResultKind: {kind}");

        switch (kind)
        {
            case ActionResultKind.UpdateScreen:
            case ActionResultKind.NavigateTo:
            case ActionResultKind.ValidationError:
                SyncNavChrome();
                SyncNavSelection();
                RefreshScreen();
                break;

            case ActionResultKind.Complete:
            case ActionResultKind.WipeComplete:
                string? defaultScreen = VauchiNative.AppDefaultScreen(_appHandle);
                if (defaultScreen != null)
                    VauchiNative.AppNavigateTo(_appHandle, defaultScreen);
                SyncNavChrome();
                SyncNavSelection();
                RefreshScreen();
                break;

            case ActionResultKind.Commands:
                var commands = ExchangeCommandParser.ParseFromActionResult(resultJson);
                _exchange?.Handle(commands);
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

            case ActionResultKind.PerformNativeBack:
                // Back reached a back-stopping root. Desktop's native default
                // is to hide the window (minimize / suspend) (ADR-044 Am2a).
                try
                {
                    AppWindow?.Hide();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Vauchi] PerformNativeBack failed: {ex.Message}");
                }
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
        string? undoActionId = toast.TryGetProperty("undo_action_id", out var id)
            ? id.GetString()
            : null;
        string? undoLabel = toast.TryGetProperty("undo_label", out var label)
            ? label.GetString()
            : null;
        FloatingToast.Message = message;
        _toastUndoActionId = !string.IsNullOrEmpty(undoActionId) && !string.IsNullOrEmpty(undoLabel)
            ? undoActionId
            : null;
        FloatingToastAction.Content = undoLabel ?? "";
        FloatingToastAction.Visibility = _toastUndoActionId != null
            ? Visibility.Visible
            : Visibility.Collapsed;
        FloatingToast.IsOpen = true;

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer?.Stop();
            FloatingToast.IsOpen = false;
            _toastUndoActionId = null;
            FloatingToastAction.Visibility = Visibility.Collapsed;
        };
        _toastTimer.Start();
    }

    private void FloatingToastAction_Click(object sender, RoutedEventArgs e)
    {
        if (_appHandle == IntPtr.Zero || _toastUndoActionId == null) return;

        string actionId = _toastUndoActionId;
        _toastTimer?.Stop();
        _toastUndoActionId = null;
        FloatingToast.IsOpen = false;
        FloatingToastAction.Visibility = Visibility.Collapsed;

        string? resultJson = VauchiNative.AppHandleAction(
            _appHandle,
            ActionJson.UndoPressed(actionId));
        if (resultJson != null) HandleActionResult(resultJson);
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
        _wakeupTimer?.Stop();

        if (_appHandle != IntPtr.Zero)
        {
            // Unregister event callback before destroying the app handle
            VauchiNative.AppSetEventCallback(_appHandle, null, IntPtr.Zero);
            _eventCallback = null;

            _exchange?.Dispose();
            VauchiNative.AppDestroy(_appHandle);
            _appHandle = IntPtr.Zero;
        }
    }
}
