// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Windows.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.Platform;

/// <summary>
/// Manages the system tray icon and context menu using H.NotifyIcon.WinUI.
/// </summary>
public class SystemTrayManager : IDisposable
{
    private readonly Window _window;
    private TaskbarIcon? _trayIcon;

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
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Vauchi",
            DoubleClickCommand = new RelayCommand(() => _window.Activate()),
        };

        var contextMenu = new MenuFlyout();

        var exchangeItem = new MenuFlyoutItem { Text = "Exchange" };
        exchangeItem.Click += (_, _) => ExchangeRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(exchangeItem);

        var openItem = new MenuFlyoutItem { Text = "Open Vauchi" };
        openItem.Click += (_, _) => _window.Activate();
        contextMenu.Items.Add(openItem);

        contextMenu.Items.Add(new MenuFlyoutSeparator());

        var quitItem = new MenuFlyoutItem { Text = "Quit" };
        quitItem.Click += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(quitItem);

        _trayIcon.ContextFlyout = contextMenu;
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Simple ICommand implementation for tray icon double-click.
    /// </summary>
    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
