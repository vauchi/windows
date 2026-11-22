// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vauchi.Contract;
using Vauchi.CoreUI;
using Vauchi.WindowsHw;

namespace Vauchi.Hardware;

/// <summary>
/// The app's wiring between Core and the hardware shell (ADR-077). Core
/// puts the hardware shell's commands in a batch's <c>hardware</c> field
/// once the shell has reported in; this hands them to the shell and
/// returns what the shell observes as contract events. A command the
/// shell does not run is the app's own to run, as it was before the shell
/// existed: Core's hardware target also covers notifications, stored
/// secrets and other effects no port of <c>windows-hw</c> has yet. A
/// command the shell refuses for any other reason is left unanswered, as
/// <c>linux-hw</c>'s host leaves it.
/// </summary>
public sealed class HardwareBridge
{
    private readonly HardwareShell _shell;
    private readonly Action<string> _report;
    private readonly Action<JsonElement> _leftToTheApp;

    /// <param name="shell">The hardware shell; its owner disposes it.</param>
    /// <param name="report">
    /// Takes one contract <c>Event</c> as JSON for Core. Called from the
    /// thread <see cref="RunAsync"/> runs on; the app marshals.
    /// </param>
    /// <param name="leftToTheApp">
    /// Takes one command the shell does not run, on the thread
    /// <see cref="Route"/> is called on. The element outlives the batch.
    /// </param>
    public HardwareBridge(HardwareShell shell, Action<string> report, Action<JsonElement> leftToTheApp)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(leftToTheApp);
        _shell = shell;
        _report = report;
        _leftToTheApp = leftToTheApp;
    }

    /// <summary>The shell's <c>HardwareCapabilitiesReported</c>, which the app dispatches first.</summary>
    public string Capabilities => Wire(_shell.Report());

    /// <summary>Hands the batch's hardware commands to the shell, in Core's order.</summary>
    public void Route(string? batchJson)
    {
        if (string.IsNullOrEmpty(batchJson))
        {
            return;
        }
        JsonDocument batch;
        try
        {
            batch = JsonDocument.Parse(batchJson);
        }
        catch (JsonException)
        {
            // The display reports a batch it cannot read; there is no hardware list to find in it.
            return;
        }
        using (batch)
        {
            if (batch.RootElement.ValueKind != JsonValueKind.Object
                || !batch.RootElement.TryGetProperty("hardware", out JsonElement hardware)
                || hardware.ValueKind != JsonValueKind.Array)
            {
                return;
            }
            foreach (JsonElement entry in hardware.EnumerateArray())
            {
                // A command is an object or, with no fields, a string.
                if (entry.ValueKind is not (JsonValueKind.Object or JsonValueKind.String))
                {
                    continue;
                }
                if (!Runs(entry))
                {
                    // Cloned: the app's effects may finish after this batch is released.
                    _leftToTheApp(entry.Clone());
                }
            }
        }
    }

    private bool Runs(JsonElement entry)
    {
        Command command;
        try
        {
            command = ContractJson.DecodeCommand(Encoding.UTF8.GetBytes(entry.GetRawText()));
        }
        catch (ContractJsonException)
        {
            // Not a command of the contract this shell was built on.
            return false;
        }
        return _shell.Execute(command) != ExecuteResult.Unsupported;
    }

    /// <summary>
    /// Reports what the shell observes until <paramref name="stop"/> is
    /// cancelled or the shell closes. Run it on the thread pool: it does not
    /// return to a captured context.
    /// </summary>
    public async Task RunAsync(CancellationToken stop)
    {
        try
        {
            while (await _shell.NextAsync(stop) is { } observation)
            {
                _report(Wire(observation));
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
        }
    }

    private static string Wire(Event observation) => Encoding.UTF8.GetString(ContractJson.EncodeEvent(observation));
}

/// <summary>
/// The engine as the display shell sees it, with the hardware field of
/// every answer routed on the way through. The display gets Core's answer
/// unchanged and reads only what is its own.
/// </summary>
public sealed class HardwareEngineLink(IEngineLink engine) : IEngineLink
{
    /// <summary>
    /// Null until the hardware shell is up. Core sends no hardware
    /// commands before the shell has reported in, so nothing is missed.
    /// </summary>
    public HardwareBridge? Hardware { get; set; }

    public string? InitialCommands() => Routed(engine.InitialCommands());

    public string? Dispatch(string eventJson) => Routed(engine.Dispatch(eventJson));

    private string? Routed(string? batchJson)
    {
        Hardware?.Route(batchJson);
        return batchJson;
    }
}
