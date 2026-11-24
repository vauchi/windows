// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using Vauchi.Hardware;
using Vauchi.WindowsHw.Native;
using Windows.Graphics.Imaging;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// The app hands the display shell's camera panel to the hardware shell
/// under Core's preview id, and takes it back when the viewport goes
/// (ADR-077 amendment 2026-11-01, vauchi/private#772).
/// </summary>
public class CameraPreviewsTests
{
    private sealed class Panel
    {
    }

    private sealed class Output : IPreviewOutput, IDisposable
    {
        public bool Released { get; private set; }

        public void Show(SoftwareBitmap frame)
        {
        }

        public void Dispose() => Released = true;
    }

    private readonly PreviewOutputs _outputs = new();
    private readonly List<Output> _opened = new();

    private CameraPreviews<Panel> Previews() => new(_outputs, _ =>
    {
        var output = new Output();
        _opened.Add(output);
        return output;
    });

    [Fact]
    public void AShownViewportGetsAnOutputTheCameraFindsUnderItsPreviewId()
    {
        using CameraPreviews<Panel> previews = Previews();

        previews.Shown("camera-1", new Panel());

        Assert.True(_outputs.TryGet("camera-1", out IPreviewOutput? found));
        Assert.Same(Assert.Single(_opened), found);
    }

    [Fact]
    public void AGoneViewportsOutputIsDetachedAndReleased()
    {
        using CameraPreviews<Panel> previews = Previews();
        var panel = new Panel();
        previews.Shown("camera-1", panel);

        previews.Gone("camera-1", panel);

        Assert.False(_outputs.TryGet("camera-1", out _));
        Assert.True(Assert.Single(_opened).Released);
    }

    [Fact]
    public void ALateGoneForAReplacedPanelLeavesTheNewOne()
    {
        using CameraPreviews<Panel> previews = Previews();
        var old = new Panel();
        previews.Shown("camera-1", old);
        previews.Shown("camera-1", new Panel());

        previews.Gone("camera-1", old);

        Assert.True(_outputs.TryGet("camera-1", out IPreviewOutput? found));
        Assert.Same(_opened[1], found);
        Assert.True(_opened[0].Released);
        Assert.False(_opened[1].Released);
    }

    [Fact]
    public void ClosingTheAppReleasesEveryOutput()
    {
        CameraPreviews<Panel> previews = Previews();
        previews.Shown("camera-1", new Panel());
        previews.Shown("camera-2", new Panel());

        previews.Dispose();

        Assert.All(_opened, output => Assert.True(output.Released));
        Assert.False(_outputs.TryGet("camera-1", out _));
        Assert.False(_outputs.TryGet("camera-2", out _));
    }
}
