// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vauchi.Interop;
using Vauchi.Platform;
using Windows.System;

namespace Vauchi;

/// <summary>
/// Main window hosting a NavigationView with ScreenRenderer.
/// Drives the AppEngine action loop.
/// </summary>
public sealed partial class MainWindow : Window
{
    private IntPtr _appHandle;
    private bool _suppressSelectionChanged;
    private SystemTrayManager? _trayManager;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Vauchi";

        // Set initial and minimum window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1024, 768));

        // Enforce minimum window size via Changed event
        appWindow.Changed += (sender, args) =>
        {
            if (args.DidSizeChange)
            {
                var size = sender.Size;
                var changed = false;
                var w = size.Width < 720 ? 720 : size.Width;
                var h = size.Height < 480 ? 480 : size.Height;
                if (w != size.Width || h != size.Height)
                    sender.Resize(new Windows.Graphics.SizeInt32(w, h));
            }
        };

        _appHandle = VauchiNative.AppCreateWithRelay(VauchiNative.DefaultRelayUrl);

        Renderer.ActionRequested += OnActionRequested;
        this.Closed += OnClosed;

        // System tray
        _trayManager = new SystemTrayManager(this);
        _trayManager.Initialize();
        _trayManager.ExchangeRequested += (_, _) =>
        {
            this.Activate();
            var result = VauchiNative.AppNavigateTo(_appHandle, "exchange");
            if (result != null)
                ApplyResult(result);
        };
        _trayManager.QuitRequested += (_, _) =>
        {
            _trayManager?.Dispose();
            _trayManager = null;
            this.Close();
        };

        // Minimize to tray on close
        appWindow.Closing += (sender, args) =>
        {
            args.Cancel = true;
            this.AppWindow.Hide();
        };

        // Keyboard shortcuts
        KeyboardShortcuts.Register(
            NavView,
            onNavigate: screenName =>
            {
                var result = VauchiNative.AppNavigateTo(_appHandle, screenName);
                if (result != null)
                    ApplyResult(result);
                else
                    RefreshScreen();
            },
            onBack: () =>
            {
                string actionJson = """{"ActionPressed": {"action_id": "back"}}""";
                string? result = VauchiNative.AppHandleAction(_appHandle, actionJson);
                if (result != null)
                    ApplyResult(result);
            },
            onQuit: () =>
            {
                _trayManager?.Dispose();
                _trayManager = null;
                this.Close();
            });

        PopulateNavigation();
        SelectDefaultScreen();
        RefreshScreen();
    }

    private void PopulateNavigation()
    {
        NavView.MenuItems.Clear();

        string? screensJson = VauchiNative.AppAvailableScreens(_appHandle);
        if (string.IsNullOrEmpty(screensJson))
        {
            // No available screens (onboarding) — hide nav pane
            NavView.IsPaneVisible = false;
            NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(screensJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return;

            bool hasScreens = false;
            foreach (var screenEl in doc.RootElement.EnumerateArray())
            {
                string? screenName = screenEl.GetString();
                if (string.IsNullOrEmpty(screenName)) continue;

                var item = new NavigationViewItem
                {
                    Content = FormatScreenName(screenName),
                    Tag = screenName,
                };
                NavView.MenuItems.Add(item);
                hasScreens = true;
            }

            NavView.IsPaneVisible = hasScreens;
            NavView.IsBackButtonVisible = hasScreens
                ? NavigationViewBackButtonVisible.Visible
                : NavigationViewBackButtonVisible.Collapsed;
        }
        catch (JsonException)
        {
            NavView.IsPaneVisible = false;
        }
    }

    private void SelectDefaultScreen()
    {
        var defaultScreen = VauchiNative.AppDefaultScreen(_appHandle);
        if (string.IsNullOrEmpty(defaultScreen)) return;

        _suppressSelectionChanged = true;
        foreach (var menuItem in NavView.MenuItems)
        {
            if (menuItem is NavigationViewItem item && item.Tag is string tag && tag == defaultScreen)
            {
                NavView.SelectedItem = item;
                break;
            }
        }
        _suppressSelectionChanged = false;
    }

    private void RefreshScreen()
    {
        string? screenJson = VauchiNative.AppCurrentScreen(_appHandle);
        if (screenJson != null)
        {
            Renderer.RenderFromJson(screenJson);
        }
    }

    private void OnActionRequested(object? sender, string actionJson)
    {
        string? resultJson = VauchiNative.AppHandleAction(_appHandle, actionJson);
        if (resultJson != null)
        {
            ApplyResult(resultJson);
        }
    }

    private async void ApplyResult(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        // String variants
        if (root.ValueKind == JsonValueKind.String)
        {
            string? variant = root.GetString();
            switch (variant)
            {
                case "Complete":
                case "WipeComplete":
                    PopulateNavigation();
                    RefreshScreen();
                    break;
                case "RequestCamera":
                    // TODO: Camera integration
                    break;
                case "StartDeviceLink":
                    // TODO: Device linking
                    break;
                case "StartBackupImport":
                    // TODO: Backup import
                    break;
            }
            return;
        }

        // Object variants
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "UpdateScreen":
                    {
                        string screenJson = property.Value.GetRawText();
                        Renderer.RenderFromJson(screenJson);
                        break;
                    }

                    case "NavigateTo":
                    {
                        string screenJson = property.Value.GetRawText();
                        Renderer.RenderFromJson(screenJson);
                        PopulateNavigation();
                        break;
                    }

                    case "ValidationError":
                        RefreshScreen();
                        break;

                    case "ShowAlert":
                    {
                        string title = "";
                        string message = "";

                        if (property.Value.TryGetProperty("title", out var titleEl))
                            title = titleEl.GetString() ?? "";
                        if (property.Value.TryGetProperty("message", out var msgEl))
                            message = msgEl.GetString() ?? "";

                        var dialog = new ContentDialog
                        {
                            Title = title,
                            Content = message,
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot,
                        };
                        await dialog.ShowAsync();
                        break;
                    }

                    case "OpenUrl":
                    {
                        string? url = property.Value.GetString();
                        if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            await Launcher.LaunchUriAsync(uri);
                        }
                        break;
                    }

                    case "OpenContact":
                    {
                        // Core returns OpenContact with a contact_id — navigate via AppNavigateTo
                        var contactId = property.Value.TryGetProperty("contact_id", out var cid)
                            ? cid.GetString() ?? "" : "";
                        var navResult = VauchiNative.AppNavigateTo(_appHandle, "contacts");
                        if (navResult != null)
                            Renderer.RenderFromJson(navResult);
                        break;
                    }

                    case "EditContact":
                    {
                        var contactId = property.Value.TryGetProperty("contact_id", out var cid)
                            ? cid.GetString() ?? "" : "";
                        var navResult = VauchiNative.AppNavigateTo(_appHandle, "contacts");
                        if (navResult != null)
                            Renderer.RenderFromJson(navResult);
                        break;
                    }

                    case "OpenEntryDetail":
                    {
                        // Navigate to entry detail — refresh current screen as the detail
                        // is typically shown in the current context
                        RefreshScreen();
                        break;
                    }

                    case "ShowToast":
                        // TODO: Toast notification
                        break;

                    case "Error":
                    {
                        string errorMsg = property.Value.GetString() ?? "An unexpected error occurred.";
                        var dialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = errorMsg,
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot,
                        };
                        await dialog.ShowAsync();
                        break;
                    }
                }
                break; // Only process first property (externally tagged enum)
            }
            return;
        }

        // Unrecognized JSON — show error
        var errorDialog = new ContentDialog
        {
            Title = "Unexpected Error",
            Content = $"Received unrecognized result: {resultJson}",
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot,
        };
        await errorDialog.ShowAsync();
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        string actionJson = """{"ActionPressed": {"action_id": "back"}}""";
        string? resultJson = VauchiNative.AppHandleAction(_appHandle, actionJson);
        if (resultJson != null)
        {
            ApplyResult(resultJson);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_suppressSelectionChanged) return;

        if (args.SelectedItem is NavigationViewItem item && item.Tag is string screenName)
        {
            string? resultJson = VauchiNative.AppNavigateTo(_appHandle, screenName);
            if (resultJson != null)
            {
                ApplyResult(resultJson);
            }
            else
            {
                // NavigateTo returned null — just refresh
                RefreshScreen();
            }
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        Renderer.ActionRequested -= OnActionRequested;

        _trayManager?.Dispose();
        _trayManager = null;

        if (_appHandle != IntPtr.Zero)
        {
            VauchiNative.AppDestroy(_appHandle);
            _appHandle = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Formats a snake_case screen name into a display-friendly title.
    /// e.g. "exchange" → "Exchange", "my_info" → "My Info"
    /// </summary>
    private static string FormatScreenName(string screenName)
    {
        if (string.IsNullOrEmpty(screenName)) return screenName;

        var parts = screenName.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
            }
        }
        return string.Join(" ", parts);
    }
}
