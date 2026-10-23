// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.Platform;
using Windows.Networking.Connectivity;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// The shell reports what the network costs and Core decides whether a
/// "Wi-Fi only" content update may run (vauchi/private#733 D6). What the
/// shell cannot tell is unknown, which Core counts as metered.
/// </summary>
public class NetworkCostTests
{
    [Fact]
    public void AnUnrestrictedConnectionIsUnmetered()
    {
        Assert.Equal("unmetered", NetworkCost.Name(NetworkCostType.Unrestricted, roaming: false, overDataLimit: false));
    }

    [Theory]
    [InlineData(NetworkCostType.Fixed)]
    [InlineData(NetworkCostType.Variable)]
    public void APaidConnectionIsMetered(NetworkCostType costType)
    {
        Assert.Equal("metered", NetworkCost.Name(costType, roaming: false, overDataLimit: false));
    }

    // Windows' guidance: roaming or past the data limit, act as if metered.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void RoamingOrPastTheDataLimitIsMetered(bool roaming, bool overDataLimit)
    {
        Assert.Equal("metered", NetworkCost.Name(NetworkCostType.Unrestricted, roaming, overDataLimit));
    }

    [Fact]
    public void NoConnectionOrAnUnknownCostIsUnknown()
    {
        Assert.Equal("unknown", NetworkCost.Name(null, roaming: false, overDataLimit: false));
        Assert.Equal("unknown", NetworkCost.Name(NetworkCostType.Unknown, roaming: false, overDataLimit: false));
    }

    [Fact]
    public void TheEventIsTheOneCoreDecodes()
    {
        Assert.Equal("""{"NetworkCostChanged":{"cost":"metered"}}""", NetworkCost.EventJson("metered"));
    }
}
