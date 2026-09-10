// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi.CoreUI;

/// <summary>The visual weight an action's wire "tone" resolves to.</summary>
public enum ActionTone
{
    Standard,
    Serious,
    Destructive,
}

/// <summary>
/// Maps the "tone" string Core attaches to an <c>ActionSpec</c> into the
/// tri-state <see cref="ActionTone"/> a Windows button renders.
/// </summary>
/// <remarks>
/// The wire protocol's <c>ActionTone</c> enum only distinguishes Standard
/// and Destructive today, so a "serious" string is Core signalling ahead
/// of the enum growing a third case rather than a token this build should
/// treat as unknown-and-therefore-Standard alongside real typos.
/// </remarks>
public static class ActionToneStyle
{
    public static ActionTone From(string? tone) => tone switch
    {
        "destructive" => ActionTone.Destructive,
        "serious" => ActionTone.Serious,
        _ => ActionTone.Standard,
    };
}
