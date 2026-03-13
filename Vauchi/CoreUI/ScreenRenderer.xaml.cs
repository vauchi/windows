// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI;

/// <summary>
/// Reads ScreenModel JSON and builds the UI via ComponentRenderer.
/// </summary>
public sealed partial class ScreenRenderer : UserControl
{
    public ScreenRenderer()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Parse ScreenModel JSON and render all components.
    /// </summary>
    public void RenderFromJson(string screenJson)
    {
        ComponentContainer.Children.Clear();

        using var doc = JsonDocument.Parse(screenJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("components", out var components) &&
            components.ValueKind == JsonValueKind.Array)
        {
            foreach (var component in components.EnumerateArray())
            {
                var control = ComponentRenderer.CreateComponent(component);
                if (control != null)
                {
                    ComponentContainer.Children.Add(control);
                }
            }
        }
    }

    /// <summary>
    /// Raised when a user action should be sent back to the workflow engine.
    /// </summary>
    public event EventHandler<string>? ActionRequested;

    internal void RaiseAction(string actionJson)
    {
        ActionRequested?.Invoke(this, actionJson);
    }
}
