// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Vauchi.Tests;

/// <summary>
/// Verifies parsing of available_screens JSON arrays from the Vauchi App API
/// and formatting of screen names for UI display.
/// </summary>
public class SidebarParsingTests
{
    /// <summary>
    /// Parse screen IDs from a JSON array, matching the logic in MainWindow.RefreshSidebar().
    /// </summary>
    private static List<string> ParseScreenIds(string json)
    {
        var ids = new List<string>();
        using var doc = JsonDocument.Parse(json);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            ids.Add(el.GetString() ?? "unknown");
        }
        return ids;
    }

    /// <summary>
    /// Format a screen ID to a user-friendly title, matching MainWindow.FormatScreenName().
    /// Converts snake_case to Title Case.
    /// </summary>
    private static string FormatScreenName(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        string display = id.Replace('_', ' ');
        return char.ToUpper(display[0]) + display[1..];
    }

    [Fact]
    public void ParseScreenIds_OnboardingOnly()
    {
        var ids = ParseScreenIds(@"[""onboarding""]");
        Assert.Single(ids);
        Assert.Equal("onboarding", ids[0]);
    }

    [Fact]
    public void ParseScreenIds_MultipleScreens()
    {
        var ids = ParseScreenIds(@"[""exchange"",""my_info"",""contacts"",""settings"",""help""]");
        Assert.Equal(5, ids.Count);
        Assert.Equal("exchange", ids[0]);
        Assert.Equal("my_info", ids[1]);
        Assert.Equal("contacts", ids[2]);
        Assert.Equal("settings", ids[3]);
        Assert.Equal("help", ids[4]);
    }

    [Fact]
    public void ParseScreenIds_EmptyArray()
    {
        var ids = ParseScreenIds("[]");
        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("onboarding", "Onboarding")]
    [InlineData("exchange", "Exchange")]
    [InlineData("my_info", "My info")]
    [InlineData("contacts", "Contacts")]
    [InlineData("settings", "Settings")]
    [InlineData("help", "Help")]
    [InlineData("device_linking", "Device linking")]
    [InlineData("emergency_shred", "Emergency shred")]
    public void FormatScreenName_SnakeCaseToTitleCase(string input, string expected)
    {
        Assert.Equal(expected, FormatScreenName(input));
    }

    [Fact]
    public void FormatScreenName_SingleWord()
    {
        Assert.Equal("Onboarding", FormatScreenName("onboarding"));
        Assert.Equal("Exchange", FormatScreenName("exchange"));
        Assert.Equal("Contacts", FormatScreenName("contacts"));
    }

    [Fact]
    public void FormatScreenName_MultipleUnderscores()
    {
        Assert.Equal("Device linking", FormatScreenName("device_linking"));
        Assert.Equal("Emergency shred", FormatScreenName("emergency_shred"));
    }

    [Fact]
    public void FormatScreenName_EmptyString()
    {
        Assert.Equal("", FormatScreenName(""));
    }

    [Fact]
    public void FormatScreenName_NullInput()
    {
        Assert.Null(FormatScreenName(null!));
    }

    [Fact]
    public void ParseAndFormat_RoundTrip()
    {
        var json = @"[""exchange"",""my_info"",""contacts""]";
        var ids = ParseScreenIds(json);

        Assert.Equal(3, ids.Count);
        Assert.Equal("Exchange", FormatScreenName(ids[0]));
        Assert.Equal("My info", FormatScreenName(ids[1]));
        Assert.Equal("Contacts", FormatScreenName(ids[2]));
    }
}
