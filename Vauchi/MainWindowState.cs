// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi;

public sealed partial class MainWindow
{
    private void SaveWindowState()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (AppWindow is not { } window)
                return;
            settings.Values["WindowWidth"] = window.Size.Width;
            settings.Values["WindowHeight"] = window.Size.Height;
            settings.Values["WindowX"] = window.Position.X;
            settings.Values["WindowY"] = window.Position.Y;
        }
        catch
        {
        }
    }

    private void RestoreWindowState()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (settings.Values["WindowWidth"] is int width
                && settings.Values["WindowHeight"] is int height
                && width >= 720
                && height >= 480)
            {
                AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            }
            if (settings.Values["WindowX"] is int x
                && settings.Values["WindowY"] is int y
                && x >= 0
                && y >= 0)
            {
                AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }
        }
        catch
        {
        }
    }
}
