// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Threading;
using System.Threading.Tasks;
using Vauchi.Contract;
using Vauchi.WindowsHw;
using Vauchi.WindowsHw.Native;

namespace Vauchi.Hardware;

/// <summary>
/// The hardware shell as this app assembles it (ADR-077, ADR-078): the
/// Windows backends <c>windows-hw</c> ships, on what this machine has.
/// The app owns its lifetime and nothing else about it.
/// </summary>
public sealed class WindowsHardware : IAsyncDisposable
{
    private readonly HardwareShell _shell;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _reporting;

    private WindowsHardware(HardwareShell shell, Action<string> report, Action<System.Text.Json.JsonElement> leftToTheApp)
    {
        _shell = shell;
        Bridge = new HardwareBridge(shell, report, leftToTheApp);
        // Off the caller's thread: the shell's events arrive whenever the OS has one.
        _reporting = Task.Run(() => Bridge.RunAsync(_stop.Token));
    }

    public HardwareBridge Bridge { get; }

    /// <param name="report">Takes one contract <c>Event</c> as JSON, on a thread-pool thread.</param>
    /// <param name="leftToTheApp">Takes one command no backend here runs, on the routing thread.</param>
    public static async Task<WindowsHardware> StartAsync(
        Action<string> report,
        Action<System.Text.Json.JsonElement> leftToTheApp)
    {
        DeviceCapabilities device = await WindowsProbe.ProbeAsync();
        var backends = new Backends
        {
            Discovery = new WinRtDiscovery(),
            Central = new GattCentral(new WinRtGattDialer()),
            Advertising = new GattPeripheral(new WinRtGattServerHost()),
            Nfc = new PcscReader(new WinSCard()),
            Direct = new TcpDirectExchange(),
        };
        return new WindowsHardware(new HardwareShell(backends, device), report, leftToTheApp);
    }

    /// <summary>
    /// Ends the shell, bounded by its shutdown grace. Safe to wait on from
    /// the UI thread: the work runs on the thread pool, so nothing in it
    /// needs the thread that waits.
    /// </summary>
    public void Stop() => Task.Run(StopAsync).GetAwaiter().GetResult();

    public ValueTask DisposeAsync() => new(Task.Run(StopAsync));

    private async Task StopAsync()
    {
        _stop.Cancel();
        await _reporting;
        await _shell.DisposeAsync();
        _stop.Dispose();
    }
}
