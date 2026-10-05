// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// <c>ArmWakeupTimer</c> read only <c>earliest_secs</c> and
/// <c>min_interval_secs</c>, so a live QR exchange asking for a wake every
/// ~100 ms via <c>earliest_millis</c> got one at best once a second
/// (vauchi/private#450, as on iOS/macOS).
/// </summary>
public class WakeupTimingTests
{
    [Fact]
    public void AnEarliestMillisOverridesTheWholeSecondFallback()
    {
        Assert.Equal(
            100,
            WakeupTiming.DelayMillisecondsFor(earliestSecs: 5, deadlineSecs: 30, earliestMillis: 100));
    }

    [Fact]
    public void AnAbsentEarliestMillisFallsBackToWholeSeconds()
    {
        Assert.Equal(
            5000,
            WakeupTiming.DelayMillisecondsFor(earliestSecs: 5, deadlineSecs: 30, earliestMillis: null));
    }

    [Fact]
    public void TheDelayIsCappedAtTheDeadline()
    {
        Assert.Equal(
            2000,
            WakeupTiming.DelayMillisecondsFor(earliestSecs: 0, deadlineSecs: 2, earliestMillis: 5000));
        Assert.Equal(
            3000,
            WakeupTiming.DelayMillisecondsFor(earliestSecs: 10, deadlineSecs: 3, earliestMillis: null));
    }

    [Fact]
    public void AZeroEverythingDelaysByZero()
    {
        Assert.Equal(
            0,
            WakeupTiming.DelayMillisecondsFor(earliestSecs: 0, deadlineSecs: 0, earliestMillis: null));
    }
}
