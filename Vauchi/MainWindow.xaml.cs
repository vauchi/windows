// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Microsoft.UI.Xaml;
using Vauchi.Helpers;
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

        Renderer.ActionRequested += OnActionRequested;

        _workflowHandle = VauchiNative.WorkflowCreate("onboarding");
        RefreshScreen();
    }

    private void OnActionRequested(object? sender, string actionJson)
    {
        if (_workflowHandle == IntPtr.Zero)
            return;

        string? resultJson = VauchiNative.WorkflowHandleAction(_workflowHandle, actionJson);
        if (resultJson == null)
            return;

        HandleActionResult(resultJson);
    }

    private void HandleActionResult(string resultJson)
    {
        var kind = ActionResultParser.Classify(resultJson);

        switch (kind)
        {
            case ActionResultKind.UpdateScreen:
            case ActionResultKind.NavigateTo:
            case ActionResultKind.ValidationError:
            case ActionResultKind.Complete:
            case ActionResultKind.WipeComplete:
                RefreshScreen();
                break;

            case ActionResultKind.Error:
                // Native returned an error — refresh to show current state
                RefreshScreen();
                break;

            default:
                // ShowAlert, OpenUrl, OpenContact, etc. — refresh for now
                RefreshScreen();
                break;
        }
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
        Renderer.ActionRequested -= OnActionRequested;

        if (_workflowHandle != IntPtr.Zero)
        {
            VauchiNative.WorkflowDestroy(_workflowHandle);
            _workflowHandle = IntPtr.Zero;
        }
    }
}
