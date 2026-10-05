// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

namespace Vauchi.CoreUI;

/// <summary>
/// How long to wait before the next Core-scheduled wakeup
/// (vauchi/private#450). Pure, so <c>WakeupTimingTests</c> can assert the
/// numbers without a <c>DispatcherTimer</c>, which needs a UI thread this
/// test project deliberately has none of.
/// </summary>
public static class WakeupTiming
{
    /// <summary>
    /// Core's <c>earliest_millis</c> when it named one — a live QR
    /// exchange advances its display from this wakeup, and whole seconds
    /// pinned it at one frame per second against a ~300 ms design
    /// (2026-08-18-hover-transfer-stalls-on-the-last-chunk) — else
    /// <c>earliest_secs</c> converted to milliseconds. Either is capped at
    /// <c>deadline_secs</c> so a generous earliest window never outruns
    /// its own deadline.
    /// </summary>
    public static int DelayMillisecondsFor(
        int earliestSecs,
        int deadlineSecs,
        int? earliestMillis)
    {
        int earliest = earliestMillis ?? earliestSecs * 1000;
        return Math.Min(earliest, deadlineSecs * 1000);
    }
}
