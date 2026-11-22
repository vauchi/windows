// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vauchi.Contract;
using Vauchi.CoreUI;
using Vauchi.Hardware;
using Vauchi.WindowsHw;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// The app's wiring between Core's batches and the hardware shell
/// (ADR-077): the commands in a batch's <c>hardware</c> field reach the
/// shell, and what the shell observes reaches Core as contract events.
/// </summary>
public class HardwareBridgeTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(20);
    private const string Service = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
    private const string Scan = "{\"BleStartScanning\":{\"service_uuid\":\"" + Service + "\"}}";

    /// <summary>A discovery backend that records what it was asked and reports what the test hands it.</summary>
    private sealed class FakeDiscovery : IDiscovery
    {
        private readonly TaskCompletionSource<EventSink> _scanning = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentQueue<string> Scans { get; } = new();

        public Task<EventSink> Scanning => _scanning.Task.WaitAsync(Limit);

        public async Task ScanAsync(string serviceUuid, EventSink events, CancellationToken stop)
        {
            Scans.Enqueue(serviceUuid);
            _scanning.TrySetResult(events);
            await Task.Delay(Timeout.Infinite, stop);
        }
    }

    private sealed class Reports
    {
        private readonly SemaphoreSlim _arrived = new(0);
        private readonly ConcurrentQueue<string> _reported = new();

        public void Take(string eventJson)
        {
            _reported.Enqueue(eventJson);
            _arrived.Release();
        }

        public async Task<string> Next()
        {
            Assert.True(await _arrived.WaitAsync(Limit), "no event reached Core");
            Assert.True(_reported.TryDequeue(out string? next));
            return next!;
        }
    }

    private sealed class FixedEngine(string? initial, string? answer) : IEngineLink
    {
        public List<string> Dispatched { get; } = new();

        public string? InitialCommands() => initial;

        public string? Dispatch(string eventJson)
        {
            Dispatched.Add(eventJson);
            return answer;
        }
    }

    private static HardwareShell ShellOver(FakeDiscovery discovery) =>
        new(new Backends { Discovery = discovery }, new DeviceCapabilities { Platform = Vauchi.Contract.Platform.Desktop });

    private static string Batch(string hardware) => "{\"commands\":[],\"hardware\":" + hardware + "}";

    [Fact]
    public async Task TheHardwareFieldOfABatchReachesTheShell()
    {
        var discovery = new FakeDiscovery();
        await using HardwareShell shell = ShellOver(discovery);
        var bridge = new HardwareBridge(shell, _ => { }, _ => { });

        bridge.Route(Batch("[" + Scan + "]"));
        await discovery.Scanning;

        Assert.Equal(new[] { Service }, discovery.Scans.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"commands\":[]}")]
    [InlineData("{\"commands\":[],\"hardware\":null}")]
    [InlineData("{\"commands\":[],\"hardware\":{}}")]
    [InlineData("{\"commands\":[],\"hardware\":\"BleStopScanning\"}")]
    [InlineData("{\"commands\":[" + Scan + "]}")]
    public async Task ABatchWithNoHardwareListAsksNothingOfTheShell(string? batch)
    {
        var discovery = new FakeDiscovery();
        await using HardwareShell shell = ShellOver(discovery);
        var bridge = new HardwareBridge(shell, _ => { }, _ => { });

        bridge.Route(batch);
        bridge.Route(Batch("[" + Scan + "]"));
        await discovery.Scanning;

        Assert.Equal(new[] { Service }, discovery.Scans.ToArray());
    }

    [Theory]
    [InlineData("{\"NoSuchCommand\":{}}")]
    [InlineData("{\"BleStartScanning\":{}}")]
    [InlineData("{\"BleStartScanning\":{\"service_uuid\":7}}")]
    [InlineData("{\"PostNotification\":{\"notification\":{\"title\":\"t\",\"body\":\"b\"}}}")]
    [InlineData("\"AudioStop\"")]
    [InlineData("\"RequestBiometricUnlock\"")]
    public async Task WhatTheShellDoesNotRunGoesToTheAppsOwnEffectsAndTheRestStillRun(string entry)
    {
        var discovery = new FakeDiscovery();
        await using HardwareShell shell = ShellOver(discovery);
        var left = new List<string>();
        var bridge = new HardwareBridge(shell, _ => { }, command => left.Add(command.GetRawText()));

        bridge.Route(Batch("[" + entry + "," + Scan + "]"));
        await discovery.Scanning;

        Assert.Equal(new[] { entry }, left);
        Assert.Equal(new[] { Service }, discovery.Scans.ToArray());
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("true")]
    public async Task AnEntryThatHasNoCommandsShapeGoesNowhere(string entry)
    {
        var discovery = new FakeDiscovery();
        await using HardwareShell shell = ShellOver(discovery);
        var left = new List<string>();
        var bridge = new HardwareBridge(shell, _ => { }, command => left.Add(command.GetRawText()));

        bridge.Route(Batch("[" + entry + "," + Scan + "]"));
        await discovery.Scanning;

        Assert.Empty(left);
        Assert.Equal(new[] { Service }, discovery.Scans.ToArray());
    }

    [Fact]
    public async Task ACommandTheShellRunsIsNotAlsoHandedToTheApp()
    {
        var discovery = new FakeDiscovery();
        await using HardwareShell shell = ShellOver(discovery);
        var left = new List<string>();
        var bridge = new HardwareBridge(shell, _ => { }, command => left.Add(command.GetRawText()));

        bridge.Route(Batch("[" + Scan + ",\"BleStopScanning\",{\"ScheduleWakeup\":{\"earliest_secs\":30,\"deadline_secs\":90,\"min_interval_secs\":30,\"earliest_millis\":null,\"delay_millis\":30000}}]"));
        await discovery.Scanning;

        Assert.Empty(left);
    }

    [Fact]
    public async Task WhatIsLeftToTheAppOutlivesTheBatchItCameIn()
    {
        await using HardwareShell shell = ShellOver(new FakeDiscovery());
        var kept = new List<System.Text.Json.JsonElement>();
        var bridge = new HardwareBridge(shell, _ => { }, kept.Add);

        bridge.Route(Batch("[\"AudioStop\"]"));

        Assert.Equal("AudioStop", Assert.Single(kept).GetString());
    }

    [Fact]
    public async Task WhatTheShellObservesReachesCoreAsTheContractsEvent()
    {
        var discovery = new FakeDiscovery();
        await using HardwareShell shell = ShellOver(discovery);
        var reports = new Reports();
        var bridge = new HardwareBridge(shell, reports.Take, _ => { });
        using var stop = new CancellationTokenSource();
        Task running = bridge.RunAsync(stop.Token);
        bridge.Route(Batch("[" + Scan + "]"));
        Event observed = BleObservations.Observed(207374182715392, -40, new[] { Guid.Parse(Service) });

        await (await discovery.Scanning).SendAsync(observed, CancellationToken.None);

        Assert.Equal(Encoding.UTF8.GetString(ContractJson.EncodeEvent(observed)), await reports.Next());
        stop.Cancel();
        await running.WaitAsync(Limit);
    }

    [Fact]
    public async Task TheCapabilityReportIsTheShellsOwn()
    {
        await using HardwareShell shell = ShellOver(new FakeDiscovery());
        var bridge = new HardwareBridge(shell, _ => { }, _ => { });

        Assert.Equal(Encoding.UTF8.GetString(ContractJson.EncodeEvent(shell.Report())), bridge.Capabilities);
    }

    [Fact]
    public async Task TheEngineLinkHandsTheDisplayCoresAnswerUnchangedAndRoutesItsHardware()
    {
        var discovery = new FakeDiscovery();
        await using HardwareShell shell = ShellOver(discovery);
        string answer = Batch("[" + Scan + "]");
        var engine = new FixedEngine(initial: "{\"commands\":[]}", answer);
        var link = new HardwareEngineLink(engine) { Hardware = new HardwareBridge(shell, _ => { }, _ => { }) };

        string? initial = link.InitialCommands();
        string? dispatched = link.Dispatch("\"AppForegrounded\"");
        await discovery.Scanning;

        Assert.Equal("{\"commands\":[]}", initial);
        Assert.Equal(answer, dispatched);
        Assert.Equal(new[] { "\"AppForegrounded\"" }, engine.Dispatched);
        Assert.Equal(new[] { Service }, discovery.Scans.ToArray());
    }

    [Fact]
    public void BeforeTheShellIsUpTheDisplayStillGetsCoresAnswer()
    {
        string answer = Batch("[" + Scan + "]");
        var link = new HardwareEngineLink(new FixedEngine(initial: answer, answer));

        Assert.Equal(answer, link.InitialCommands());
        Assert.Equal(answer, link.Dispatch("\"AppForegrounded\""));
    }

    [Fact]
    public async Task AnEngineThatAnswersNothingAsksNothingOfTheShell()
    {
        await using HardwareShell shell = ShellOver(new FakeDiscovery());
        var link = new HardwareEngineLink(new FixedEngine(initial: null, answer: null))
        {
            Hardware = new HardwareBridge(shell, _ => { }, _ => { }),
        };

        Assert.Null(link.InitialCommands());
        Assert.Null(link.Dispatch("\"AppForegrounded\""));
    }
}
