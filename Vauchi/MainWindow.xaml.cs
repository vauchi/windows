// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
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
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        // ActionResult is a serde enum: {"UpdateScreen": {...}} or {"NavigateTo": {...}}
        if (root.TryGetProperty("UpdateScreen", out _) ||
            root.TryGetProperty("NavigateTo", out _))
        {
            RefreshScreen();
        }
        else if (root.TryGetProperty("Complete", out _) ||
                 root.TryGetProperty("WipeComplete", out _))
        {
            RefreshScreen();
        }
        else if (root.TryGetProperty("ValidationError", out _))
        {
            // Re-render current screen (engine state may have updated)
            RefreshScreen();
        }
        else
        {
            // Other results (ShowAlert, OpenUrl, etc.) — refresh for now
            RefreshScreen();
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
