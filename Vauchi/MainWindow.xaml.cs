// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    public MainWindow()
    {
        InitializeComponent();
        Title = "Vauchi";

        Renderer.ActionRequested += OnActionRequested;
        InitializeApp();
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
            _appHandle = VauchiNative.AppCreateWithRelay(null);
        }

        string? defaultScreen = VauchiNative.AppDefaultScreen(_appHandle);
        if (defaultScreen != null)
        {
            VauchiNative.AppNavigateTo(_appHandle, defaultScreen);
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

        string? resultJson = VauchiNative.AppHandleAction(_appHandle, actionJson);
        if (resultJson == null) return;

        HandleActionResult(resultJson);
    }

    private void HandleActionResult(string resultJson)
    {
        var kind = ActionResultParser.Classify(resultJson);

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

            case ActionResultKind.Error:
                RefreshScreen();
                break;

            default:
                RefreshScreen();
                break;
        }
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
                    // TODO: launch camera scanner
                    break;
                default:
                    break;
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

        if (_appHandle != IntPtr.Zero)
        {
            VauchiNative.AppDestroy(_appHandle);
            _appHandle = IntPtr.Zero;
        }
    }
}
