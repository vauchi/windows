// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml.Controls;
using Vauchi.WindowsHw.Native;
using Vauchi.WindowsHw.Preview;
using Windows.Graphics.Imaging;

namespace Vauchi.Hardware;

/// <summary>
/// The hardware shell's preview drawn on the display shell's camera panel
/// (ADR-077 amendment 2026-11-01). Made and released on the UI thread;
/// frames arrive on the camera's.
/// </summary>
public sealed class PanelPreview : IPreviewOutput, IDisposable
{
    private readonly PanelHost _host;
    private readonly SwapChainPreview _preview;

    public PanelPreview(SwapChainPanel panel)
    {
        _host = new PanelHost(panel);
        _preview = new SwapChainPreview(_host);
    }

    public void Show(SoftwareBitmap frame) => _preview.Show(frame);

    public void Dispose()
    {
        _host.Clear();
        _preview.Dispose();
    }

    private sealed class PanelHost : ISwapChainHost
    {
        // ISwapChainPanelNative, microsoft.ui.xaml.media.dxinterop.h: WinUI's
        // projection has no managed SetSwapChain.
        private static readonly Guid SwapChainPanelNative = new("63aad0b8-7c24-40ff-85a8-640d944cc325");

        private readonly SwapChainPanel _panel;
        private double _width;
        private double _height;

        public PanelHost(SwapChainPanel panel)
        {
            _panel = panel;
            Remember(panel.ActualWidth, panel.ActualHeight);
            panel.SizeChanged += (_, size) => Remember(size.NewSize.Width, size.NewSize.Height);
        }

        public (double Width, double Height) Size => (Volatile.Read(ref _width), Volatile.Read(ref _height));

        public void Attach(IntPtr swapChain)
        {
            // Held until the UI thread sets it: the preview may be released first.
            Marshal.AddRef(swapChain);
            bool queued = _panel.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    Set(swapChain);
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Vauchi] Camera preview failed: {exception.GetType().Name}");
                }
                finally
                {
                    Marshal.Release(swapChain);
                }
            });
            if (!queued)
                Marshal.Release(swapChain);
        }

        public void Clear()
        {
            try
            {
                Set(IntPtr.Zero);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Vauchi] Camera preview failed to clear: {exception.GetType().Name}");
            }
        }

        private void Remember(double width, double height)
        {
            Volatile.Write(ref _width, width);
            Volatile.Write(ref _height, height);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetSwapChainCall(IntPtr self, IntPtr swapChain);

        private void Set(IntPtr swapChain)
        {
            IntPtr panel = ((WinRT.IWinRTObject)_panel).NativeObject.ThisPtr;
            Guid iid = SwapChainPanelNative;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(panel, ref iid, out IntPtr native));
            try
            {
                // SetSwapChain follows IUnknown's three methods.
                IntPtr method = Marshal.ReadIntPtr(Marshal.ReadIntPtr(native), 3 * IntPtr.Size);
                var setSwapChain = Marshal.GetDelegateForFunctionPointer<SetSwapChainCall>(method);
                Marshal.ThrowExceptionForHR(setSwapChain(native, swapChain));
            }
            finally
            {
                Marshal.Release(native);
            }
        }
    }
}
