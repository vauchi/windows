// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Vauchi.Helpers;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Vauchi.Platform;

/// <summary>
/// Adapter between ADR-031 BLE exchange commands and WinRT Bluetooth LE APIs.
/// Core emits commands (scan, connect, write characteristic); this service
/// executes them using WinRT and reports events back via the callback.
/// All protocol logic (handshake, key exchange, chunking) lives in core.
/// </summary>
public sealed class BleExchangeService : IDisposable
{
    private readonly Action<string> _onHardwareEvent;
    private BluetoothLEAdvertisementWatcher? _watcher;
    private BluetoothLEAdvertisementPublisher? _publisher;
    private BluetoothLEDevice? _connectedDevice;
    private GattDeviceService? _gattService;
    private string? _targetServiceUuid;
    private bool _disposed;

    /// <param name="onHardwareEvent">
    /// Callback that receives ExchangeHardwareEvent JSON strings.
    /// Caller routes these to VauchiNative.AppHandleHardwareEvent.
    /// </param>
    public BleExchangeService(Action<string> onHardwareEvent)
    {
        _onHardwareEvent = onHardwareEvent;
    }

    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Check if Bluetooth LE is available on this machine.
    /// Call once during init; if false, all commands return HardwareUnavailable.
    /// </summary>
    public async System.Threading.Tasks.Task<bool> CheckAvailabilityAsync()
    {
        try
        {
            var adapter = await BluetoothAdapter.GetDefaultAsync();
            IsAvailable = adapter != null && adapter.IsLowEnergySupported;
        }
        catch
        {
            IsAvailable = false;
        }
        return IsAvailable;
    }

    public void StartScanning(string serviceUuid)
    {
        if (!IsAvailable)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareUnavailable("BLE"));
            return;
        }

        _targetServiceUuid = serviceUuid;
        StopScanning();

        _watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active,
        };
        _watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(Guid.Parse(serviceUuid));
        _watcher.Received += OnAdvertisementReceived;
        _watcher.Start();
    }

    public void StartAdvertising(string serviceUuid, byte[] payload)
    {
        if (!IsAvailable)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareUnavailable("BLE"));
            return;
        }

        StopAdvertising();

        _publisher = new BluetoothLEAdvertisementPublisher();
        var data = new BluetoothLEAdvertisementDataSection(0xFF, payload.AsBuffer());
        _publisher.Advertisement.DataSections.Add(data);
        _publisher.Advertisement.ServiceUuids.Add(Guid.Parse(serviceUuid));
        _publisher.Start();
    }

    public async System.Threading.Tasks.Task ConnectAsync(string deviceId)
    {
        if (!IsAvailable)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareUnavailable("BLE"));
            return;
        }

        try
        {
            if (!ulong.TryParse(deviceId, out ulong addr))
            {
                _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", $"Invalid device ID: {deviceId}"));
                return;
            }

            _connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(addr);
            if (_connectedDevice == null)
            {
                _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", "Device not found"));
                return;
            }

            if (_targetServiceUuid != null)
            {
                var servicesResult = await _connectedDevice.GetGattServicesForUuidAsync(
                    Guid.Parse(_targetServiceUuid));
                if (servicesResult.Status == GattCommunicationStatus.Success
                    && servicesResult.Services.Count > 0)
                {
                    _gattService = servicesResult.Services[0];
                }
            }

            _onHardwareEvent(ExchangeHardwareEventJson.BleConnected(deviceId));
        }
        catch (Exception ex)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", ex.Message));
        }
    }

    public async System.Threading.Tasks.Task WriteCharacteristicAsync(string uuid, byte[] data)
    {
        if (_gattService == null)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", "Not connected"));
            return;
        }

        try
        {
            var charsResult = await _gattService.GetCharacteristicsForUuidAsync(Guid.Parse(uuid));
            if (charsResult.Status != GattCommunicationStatus.Success || charsResult.Characteristics.Count == 0)
            {
                _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", $"Characteristic {uuid} not found"));
                return;
            }

            var characteristic = charsResult.Characteristics[0];
            var writeResult = await characteristic.WriteValueAsync(data.AsBuffer());
            if (writeResult != GattCommunicationStatus.Success)
            {
                _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", $"Write failed: {writeResult}"));
            }
        }
        catch (Exception ex)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", ex.Message));
        }
    }

    public async System.Threading.Tasks.Task ReadCharacteristicAsync(string uuid)
    {
        if (_gattService == null)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", "Not connected"));
            return;
        }

        try
        {
            var charsResult = await _gattService.GetCharacteristicsForUuidAsync(Guid.Parse(uuid));
            if (charsResult.Status != GattCommunicationStatus.Success || charsResult.Characteristics.Count == 0)
            {
                _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", $"Characteristic {uuid} not found"));
                return;
            }

            var characteristic = charsResult.Characteristics[0];
            var readResult = await characteristic.ReadValueAsync();
            if (readResult.Status == GattCommunicationStatus.Success)
            {
                byte[] bytes = new byte[readResult.Value.Length];
                using var reader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value);
                reader.ReadBytes(bytes);
                _onHardwareEvent(ExchangeHardwareEventJson.BleCharacteristicRead(uuid, bytes));
            }
            else
            {
                _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", $"Read failed: {readResult.Status}"));
            }
        }
        catch (Exception ex)
        {
            _onHardwareEvent(ExchangeHardwareEventJson.HardwareError("BLE", ex.Message));
        }
    }

    public void Disconnect()
    {
        StopScanning();
        StopAdvertising();
        _gattService?.Dispose();
        _gattService = null;
        _connectedDevice?.Dispose();
        _connectedDevice = null;
        _onHardwareEvent(ExchangeHardwareEventJson.BleDisconnected("user_requested"));
    }

    private void OnAdvertisementReceived(
        BluetoothLEAdvertisementWatcher sender,
        BluetoothLEAdvertisementReceivedEventArgs args)
    {
        byte[] advData = Array.Empty<byte>();
        if (args.Advertisement.DataSections.Count > 0)
        {
            var section = args.Advertisement.DataSections[0];
            advData = new byte[section.Data.Length];
            using var reader = Windows.Storage.Streams.DataReader.FromBuffer(section.Data);
            reader.ReadBytes(advData);
        }

        _onHardwareEvent(ExchangeHardwareEventJson.BleDeviceDiscovered(
            args.BluetoothAddress.ToString(),
            args.RawSignalStrengthInDBm,
            advData));
    }

    private void StopScanning()
    {
        if (_watcher != null)
        {
            _watcher.Received -= OnAdvertisementReceived;
            _watcher.Stop();
            _watcher = null;
        }
    }

    private void StopAdvertising()
    {
        if (_publisher != null)
        {
            _publisher.Stop();
            _publisher = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopScanning();
        StopAdvertising();
        _gattService?.Dispose();
        _connectedDevice?.Dispose();
    }
}
