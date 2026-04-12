// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vauchi.Helpers;

namespace Vauchi.Services;

/// <summary>
/// TCP client for USB cable exchange (ADR-031).
/// Implements VXCH wire framing protocol.
/// </summary>
internal sealed class DirectSendService
{
    public const int DefaultPort = 19283;
    private static readonly byte[] Magic = "VXCH"u8.ToArray();
    private const byte ProtocolVersion = 1;
    private const int MaxPayload = 65536;
    private const int TimeoutMs = 10000;

    public event Action<string>? OnPayloadReceived;
    public event Action<string, string>? OnError;

    public async Task ExchangeAsync(string address, byte[] payload, bool isInitiator)
    {
        try
        {
            var parts = address.Split(':');
            var host = parts[0];
            var port = parts.Length > 1 ? int.Parse(parts[1]) : DefaultPort;

            using var client = new TcpClient();
            client.ReceiveTimeout = TimeoutMs;
            client.SendTimeout = TimeoutMs;
            await client.ConnectAsync(host, port);

            var stream = client.GetStream();
            byte[] theirPayload;

            if (isInitiator)
            {
                await SendVxch(stream, payload);
                theirPayload = await RecvVxch(stream);
            }
            else
            {
                theirPayload = await RecvVxch(stream);
                await SendVxch(stream, payload);
            }

            string eventJson = ExchangeHardwareEventJson.DirectPayloadReceived(theirPayload);
            OnPayloadReceived?.Invoke(eventJson);
        }
        catch (Exception ex)
        {
            OnError?.Invoke("USB", ex.Message);
        }
    }

    private static async Task SendVxch(NetworkStream stream, byte[] payload)
    {
        if (payload.Length == 0) throw new InvalidOperationException("empty payload");
        var header = new byte[9];
        Buffer.BlockCopy(Magic, 0, header, 0, 4);
        header[4] = ProtocolVersion;
        var len = BitConverter.GetBytes((uint)payload.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(len);
        Buffer.BlockCopy(len, 0, header, 5, 4);
        await stream.WriteAsync(header);
        await stream.WriteAsync(payload);
        await stream.FlushAsync();
    }

    private static async Task<byte[]> RecvVxch(NetworkStream stream)
    {
        var header = await ReadExact(stream, 9);
        if (header[0] != Magic[0] || header[1] != Magic[1] ||
            header[2] != Magic[2] || header[3] != Magic[3])
            throw new InvalidOperationException("invalid VXCH magic");
        if (header[4] != ProtocolVersion)
            throw new InvalidOperationException("unsupported version");
        var lenBytes = new byte[4];
        Buffer.BlockCopy(header, 5, lenBytes, 0, 4);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        var len = BitConverter.ToUInt32(lenBytes);
        if (len == 0 || len > MaxPayload)
            throw new InvalidOperationException($"invalid length: {len}");
        return await ReadExact(stream, (int)len);
    }

    private static async Task<byte[]> ReadExact(NetworkStream stream, int count)
    {
        var buf = new byte[count];
        var received = 0;
        while (received < count)
        {
            var n = await stream.ReadAsync(buf, received, count - received);
            if (n == 0) throw new InvalidOperationException("connection closed");
            received += n;
        }
        return buf;
    }
}
