// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.IO;
using Vauchi.Platform;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// Core's opaque restorable state is kept in the data directory and handed
/// back at the next start (vauchi/private#814).
/// </summary>
public sealed class RestorableStateTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "vauchi-restorable-" + Guid.NewGuid().ToString("N"));

    public RestorableStateTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string KeptFile()
    {
        RestorableState.Keep(_directory, "placeholder");
        return Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void AKeptValueComesBackUnchanged()
    {
        RestorableState.Keep(_directory, """{"v":1,"locale":"de","theme_id":null}""");

        Assert.Equal("""{"v":1,"locale":"de","theme_id":null}""", RestorableState.Kept(_directory));
    }

    [Fact]
    public void ANewValueReplacesTheKeptOneAndLeavesNoOtherFile()
    {
        RestorableState.Keep(_directory, "first");
        RestorableState.Keep(_directory, "second");

        Assert.Equal("second", RestorableState.Kept(_directory));
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void ADataDirectoryThatKeptNothingHandsBackNothing()
    {
        Assert.Null(RestorableState.Kept(_directory));
    }

    [Fact]
    public void AnOversizedFileIsNotHandedBack()
    {
        File.WriteAllBytes(KeptFile(), new byte[4097]);

        Assert.Null(RestorableState.Kept(_directory));
    }

    [Fact]
    public void AFileAtTheBoundIsHandedBack()
    {
        File.WriteAllText(KeptFile(), new string('a', 4096));

        Assert.Equal(4096, RestorableState.Kept(_directory)?.Length);
    }

    [Fact]
    public void AFileThatIsNotUtf8IsNotHandedBack()
    {
        File.WriteAllBytes(KeptFile(), new byte[] { 0xff, 0xfe, 0x41 });

        Assert.Null(RestorableState.Kept(_directory));
    }
}
