// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using Vauchi.WindowsHw.Native;

namespace Vauchi.Hardware;

/// <summary>
/// The app's half of the camera preview (ADR-077 amendment 2026-11-01): the
/// display shell shows a panel under Core's preview id, and the app hands
/// the hardware shell an output drawing on it, under the same id. Called on
/// the UI thread.
/// </summary>
/// <typeparam name="TPanel">The display shell's native panel.</typeparam>
public sealed class CameraPreviews<TPanel>(PreviewOutputs outputs, Func<TPanel, IPreviewOutput> open) : IDisposable
    where TPanel : class
{
    private readonly Dictionary<string, (TPanel Panel, IPreviewOutput Output)> _open = new(StringComparer.Ordinal);

    public void Shown(string previewId, TPanel panel)
    {
        if (_open.TryGetValue(previewId, out var previous))
            Close(previewId, previous.Output);
        IPreviewOutput output = open(panel);
        _open[previewId] = (panel, output);
        outputs.Attach(previewId, output);
    }

    public void Gone(string previewId, TPanel panel)
    {
        if (_open.TryGetValue(previewId, out var current) && ReferenceEquals(current.Panel, panel))
            Close(previewId, current.Output);
    }

    public void Dispose()
    {
        foreach ((string previewId, (TPanel _, IPreviewOutput output)) in new List<KeyValuePair<string, (TPanel, IPreviewOutput)>>(_open))
            Close(previewId, output);
    }

    private void Close(string previewId, IPreviewOutput output)
    {
        outputs.Detach(previewId, output);
        _open.Remove(previewId);
        (output as IDisposable)?.Dispose();
    }
}
