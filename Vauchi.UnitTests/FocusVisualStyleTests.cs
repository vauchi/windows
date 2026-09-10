// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// Pins the hand-maintained mirror of <c>tokens.json</c>'s
/// <c>focus.ring_width</c> / <c>ring_offset</c> — see the remark on
/// <c>FocusVisualStyle</c> for why it is hand-maintained rather than
/// generated. Same class of drift
/// <c>ThemeColorsTests.AvatarFallbackMatchesItsHexConstant</c> guards for
/// the avatar-fallback color.
/// </summary>
public class FocusVisualStyleTests
{
    [Fact]
    public void RingWidthMatchesTokensJson()
    {
        Assert.Equal(3, FocusVisualStyle.RingWidth);
    }

    [Fact]
    public void RingOffsetMatchesTokensJson()
    {
        Assert.Equal(2, FocusVisualStyle.RingOffset);
    }
}
