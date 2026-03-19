// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Vauchi.Platform;

/// <summary>
/// Manages the system tray icon and context menu.
/// </summary>
public class SystemTrayManager : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly Window? _window;
    private readonly Action<string>? _navigateAction;

    public SystemTrayManager(Window window, Action<string>? navigateAction = null)
    {
        _window = window;
        _navigateAction = navigateAction;
    }

    public void Initialize()
    {
        var menu = new MenuFlyout();

        var exchangeItem = new MenuFlyoutItem { Text = "Exchange" };
        exchangeItem.Click += (_, _) => { ShowWindow(); _navigateAction?.Invoke("exchange"); };
        menu.Items.Add(exchangeItem);
        menu.Items.Add(new MenuFlyoutSeparator());

        var showItem = new MenuFlyoutItem { Text = "Show Vauchi" };
        showItem.Click += (_, _) => ShowWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new MenuFlyoutSeparator());

        var quitItem = new MenuFlyoutItem { Text = "Quit" };
        quitItem.Click += (_, _) => Application.Current.Exit();
        menu.Items.Add(quitItem);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Vauchi",
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

    public void Dispose()
    {
        _trayIcon?.Dispose();
    }
}
