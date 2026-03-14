// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Vauchi.Platform;

namespace Vauchi;

/// <summary>
/// WinUI 3 application entry point.
/// </summary>
public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Single instance enforcement
        _singleInstanceMutex = new Mutex(true, "Global\\VauchiDesktopApp", out bool createdNew);

        if (!createdNew)
        {
            // Another instance is running — bring it to the foreground and exit
            BringExistingWindowToFront();
            Environment.Exit(0);
            return;
        }

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
                _singleInstanceMutex.ReleaseMutex();
                Environment.Exit(1);
                return;
            }
        }

        _window = new MainWindow();
        _window.Activate();
    }

    private static void BringExistingWindowToFront()
    {
        IntPtr hwnd = FindWindow(null, "Vauchi");
        if (hwnd != IntPtr.Zero)
        {
            // Restore if minimized, then bring to front
            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
        }
    }

    private const int SW_RESTORE = 9;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
