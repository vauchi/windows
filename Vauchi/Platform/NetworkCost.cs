// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Windows.Networking.Connectivity;

namespace Vauchi.Platform;

/// <summary>
/// Reports what the current network costs, so Core can hold "Wi-Fi only"
/// content updates back on a metered one (vauchi/private#733 D6). Core
/// decides; the shell only observes.
/// </summary>
public static class NetworkCost
{
    public static string Name(NetworkCostType? costType, bool roaming, bool overDataLimit) => costType switch
    {
        null or NetworkCostType.Unknown => "unknown",
        NetworkCostType.Unrestricted when !roaming && !overDataLimit => "unmetered",
        _ => "metered",
    };

    public static string EventJson(string cost) =>
        JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, string>>
        {
            ["NetworkCostChanged"] = new() { ["cost"] = cost },
        });

    /// <summary>
    /// Hands <paramref name="dispatch"/> the cost now and again whenever the
    /// network changes, on <paramref name="queue"/>, since Windows raises the
    /// change on a background thread.
    /// </summary>
    public static void Watch(Action<string> dispatch, DispatcherQueue queue)
    {
        dispatch(CurrentEventJson());
        NetworkInformation.NetworkStatusChanged += _ =>
            queue.TryEnqueue(() => dispatch(CurrentEventJson()));
    }

    private static string CurrentEventJson()
    {
        ConnectionCost? cost = NetworkInformation.GetInternetConnectionProfile()?.GetConnectionCost();
        return EventJson(Name(cost?.NetworkCostType, cost?.Roaming ?? false, cost?.OverDataLimit ?? false));
    }
}
