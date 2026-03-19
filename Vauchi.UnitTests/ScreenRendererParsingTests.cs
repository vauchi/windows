// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Xunit;

namespace Vauchi.UnitTests;

public class ScreenRendererParsingTests
{
    [Fact]
    public void ParsesTitle()
    {
        var json = """{"screen_id":"test","title":"Settings","components":[],"actions":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Settings", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public void ParsesSubtitle_WhenPresent()
    {
        var json = """{"screen_id":"test","title":"T","subtitle":"Edit your card","components":[],"actions":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Edit your card", doc.RootElement.GetProperty("subtitle").GetString());
    }

    [Fact]
    public void SubtitleNull_WhenAbsent()
    {
        var json = """{"screen_id":"test","title":"T","subtitle":null,"components":[],"actions":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("subtitle").ValueKind);
    }

    [Fact]
    public void ParsesProgress()
    {
        var json = """{"screen_id":"test","title":"T","components":[],"actions":[],"progress":{"current_step":2,"total_steps":5,"label":"Step 2 of 5"}}""";
        using var doc = JsonDocument.Parse(json);
        var progress = doc.RootElement.GetProperty("progress");
        Assert.Equal(2, progress.GetProperty("current_step").GetInt32());
        Assert.Equal(5, progress.GetProperty("total_steps").GetInt32());
        Assert.Equal("Step 2 of 5", progress.GetProperty("label").GetString());
    }

    [Fact]
    public void ProgressNull_WhenAbsent()
    {
        var json = """{"screen_id":"test","title":"T","components":[],"actions":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("progress", out _));
    }

    [Theory]
    [InlineData("Primary")]
    [InlineData("Secondary")]
    [InlineData("Destructive")]
    public void ParsesActionStyles(string style)
    {
        var json = $$"""{"id":"btn","label":"Go","style":"{{style}}","enabled":true}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(style, doc.RootElement.GetProperty("style").GetString());
    }

    [Fact]
    public void ParsesActionEnabled_False()
    {
        var json = """{"id":"btn","label":"Go","style":"Primary","enabled":false}""";
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void ParsesScreenId()
    {
        var json = """{"screen_id":"onboarding_welcome","title":"T","components":[],"actions":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("onboarding_welcome", doc.RootElement.GetProperty("screen_id").GetString());
    }
}
