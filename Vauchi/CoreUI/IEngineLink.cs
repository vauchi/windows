// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi.CoreUI;

/// <summary>
/// What the display shell needs of the engine, and all it knows of it.
/// The app implements this over Core; the display shell links no Core
/// (ADR-078). Both calls answer with a presentation batch as JSON, or
/// null when the engine has none.
/// </summary>
public interface IEngineLink
{
    /// <summary>The commands that show the current state from nothing.</summary>
    string? InitialCommands();

    /// <summary>Hands one event to the engine and returns what it answers.</summary>
    string? Dispatch(string eventJson);
}
