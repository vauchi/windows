// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Vauchi.Services;

namespace Vauchi.Platform;

/// <summary>
/// Manages the system tray icon and context menu.
/// </summary>
public class SystemTrayManager : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly Window? _window;

    public SystemTrayManager(Window window)
    {
        _window = window;
    }

    public void Initialize()
    {
        var menu = new MenuFlyout();

        var showItem = new MenuFlyoutItem { Text = Localizer.T("tray.show_app") };
        showItem.Click += (_, _) => ShowWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new MenuFlyoutSeparator());

        var quitItem = new MenuFlyoutItem { Text = Localizer.T("action.quit") };
        quitItem.Click += (_, _) => Application.Current.Exit();
        menu.Items.Add(quitItem);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = Localizer.T("app.name"),
            MenuActivation = PopupActivationMode.RightClick,
            ContextFlyout = menu,
        };

        _trayIcon.ForceCreate(enablesEfficiencyMode: false);
    }

    private void ShowWindow()
    {
        if (_window is null) return;
        _window.Show(disableEfficiencyMode: true);
        _window.Activate();
    }

    public void ShowNotification(string title, string body)
    {
        _trayIcon?.ShowNotification(title, body);
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
    }
}
