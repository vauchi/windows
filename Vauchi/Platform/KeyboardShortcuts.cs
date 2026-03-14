// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Vauchi.Platform;

/// <summary>
/// Registers global keyboard shortcuts on a NavigationView.
/// </summary>
public static class KeyboardShortcuts
{
    /// <summary>
    /// Registers keyboard accelerators on the given NavigationView.
    /// </summary>
    /// <param name="navView">The NavigationView to attach accelerators to.</param>
    /// <param name="onNavigate">Called with a screen name (e.g. "exchange", "my_info").</param>
    /// <param name="onBack">Called when Escape is pressed.</param>
    /// <param name="onQuit">Called when Ctrl+Q is pressed.</param>
    public static void Register(
        NavigationView navView,
        Action<string> onNavigate,
        Action onBack,
        Action onQuit)
    {
        // Ctrl+E → Exchange
        AddAccelerator(navView, VirtualKey.E, VirtualKeyModifiers.Control,
            () => onNavigate("exchange"));

        // Ctrl+1 → My Info
        AddAccelerator(navView, VirtualKey.Number1, VirtualKeyModifiers.Control,
            () => onNavigate("my_info"));

        // Ctrl+2 → Contacts
        AddAccelerator(navView, VirtualKey.Number2, VirtualKeyModifiers.Control,
            () => onNavigate("contacts"));

        // Ctrl+3 → Settings
        AddAccelerator(navView, VirtualKey.Number3, VirtualKeyModifiers.Control,
            () => onNavigate("settings"));

        // Ctrl+Q → Quit
        AddAccelerator(navView, VirtualKey.Q, VirtualKeyModifiers.Control,
            onQuit);

        // Escape → Back
        AddAccelerator(navView, VirtualKey.Escape, VirtualKeyModifiers.None,
            onBack);
    }

    private static void AddAccelerator(
        NavigationView navView,
        VirtualKey key,
        VirtualKeyModifiers modifiers,
        Action action)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers,
        };

        accelerator.Invoked += (_, args) =>
        {
            action();
            args.Handled = true;
        };

        navView.KeyboardAccelerators.Add(accelerator);
    }
}
