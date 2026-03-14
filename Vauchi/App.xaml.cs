// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Microsoft.UI.Xaml;
using Vauchi.Platform;

namespace Vauchi;

/// <summary>
/// WinUI 3 application entry point.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Windows Hello lock gate
        if (SecureStorageService.IsHelloEnabled)
        {
            bool authenticated = false;
            try
            {
                authenticated = await SecureStorageService.AuthenticateAsync();
            }
            catch
            {
                authenticated = false;
            }

            if (!authenticated)
            {
                // Authentication failed — exit the app
                Environment.Exit(1);
                return;
            }
        }

        _window = new MainWindow();
        _window.Activate();
    }
}
