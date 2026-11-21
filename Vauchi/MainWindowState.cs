// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Vauchi;

public sealed partial class MainWindow
{
    private const int MinRestorableWidth = 720;
    private const int MinRestorableHeight = 480;

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
            if (settings.Values["WindowWidth"] is not int width
                || settings.Values["WindowHeight"] is not int height
                || width < MinRestorableWidth
                || height < MinRestorableHeight)
            {
                return;
            }
            if (settings.Values["WindowX"] is not int x
                || settings.Values["WindowY"] is not int y)
            {
                AppWindow.Resize(new SizeInt32(width, height));
                return;
            }
            var proposed = new RectInt32(x, y, width, height);
            if (RectIntersectsAnyWorkArea(proposed, CurrentWorkAreas()))
            {
                AppWindow.MoveAndResize(proposed);
                return;
            }
            // Monitor at this origin is no longer connected — restore the
            // size but let the OS place the window on a visible display.
            AppWindow.Resize(new SizeInt32(width, height));
        }
        catch
        {
        }
    }

    private static IEnumerable<RectInt32> CurrentWorkAreas()
    {
        foreach (var area in DisplayArea.FindAll())
            yield return area.WorkArea;
    }

    // Window placement must survive monitors left of or above the primary,
    // which carry negative coordinates on Windows (private#1133). The
    // previous `x >= 0 && y >= 0` guard discarded them wholesale, so a
    // multi-monitor user who closed the app on a secondary display always
    // reopened on the primary. Validate against actual work areas instead.
    internal static bool RectIntersectsAnyWorkArea(
        RectInt32 proposed,
        IEnumerable<RectInt32> workAreas)
    {
        foreach (var area in workAreas)
        {
            if (proposed.X < area.X + area.Width
                && proposed.X + proposed.Width > area.X
                && proposed.Y < area.Y + area.Height
                && proposed.Y + proposed.Height > area.Y)
            {
                return true;
            }
        }
        return false;
    }
}
