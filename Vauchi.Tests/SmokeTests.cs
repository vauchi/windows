// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace Vauchi.Tests;

// These smoke tests live in the WinUI-context project and exercise the
// reducer boundary used by PresentationHost.
public class SmokeTests
{
    // @scenario: generic_presentation_protocol.feature :: Invalid boundary input fails safely
    [Fact]
    public void PresentationState_PreservesUnknownCommand_AsEffect()
    {
        var state = new CoreUI.PresentationState();

        bool applied = state.TryApplyEnvelope(
            """{"commands":[{"FutureEffect":{"value":1}}]}""",
            out var effects,
            out var error);

        Assert.True(applied);
        Assert.Null(error);
        Assert.Single(effects);
        Assert.Equal("FutureEffect", CoreUI.PresentationState.CommandName(effects[0]));
    }

    // @scenario: generic_presentation_protocol.feature :: Invalid boundary input fails safely
    [Fact]
    public void PresentationState_RejectsEnvelope_WithoutCommands()
    {
        var state = new CoreUI.PresentationState();

        bool applied = state.TryApplyEnvelope(
            """{"schema_version":1}""",
            out var effects,
            out var error);

        Assert.False(applied);
        Assert.Empty(effects);
        Assert.Equal("command envelope is missing commands", error);
    }
}
