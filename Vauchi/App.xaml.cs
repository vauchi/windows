// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading;
using Vauchi.Services;

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

        // Point the CABI i18n subsystem at the bundled locale files so
        // Localizer.T("key") returns translated strings. The `locales`
        // folder is copied next to the executable via Vauchi.csproj.
        // Failure here is non-fatal — Localizer falls back to raw keys.
        string localesDir = Path.Combine(AppContext.BaseDirectory, "locales");
        Localizer.Init(localesDir);

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
