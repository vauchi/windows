// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// <c>PresentationSurface</c> inlined its <c>minimum_target_size</c>
/// parsing, defaulted to 44 (Apple's floor), and every call site outside
/// that one class kept its own hardcoded fallback (the navigation overlay
/// used a bare 44 with no token at all). This pins the extracted, 48
/// (Android's floor) default the review's D2 asked for.
/// </summary>
public class TargetSizeTests
{
    [Fact]
    public void DefaultIsFortyEight()
    {
        Assert.Equal(48, TargetSize.Default);
    }

    [Fact]
    public void NoTokensObjectFallsBackToTheDefault()
    {
        using JsonDocument document = JsonDocument.Parse("""{"surface_id":"home"}""");
        Assert.Equal(TargetSize.Default, TargetSize.From(document.RootElement));
    }

    [Fact]
    public void TokensWithoutMinimumTargetSizeFallsBackToTheDefault()
    {
        using JsonDocument document = JsonDocument.Parse("""{"tokens":{"spacing_medium":12}}""");
        Assert.Equal(TargetSize.Default, TargetSize.From(document.RootElement));
    }

    [Fact]
    public void CoresValueWinsWhenAboveTheFloor()
    {
        using JsonDocument document =
            JsonDocument.Parse("""{"tokens":{"minimum_target_size":60}}""");
        Assert.Equal(60, TargetSize.From(document.RootElement));
    }

    /// <summary>
    /// A malformed or adversarial value below the floor is clamped rather
    /// than trusted outright — an actionable row that shrinks to a few
    /// pixels fails a touch target, not merely a style preference.
    /// </summary>
    [Fact]
    public void ValuesBelowTheFloorAreClampedUp()
    {
        using JsonDocument document =
            JsonDocument.Parse("""{"tokens":{"minimum_target_size":4}}""");
        Assert.Equal(24, TargetSize.From(document.RootElement));
    }
}
