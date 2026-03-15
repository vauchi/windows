// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

public class ExchangeCommandParserTests
{
    // ── QR ──

    [Fact]
    public void Parses_QrDisplay()
    {
        var cmd = ParseSingle("""{"QrDisplay":{"data":"vauchi://abc"}}""");
        Assert.Equal(ExchangeCommandKind.QrDisplay, cmd.Kind);
        Assert.Equal("vauchi://abc", cmd.GetString("data"));
    }

    [Fact]
    public void Parses_QrRequestScan()
    {
        var cmd = ParseSingle("\"QrRequestScan\"");
        Assert.Equal(ExchangeCommandKind.QrRequestScan, cmd.Kind);
    }

    // ── BLE ──

    [Fact]
    public void Parses_BleStartAdvertising()
    {
        var cmd = ParseSingle("""{"BleStartAdvertising":{"service_uuid":"0000abcd-0000-1000-8000-00805f9b34fb","payload":[1,2,3]}}""");
        Assert.Equal(ExchangeCommandKind.BleStartAdvertising, cmd.Kind);
        Assert.Equal("0000abcd-0000-1000-8000-00805f9b34fb", cmd.GetString("service_uuid"));
        Assert.Equal(new byte[] { 1, 2, 3 }, cmd.GetBytes("payload"));
    }

    [Fact]
    public void Parses_BleStartScanning()
    {
        var cmd = ParseSingle("""{"BleStartScanning":{"service_uuid":"0000abcd"}}""");
        Assert.Equal(ExchangeCommandKind.BleStartScanning, cmd.Kind);
        Assert.Equal("0000abcd", cmd.GetString("service_uuid"));
    }

    [Fact]
    public void Parses_BleConnect()
    {
        var cmd = ParseSingle("""{"BleConnect":{"device_id":"AA:BB:CC:DD"}}""");
        Assert.Equal(ExchangeCommandKind.BleConnect, cmd.Kind);
        Assert.Equal("AA:BB:CC:DD", cmd.GetString("device_id"));
    }

    [Fact]
    public void Parses_BleWriteCharacteristic()
    {
        var cmd = ParseSingle("""{"BleWriteCharacteristic":{"uuid":"char-uuid","data":[10,20]}}""");
        Assert.Equal(ExchangeCommandKind.BleWriteCharacteristic, cmd.Kind);
        Assert.Equal("char-uuid", cmd.GetString("uuid"));
        Assert.Equal(new byte[] { 10, 20 }, cmd.GetBytes("data"));
    }

    [Fact]
    public void Parses_BleReadCharacteristic()
    {
        var cmd = ParseSingle("""{"BleReadCharacteristic":{"uuid":"char-uuid"}}""");
        Assert.Equal(ExchangeCommandKind.BleReadCharacteristic, cmd.Kind);
        Assert.Equal("char-uuid", cmd.GetString("uuid"));
    }

    [Fact]
    public void Parses_BleDisconnect()
    {
        var cmd = ParseSingle("\"BleDisconnect\"");
        Assert.Equal(ExchangeCommandKind.BleDisconnect, cmd.Kind);
    }

    // ── NFC ──

    [Fact]
    public void Parses_NfcActivate()
    {
        var cmd = ParseSingle("""{"NfcActivate":{"payload":[255,0,128]}}""");
        Assert.Equal(ExchangeCommandKind.NfcActivate, cmd.Kind);
        Assert.Equal(new byte[] { 255, 0, 128 }, cmd.GetBytes("payload"));
    }

    [Fact]
    public void Parses_NfcDeactivate()
    {
        var cmd = ParseSingle("\"NfcDeactivate\"");
        Assert.Equal(ExchangeCommandKind.NfcDeactivate, cmd.Kind);
    }

    // ── Audio ──

    [Fact]
    public void Parses_AudioEmitChallenge()
    {
        var cmd = ParseSingle("""{"AudioEmitChallenge":{"data":[42]}}""");
        Assert.Equal(ExchangeCommandKind.AudioEmitChallenge, cmd.Kind);
        Assert.Equal(new byte[] { 42 }, cmd.GetBytes("data"));
    }

    [Fact]
    public void Parses_AudioListenForResponse()
    {
        var cmd = ParseSingle("""{"AudioListenForResponse":{"timeout_ms":5000}}""");
        Assert.Equal(ExchangeCommandKind.AudioListenForResponse, cmd.Kind);
        Assert.Equal(5000L, cmd.GetLong("timeout_ms"));
    }

    [Fact]
    public void Parses_AudioStop()
    {
        var cmd = ParseSingle("\"AudioStop\"");
        Assert.Equal(ExchangeCommandKind.AudioStop, cmd.Kind);
    }

    // ── Forward compat ──

    [Fact]
    public void Unknown_DataVariant_Returns_Unknown()
    {
        var cmd = ParseSingle("""{"FutureThing":{"x":1}}""");
        Assert.Equal(ExchangeCommandKind.Unknown, cmd.Kind);
    }

    [Fact]
    public void Unknown_UnitVariant_Returns_Unknown()
    {
        var cmd = ParseSingle("\"FutureUnit\"");
        Assert.Equal(ExchangeCommandKind.Unknown, cmd.Kind);
    }

    // ── Array parsing (from ActionResult) ──

    [Fact]
    public void ParseAll_Handles_Mixed_Array()
    {
        string json = """[{"QrDisplay":{"data":"abc"}},"QrRequestScan","BleDisconnect"]""";
        var cmds = ExchangeCommandParser.ParseAll(json);
        Assert.Equal(3, cmds.Length);
        Assert.Equal(ExchangeCommandKind.QrDisplay, cmds[0].Kind);
        Assert.Equal(ExchangeCommandKind.QrRequestScan, cmds[1].Kind);
        Assert.Equal(ExchangeCommandKind.BleDisconnect, cmds[2].Kind);
    }

    [Fact]
    public void ParseAll_Empty_Array()
    {
        var cmds = ExchangeCommandParser.ParseAll("[]");
        Assert.Empty(cmds);
    }

    [Fact]
    public void ParseFromActionResult_Extracts_Commands()
    {
        string resultJson = """{"ExchangeCommands":{"commands":[{"QrDisplay":{"data":"x"}},"QrRequestScan"]}}""";
        var cmds = ExchangeCommandParser.ParseFromActionResult(resultJson);
        Assert.Equal(2, cmds.Length);
        Assert.Equal(ExchangeCommandKind.QrDisplay, cmds[0].Kind);
        Assert.Equal(ExchangeCommandKind.QrRequestScan, cmds[1].Kind);
    }

    // ── Helper ──

    private static ExchangeCommand ParseSingle(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return ExchangeCommandParser.Parse(doc.RootElement);
    }
}
