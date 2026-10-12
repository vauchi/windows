// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vauchi.CoreUI;
using Vauchi.Platform;
using Vauchi.Services;

namespace Vauchi;

public partial class App : Application
{
    private const string MainInstanceKey = "main";
    private static readonly TimeSpan RedirectTimeout = TimeSpan.FromSeconds(5);

    private Window? _window;
    private static Mutex? _singleInstanceMutex;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Before the single-instance gate: a leftover instance on a CI
        // runner must not turn the catalog run into a silent no-op.
        if (ScreenCatalog.TryParseLaunchArguments(
                Environment.GetCommandLineArgs(),
                out string catalogPath,
                out string outputDirectory))
        {
            Localizer.Init(Path.Combine(AppContext.BaseDirectory, "locales"));
            var renderWindow = new MainWindow(initializeCore: false);
            _window = renderWindow;
            renderWindow.Activate();
            _ = ScreenCatalogRenderer.RunAsync(renderWindow, catalogPath, outputDirectory);
            return;
        }

        _singleInstanceMutex = new Mutex(true, @"Global\VauchiDesktopSingleInstance", out bool isNew);
        if (!isNew)
        {
            // The running instance takes this launch's vauchi:// link
            // (vauchi/private#650) and comes forward; this one ends.
            RedirectToRunningInstance();
            Environment.Exit(0);
            return;
        }
        AppInstance mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
        mainInstance.Activated += OnRedirectedActivation;

        // Point the CABI i18n subsystem at the bundled locale files so
        // Localizer.T("key") returns translated strings. The `locales`
        // folder is copied next to the executable via Vauchi.csproj.
        // Failure here is non-fatal — Localizer falls back to raw keys.
        string localesDir = Path.Combine(AppContext.BaseDirectory, "locales");
        Localizer.Init(localesDir);

        try
        {
            var mainWindow = new MainWindow();
            _window = mainWindow;
            mainWindow.Activate();
            foreach (string uri in LinksFrom(AppInstance.GetCurrent().GetActivatedEventArgs()))
                mainWindow.OpenDeepLink(uri);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Vauchi] Launch failed: {ex}");
            throw;
        }
    }

    private static void RedirectToRunningInstance()
    {
        AppActivationArguments activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        AppInstance mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
        if (mainInstance.IsCurrent)
            return;
        // Off the UI thread: waiting on the redirect from the STA thread
        // that has to pump its COM call deadlocks.
        Task.Run(() => mainInstance.RedirectActivationToAsync(activation).AsTask())
            .Wait(RedirectTimeout);
    }

    private void OnRedirectedActivation(object? sender, AppActivationArguments activation)
    {
        IReadOnlyList<string> links = LinksFrom(activation);
        _window?.DispatcherQueue.TryEnqueue(() =>
        {
            if (_window is not MainWindow mainWindow)
                return;
            mainWindow.Activate();
            foreach (string uri in links)
                mainWindow.OpenDeepLink(uri);
        });
    }

    private static IReadOnlyList<string> LinksFrom(AppActivationArguments activation) =>
        activation.Kind switch
        {
            ExtendedActivationKind.Protocol when activation.Data is Windows.ApplicationModel.Activation.IProtocolActivatedEventArgs protocol =>
                DeepLinks.Select(new[] { protocol.Uri.OriginalString }),
            ExtendedActivationKind.Launch when activation.Data is Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs launch =>
                DeepLinks.Select(launch.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            _ => Array.Empty<string>(),
        };
}
