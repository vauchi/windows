// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Vauchi.Handlers;
using Vauchi.Interop;
using Vauchi.Platform;
using Vauchi.Services;

namespace Vauchi;

public sealed partial class MainWindow : Window
{
    private IntPtr _appHandle;
    private SystemTrayManager? _tray;
    private ExchangeCommandHandler? _exchange;
    private VauchiNative.VauchiEventCallback? _eventCallback;
    private DispatcherTimer? _wakeupTimer;

    public MainWindow()
    {
        InitializeComponent();
        Title = Localizer.T("app.name");
        ScreenCaptureProtection.Enable(this);

        Presentation.NativeEffectReady += ExecuteNativeEffect;
        Presentation.NativeBackRequested += HideWindow;
        Activated += OnActivated;
        _ = InitializeAsync();
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        try
        {
            if (SecureStorageService.IsHelloEnabled
                && !await SecureStorageService.AuthenticateAsync())
            {
                var dialog = new ContentDialog
                {
                    Title = Localizer.T("auth.required_title"),
                    Content = Localizer.T("auth.windows_hello_required"),
                    PrimaryButtonText = Localizer.T("action.retry"),
                    CloseButtonText = Localizer.T("action.quit"),
                    XamlRoot = Content.XamlRoot,
                };
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await InitializeAsync();
                    return;
                }
                Application.Current.Exit();
                return;
            }

            InitializeApp();
            RestoreWindowState();
            _tray = new SystemTrayManager(this);
            _tray.Initialize();
        }
        catch (Exception exception)
        {
            await ShowInitializationError(exception);
        }
    }

    private void InitializeApp()
    {
        string dataDirectory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vauchi");
        byte[] key = KeyStorageService.RetrieveKey()
                     ?? KeyStorageService.GenerateKey();
        KeyStorageService.StoreKey(key);

        IntPtr config = VauchiNative.ConfigNew(dataDirectory, null);
        VauchiNative.ConfigSetStorageKey(config, key, (nuint)key.Length);
        Array.Clear(key);
        VauchiNative.ConfigEnableBle(config, true);
        VauchiNative.ConfigEnableAudio(config, true);
        _appHandle = VauchiNative.AppCreateFromConfig(config);
        if (_appHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to initialize the Core application engine.");
        }

#if DEBUG
        if (Array.Exists(
                Environment.GetCommandLineArgs(),
                argument => argument == "--reset-for-testing")
            && VauchiNative.AppHasIdentity(_appHandle) != 1)
        {
            VauchiNative.AppCreateIdentity(_appHandle, "Test User");
        }
#endif

        _exchange = new ExchangeCommandHandler(
            Presentation.DispatchPlatformEvent,
            DispatcherQueue,
            this);
        Presentation.Initialize(_appHandle);

        _eventCallback = OnCoreEvent;
        VauchiNative.AppSetEventCallback(_appHandle, _eventCallback, IntPtr.Zero);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_appHandle == IntPtr.Zero)
            return;
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            Presentation.DispatchPlatformEvent("\"AppBackgrounded\"");
            return;
        }
        Presentation.Refresh();
        RunWakeupTick();
    }

    private void OnCoreEvent(IntPtr screenIdsJsonPtr, IntPtr userData)
    {
        if (_appHandle == IntPtr.Zero || screenIdsJsonPtr == IntPtr.Zero)
            return;
        DispatcherQueue.TryEnqueue(() =>
        {
            Presentation.Refresh();
            DrainAndShowNotifications();
        });
    }

    private async System.Threading.Tasks.Task ShowInitializationError(Exception exception)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[Vauchi] Initialization failed: {exception}");
        var dialog = new ContentDialog
        {
            Title = Localizer.T("app.error_title"),
            Content = exception.Message,
            CloseButtonText = Localizer.T("action.ok"),
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private void HideWindow()
    {
        try
        {
            AppWindow?.Hide();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] Native back failed: {exception.Message}");
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        SaveWindowState();
        Presentation.NativeEffectReady -= ExecuteNativeEffect;
        Presentation.NativeBackRequested -= HideWindow;
        _tray?.Dispose();
        _wakeupTimer?.Stop();
        if (_appHandle == IntPtr.Zero)
            return;

        VauchiNative.AppSetEventCallback(_appHandle, null, IntPtr.Zero);
        _eventCallback = null;
        _exchange?.Dispose();
        VauchiNative.AppDestroy(_appHandle);
        _appHandle = IntPtr.Zero;
    }
}
