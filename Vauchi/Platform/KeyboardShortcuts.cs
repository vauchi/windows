// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Vauchi.Platform;

/// <summary>
/// Registers keyboard shortcuts for screen navigation.
/// Ctrl+E=Exchange, Ctrl+1=My Info, Ctrl+2=Contacts, Ctrl+3=Settings.
/// Ctrl+Q=Quit is handled by the MenuBar accelerator in MainWindow.xaml.
/// </summary>
public class KeyboardShortcuts
{
    public event Action<string>? NavigateRequested;

    public void Register(UIElement root)
    {
        AddAccelerator(root, VirtualKey.E, VirtualKeyModifiers.Control, () => NavigateRequested?.Invoke("exchange"));
        AddAccelerator(root, VirtualKey.Number1, VirtualKeyModifiers.Control, () => NavigateRequested?.Invoke("my_info"));
        AddAccelerator(root, VirtualKey.Number2, VirtualKeyModifiers.Control, () => NavigateRequested?.Invoke("contacts"));
        AddAccelerator(root, VirtualKey.Number3, VirtualKeyModifiers.Control, () => NavigateRequested?.Invoke("settings"));
    }

    private static void AddAccelerator(UIElement target, VirtualKey key, VirtualKeyModifiers modifiers, Action action)
    {
        var accel = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accel.Invoked += (_, args) =>
        {
            action();
            args.Handled = true;
        };
        target.KeyboardAccelerators.Add(accel);
    }
}
