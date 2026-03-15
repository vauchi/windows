// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

public class ExchangeHardwareEventJsonTests
{
    [Fact]
    public void QrScanned_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.QrScanned("vauchi://abc");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("QrScanned");
        Assert.Equal("vauchi://abc", inner.GetProperty("data").GetString());
    }

    [Fact]
    public void BleDeviceDiscovered_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.BleDeviceDiscovered("AA:BB", -42, [1, 2, 3]);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("BleDeviceDiscovered");
        Assert.Equal("AA:BB", inner.GetProperty("id").GetString());
        Assert.Equal(-42, inner.GetProperty("rssi").GetInt16());
        var arr = inner.GetProperty("adv_data");
        Assert.Equal(3, arr.GetArrayLength());
        Assert.Equal(1, arr[0].GetInt32());
    }

    [Fact]
    public void BleConnected_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.BleConnected("device-1");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("BleConnected");
        Assert.Equal("device-1", inner.GetProperty("device_id").GetString());
    }

    [Fact]
    public void BleCharacteristicRead_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.BleCharacteristicRead("uuid-1", [10, 20]);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("BleCharacteristicRead");
        Assert.Equal("uuid-1", inner.GetProperty("uuid").GetString());
        Assert.Equal(2, inner.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public void BleCharacteristicNotified_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.BleCharacteristicNotified("uuid-2", [30]);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("BleCharacteristicNotified");
        Assert.Equal("uuid-2", inner.GetProperty("uuid").GetString());
    }

    [Fact]
    public void BleDisconnected_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.BleDisconnected("timeout");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("BleDisconnected");
        Assert.Equal("timeout", inner.GetProperty("reason").GetString());
    }

    [Fact]
    public void NfcDataReceived_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.NfcDataReceived([0, 255]);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("NfcDataReceived");
        Assert.Equal(2, inner.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public void AudioResponseReceived_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.AudioResponseReceived([42]);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("AudioResponseReceived");
        Assert.Equal(1, inner.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public void HardwareError_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.HardwareError("ble", "adapter not found");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("HardwareError");
        Assert.Equal("ble", inner.GetProperty("transport").GetString());
        Assert.Equal("adapter not found", inner.GetProperty("error").GetString());
    }

    [Fact]
    public void HardwareUnavailable_MatchesSerdeFormat()
    {
        string json = ExchangeHardwareEventJson.HardwareUnavailable("nfc");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("HardwareUnavailable");
        Assert.Equal("nfc", inner.GetProperty("transport").GetString());
    }

    [Fact]
    public void BleDeviceDiscovered_ByteBoundaries_WrittenAsIntegers()
    {
        // CC-14: adversarial byte values — must be JSON int array, NOT Base64
        string json = ExchangeHardwareEventJson.BleDeviceDiscovered("id", 0, [0, 127, 255]);
        Assert.Contains("[0,127,255]", json);  // NOT Base64 "AH//"
    }

    [Fact]
    public void QrScanned_EscapesSpecialCharacters()
    {
        string json = ExchangeHardwareEventJson.QrScanned("data\"with\\special");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("QrScanned");
        Assert.Equal("data\"with\\special", inner.GetProperty("data").GetString());
    }

    [Fact]
    public void AllVariants_ProduceValidJson()
    {
        string[] jsons =
        [
            ExchangeHardwareEventJson.QrScanned("x"),
            ExchangeHardwareEventJson.BleDeviceDiscovered("id", 0, []),
            ExchangeHardwareEventJson.BleConnected("id"),
            ExchangeHardwareEventJson.BleCharacteristicRead("u", []),
            ExchangeHardwareEventJson.BleCharacteristicNotified("u", []),
            ExchangeHardwareEventJson.BleDisconnected("r"),
            ExchangeHardwareEventJson.NfcDataReceived([]),
            ExchangeHardwareEventJson.AudioResponseReceived([]),
            ExchangeHardwareEventJson.HardwareError("t", "e"),
            ExchangeHardwareEventJson.HardwareUnavailable("t"),
        ];
        foreach (string j in jsons)
        {
            var doc = JsonDocument.Parse(j);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
    }
}
