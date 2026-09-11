// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class ScreenCatalogTests
{
    private static string TwoEntryCatalog() =>
        File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "screen_catalog_two_entries.json"));

    [Fact]
    public void TwoEntryCatalog_ReducesToTwoSurfacesWithTheirTitles()
    {
        IReadOnlyList<ScreenCatalogEntry> entries = ScreenCatalog.Parse(TwoEntryCatalog());

        Assert.Equal(
            new[] { "onboarding_welcome", "onboarding_name" },
            entries.Select(entry => entry.CodeId));
        Assert.All(entries, entry => Assert.Null(entry.Error));
        Assert.Equal(
            new[] { "Welcome to Vauchi", "What's your name?" },
            entries.Select(entry => entry.SurfaceTitle));
        Assert.All(entries, entry => Assert.Equal("onboarding", entry.State.ActiveSurfaceId));
        Assert.Equal("en", entries[0].Locale);
        Assert.Equal("Welcome", entries[0].Title);
    }

    [Fact]
    public void SecondEntry_CarriesItsOwnContextBarAndNavigation()
    {
        IReadOnlyList<ScreenCatalogEntry> entries = ScreenCatalog.Parse(TwoEntryCatalog());
        PresentationState state = entries[1].State;

        Assert.NotNull(state.ActiveContextBar);
        Assert.Equal("Welcome", Assert.Single(state.ActiveNavigation!).Label);
    }

    [Fact]
    public void UnknownCommandVariant_DoesNotAbortTheEntry()
    {
        const string catalog = """
            {"schema_version":1,"screens":[{"code_id":"future","title":"Future","locale":"en","commands":[
              {"FrobnicateWidget":{"surface_id":"future","mode":"unheard_of"}},
              {"ReplaceSurface":{"surface":{"surface_id":"future","revision":1,"title":"From tomorrow","layout":"holographic","nodes":[{"Hologram":{"id":"h"}}]}}}
            ]}]}
            """;

        ScreenCatalogEntry entry = Assert.Single(ScreenCatalog.Parse(catalog));

        Assert.Null(entry.Error);
        Assert.Equal("From tomorrow", entry.SurfaceTitle);
    }

    [Fact]
    public void MalformedEntry_IsReportedWithoutDroppingTheOthers()
    {
        const string catalog = """
            {"schema_version":1,"screens":[
              {"code_id":"broken","title":"Broken","locale":"en","commands":[{"ReplaceSurface":{}}]},
              {"code_id":"fine","title":"Fine","locale":"en","commands":[{"ReplaceSurface":{"surface":{"surface_id":"s","revision":1,"title":"Fine","nodes":[]}}}]}
            ]}
            """;

        IReadOnlyList<ScreenCatalogEntry> entries = ScreenCatalog.Parse(catalog);

        Assert.Equal(2, entries.Count);
        Assert.Contains("ReplaceSurface is missing surface", entries[0].Error);
        Assert.Null(entries[1].Error);
        Assert.Single(entries.Where(entry => entry.Error is null));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""{"schema_version":1}""")]
    [InlineData("""{"schema_version":2,"screens":[]}""")]
    [InlineData("not json")]
    public void InvalidCatalog_Throws(string json)
    {
        Assert.Throws<FormatException>(() => ScreenCatalog.Parse(json));
    }

    [Fact]
    public void EntryWithoutCodeId_FallsBackToItsPosition()
    {
        const string catalog = """
            {"schema_version":1,"screens":[{"title":"Nameless","commands":[]}]}
            """;

        ScreenCatalogEntry entry = Assert.Single(ScreenCatalog.Parse(catalog));

        Assert.Equal("screen-0", entry.CodeId);
        Assert.Null(entry.SurfaceTitle);
    }

    [Theory]
    [InlineData("contacts", "contacts")]
    [InlineData("card.edit-2", "card.edit-2")]
    [InlineData("../../escape", "_.._escape")]
    [InlineData("a/b\\c:d", "a_b_c_d")]
    public void FileStem_KeepsOnlyPortableFileNameCharacters(string codeId, string expected)
    {
        string catalog = $$"""
            {"schema_version":1,"screens":[{"code_id":"{{codeId.Replace("\\", "\\\\")}}","commands":[]}]}
            """;

        ScreenCatalogEntry entry = Assert.Single(ScreenCatalog.Parse(catalog));

        Assert.Equal(expected, entry.FileStem);
    }

    [Fact]
    public void LaunchArguments_SelectTheRenderMode()
    {
        Assert.True(ScreenCatalog.TryParseLaunchArguments(
            new[] { "Vauchi.exe", "--render-catalog", @"C:\c.json", @"C:\out" },
            out string catalog,
            out string outputDirectory));
        Assert.Equal(@"C:\c.json", catalog);
        Assert.Equal(@"C:\out", outputDirectory);

        Assert.False(ScreenCatalog.TryParseLaunchArguments(
            new[] { "Vauchi.exe", "--reset-for-testing" }, out _, out _));
        Assert.False(ScreenCatalog.TryParseLaunchArguments(
            new[] { "Vauchi.exe", "--render-catalog", @"C:\c.json" }, out _, out _));
    }
}
