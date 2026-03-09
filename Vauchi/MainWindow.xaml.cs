// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Microsoft.UI.Xaml;
using Vauchi.Interop;

namespace Vauchi;

/// <summary>
/// Main window hosting the ScreenRenderer.
/// Creates the onboarding workflow on initialization.
/// </summary>
public sealed partial class MainWindow : Window
{
    private IntPtr _workflowHandle;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Vauchi";

        _workflowHandle = VauchiNative.WorkflowCreate("onboarding");
        RefreshScreen();
    }

    private void RefreshScreen()
    {
        string? screenJson = VauchiNative.WorkflowCurrentScreen(_workflowHandle);
        if (screenJson != null)
        {
            Renderer.RenderFromJson(screenJson);
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_workflowHandle != IntPtr.Zero)
        {
            VauchiNative.WorkflowDestroy(_workflowHandle);
            _workflowHandle = IntPtr.Zero;
        }
    }
}
