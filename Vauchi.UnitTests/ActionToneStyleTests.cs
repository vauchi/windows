// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// <c>ActionButton</c> compared the wire "tone" string against
/// <c>"destructive"</c> inline, so a "serious" tone (Recovery, sign-out)
/// rendered identically to a plain action instead of getting a treatment
/// of its own. This pins the pure mapping the render code now defers to.
/// </summary>
public class ActionToneStyleTests
{
    [Fact]
    public void DestructiveToneMapsToDestructive()
    {
        Assert.Equal(ActionTone.Destructive, ActionToneStyle.From("destructive"));
    }

    [Fact]
    public void SeriousToneMapsToSerious()
    {
        Assert.Equal(ActionTone.Serious, ActionToneStyle.From("serious"));
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Destructive")]
    [InlineData("not_a_real_tone")]
    public void EverythingElseMapsToStandard(string? tone)
    {
        Assert.Equal(ActionTone.Standard, ActionToneStyle.From(tone));
    }
}
