// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Vauchi.CoreUI;

/// <summary>
/// Headless replay of Core's screen catalog through the real
/// <see cref="PresentationHost"/>: every entry is shown in the main window
/// at 1440x900 and captured to <c>&lt;code_id&gt;.png</c> plus a
/// <c>.dark.png</c> variant. Ends the process with exit code 0 when at
/// least one screen rendered, 1 when none did and 2 on a setup failure.
/// </summary>
public static class ScreenCatalogRenderer
{
    private const int ClientWidth = 1440;
    private const int ClientHeight = 900;
    private const string LogFileName = "render-catalog.log";

    public static async Task RunAsync(MainWindow window, string catalogPath, string outputDirectory)
    {
        StreamWriter? log = null;
        int exitCode = 2;
        try
        {
            outputDirectory = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(outputDirectory);
            log = new StreamWriter(Path.Combine(outputDirectory, LogFileName)) { AutoFlush = true };
            IReadOnlyList<ScreenCatalogEntry> entries =
                ScreenCatalog.Parse(File.ReadAllText(catalogPath));
            Log(log, $"catalog {catalogPath}: {entries.Count} screens");

            window.AppWindow.ResizeClient(new SizeInt32(ClientWidth, ClientHeight));
            var root = (FrameworkElement)window.Content;
            int rendered = 0;
            foreach (ScreenCatalogEntry entry in entries)
            {
                if (entry.Error is { } error)
                {
                    Log(log, $"skip {entry.CodeId}: {error}");
                    continue;
                }
                string lightPath = Path.Combine(outputDirectory, entry.FileStem + ".png");
                string darkPath = Path.Combine(outputDirectory, entry.FileStem + ".dark.png");
                await CaptureAsync(window, root, entry, ElementTheme.Light, lightPath, log);
                await CaptureAsync(window, root, entry, ElementTheme.Dark, darkPath, log);
                rendered++;
            }
            Log(log, $"rendered {rendered} of {entries.Count} screens into {outputDirectory}");
            exitCode = rendered > 0 ? 0 : 1;
        }
        catch (Exception exception)
        {
            Log(log, $"render failed: {exception}");
        }
        finally
        {
            log?.Dispose();
        }
        Environment.Exit(exitCode);
    }

    private static async Task CaptureAsync(
        MainWindow window,
        FrameworkElement root,
        ScreenCatalogEntry entry,
        ElementTheme theme,
        string path,
        StreamWriter log)
    {
        root.RequestedTheme = theme;
        window.Host.Present(entry.State);
        await WaitForLayoutAsync(root);

        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(root);
        IBuffer pixels = await bitmap.GetPixelsAsync();
        await WritePngAsync(path, bitmap, pixels, root.XamlRoot.RasterizationScale * 96);
        Log(log, $"wrote {Path.GetFileName(path)} ({bitmap.PixelWidth}x{bitmap.PixelHeight}, \"{entry.SurfaceTitle}\")");
    }

    /// <summary>
    /// Two low-priority dispatcher turns let the NavigationView and the
    /// surface controls apply their templates and measure before the
    /// compositor frame RenderTargetBitmap reads.
    /// </summary>
    private static async Task WaitForLayoutAsync(FrameworkElement root)
    {
        root.UpdateLayout();
        for (int turn = 0; turn < 2; turn++)
        {
            var completion = new TaskCompletionSource();
            root.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                () => completion.SetResult());
            await completion.Task;
        }
    }

    private static async Task WritePngAsync(
        string path,
        RenderTargetBitmap bitmap,
        IBuffer pixels,
        double dpi)
    {
        using var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        using IRandomAccessStream stream = file.AsRandomAccessStream();
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,
            (uint)bitmap.PixelHeight,
            dpi,
            dpi,
            pixels.ToArray());
        await encoder.FlushAsync();
    }

    private static void Log(StreamWriter? log, string message)
    {
        string line = $"[render-catalog] {message}";
        log?.WriteLine(line);
        Console.WriteLine(line);
        System.Diagnostics.Debug.WriteLine(line);
    }
}
