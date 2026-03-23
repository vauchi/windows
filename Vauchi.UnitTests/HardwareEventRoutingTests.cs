// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

public class HardwareEventRoutingTests
{
    [Theory]
    [InlineData("{\"QrScanned\":{\"data\":\"test\"}}", true)]
    [InlineData("{\"BleDeviceDiscovered\":{\"id\":\"d1\",\"rssi\":-42,\"adv_data\":[]}}", true)]
    [InlineData("{\"BleConnected\":{\"device_id\":\"d1\"}}", true)]
    [InlineData("{\"BleCharacteristicRead\":{\"uuid\":\"abc\",\"data\":[1,2]}}", true)]
    [InlineData("{\"BleCharacteristicNotified\":{\"uuid\":\"abc\",\"data\":[]}}", true)]
    [InlineData("{\"BleDisconnected\":{\"reason\":\"timeout\"}}", true)]
    [InlineData("{\"NfcDataReceived\":{\"data\":[1,2,3]}}", true)]
    [InlineData("{\"AudioResponseReceived\":{\"data\":[4,5,6]}}", true)]
    [InlineData("{\"HardwareError\":{\"transport\":\"BLE\",\"error\":\"fail\"}}", true)]
    [InlineData("{\"HardwareUnavailable\":{\"transport\":\"NFC\"}}", true)]
    [InlineData("{\"ActionPressed\":{\"action_id\":\"back\"}}", false)]
    [InlineData("{\"TextChanged\":{\"component_id\":\"name\",\"value\":\"x\"}}", false)]
    [InlineData("{\"SearchChanged\":{\"component_id\":\"s\",\"query\":\"q\"}}", false)]
    [InlineData("not json", false)]
    [InlineData("", false)]
    [InlineData("{}", false)]
    public void IsHardwareEvent_ClassifiesCorrectly(string json, bool expected)
    {
        Assert.Equal(expected, ActionRouter.IsHardwareEvent(json));
    }
}
