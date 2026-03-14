// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Microsoft.UI.Xaml;

namespace Vauchi.Platform;

/// <summary>
/// Manages the system tray icon and context menu.
/// Uses H.NotifyIcon.WinUI when available (requires net10.0+).
/// </summary>
public class SystemTrayManager : IDisposable
{
    private readonly Window _window;

    // TODO: Uncomment when H.NotifyIcon.WinUI is available (net10.0+)
    // private TaskbarIcon? _trayIcon;

    /// <summary>Raised when the user clicks "Exchange" in the tray menu.</summary>
    public event EventHandler? ExchangeRequested;

    /// <summary>Raised when the user clicks "Quit" in the tray menu.</summary>
    public event EventHandler? QuitRequested;

    public SystemTrayManager(Window window)
    {
        _window = window;
    }

    /// <summary>
    /// Creates the tray icon with context menu entries:
    /// Exchange, Open Vauchi, separator, Quit.
    /// </summary>
    public void Initialize()
    {
        // TODO: Implement with H.NotifyIcon.WinUI when available (net10.0+)
        //
        // _trayIcon = new TaskbarIcon
        // {
        //     ToolTipText = "Vauchi",
        // };
        //
        // var contextMenu = new MenuFlyout();
        //
        // var exchangeItem = new MenuFlyoutItem { Text = "Exchange" };
        // exchangeItem.Click += (_, _) => ExchangeRequested?.Invoke(this, EventArgs.Empty);
        // contextMenu.Items.Add(exchangeItem);
        //
        // var openItem = new MenuFlyoutItem { Text = "Open Vauchi" };
        // openItem.Click += (_, _) => _window.Activate();
        // contextMenu.Items.Add(openItem);
        //
        // contextMenu.Items.Add(new MenuFlyoutSeparator());
        //
        // var quitItem = new MenuFlyoutItem { Text = "Quit" };
        // quitItem.Click += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);
        // contextMenu.Items.Add(quitItem);
        //
        // _trayIcon.ContextFlyout = contextMenu;
        // _trayIcon.TrayMouseDoubleClick += (_, _) => _window.Activate();
    }

    public void Dispose()
    {
        // TODO: Uncomment when H.NotifyIcon.WinUI is available
        // _trayIcon?.Dispose();
        // _trayIcon = null;
        GC.SuppressFinalize(this);
    }
}
