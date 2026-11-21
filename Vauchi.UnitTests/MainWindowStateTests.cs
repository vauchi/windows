// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Windows.Graphics;
using Xunit;

namespace Vauchi.Tests;

public class MainWindowStateTests
{
    private static RectInt32 Rect(int x, int y, int w, int h) => new(x, y, w, h);

    [Fact]
    public void WindowOnPrimaryMonitor_Accepted()
    {
        var primary = new[] { Rect(0, 0, 1920, 1080) };
        Assert.True(MainWindow.RectIntersectsAnyWorkArea(
            Rect(200, 100, 1280, 720), primary));
    }

    [Fact]
    public void WindowOnMonitorLeftOfPrimary_Accepted()
    {
        // Secondary display to the left of (0,0): origin at (-1920, 0).
        var displays = new[]
        {
            Rect(0, 0, 1920, 1080),
            Rect(-1920, 0, 1920, 1080),
        };
        Assert.True(MainWindow.RectIntersectsAnyWorkArea(
            Rect(-1800, 100, 1280, 720), displays));
    }

    [Fact]
    public void WindowOnMonitorAbovePrimary_Accepted()
    {
        // Secondary stacked above primary.
        var displays = new[]
        {
            Rect(0, 0, 1920, 1080),
            Rect(0, -1200, 1920, 1200),
        };
        Assert.True(MainWindow.RectIntersectsAnyWorkArea(
            Rect(300, -1100, 1280, 720), displays));
    }

    [Fact]
    public void WindowOnMonitorBothNegative_Accepted()
    {
        // Private#1133 regression case: monitor origin at (-1286, -2160).
        var displays = new[]
        {
            Rect(0, 0, 1707, 1019),
            Rect(-1286, -2160, 1707, 960),
        };
        Assert.True(MainWindow.RectIntersectsAnyWorkArea(
            Rect(-1200, -2000, 1280, 720), displays));
    }

    [Fact]
    public void WindowOnDisconnectedMonitor_Rejected()
    {
        // Saved at (-1800, 100) but the monitor at (-1920, 0) is gone.
        var displays = new[] { Rect(0, 0, 1920, 1080) };
        Assert.False(MainWindow.RectIntersectsAnyWorkArea(
            Rect(-1800, 100, 1280, 720), displays));
    }

    [Fact]
    public void WindowEntirelyRightOfAllDisplays_Rejected()
    {
        var displays = new[] { Rect(0, 0, 1920, 1080) };
        Assert.False(MainWindow.RectIntersectsAnyWorkArea(
            Rect(2500, 100, 1280, 720), displays));
    }

    [Fact]
    public void WindowPartiallyOnScreen_Accepted()
    {
        // Half the window is off the right edge; the other half is visible.
        var displays = new[] { Rect(0, 0, 1920, 1080) };
        Assert.True(MainWindow.RectIntersectsAnyWorkArea(
            Rect(1500, 100, 1280, 720), displays));
    }

    [Fact]
    public void WindowTouchingEdgeOnly_Rejected()
    {
        // Window right edge aligns with display left edge (x + width == area.X):
        // zero intersection area, so not visible.
        var displays = new[] { Rect(0, 0, 1920, 1080) };
        Assert.False(MainWindow.RectIntersectsAnyWorkArea(
            Rect(-1280, 100, 1280, 720), displays));
    }

    [Fact]
    public void NoDisplaysConnected_Rejected()
    {
        Assert.False(MainWindow.RectIntersectsAnyWorkArea(
            Rect(0, 0, 1280, 720), System.Array.Empty<RectInt32>()));
    }
}
