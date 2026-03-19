// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace Vauchi;

public partial class App : Application
{
    private Window? _window;
    private static Mutex? _singleInstanceMutex;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _singleInstanceMutex = new Mutex(true, @"Global\VauchiDesktopSingleInstance", out bool isNew);
        if (!isNew)
        {
            Environment.Exit(0);
            return;
        }

        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Vauchi] Launch failed: {ex}");
            throw;
        }
    }
}
