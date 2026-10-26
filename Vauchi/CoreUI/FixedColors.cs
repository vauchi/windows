// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Windows.UI;

namespace Vauchi.CoreUI;

/// <summary>
/// Colours that are the design itself rather than a theme's, so they stay
/// the same whatever palette Core sends.
/// </summary>
public static class FixedColors
{
    // White in every theme, so a peer's camera sees one bright square
    // around a display code (vauchi/private#450).
    public static readonly Color QrSquare = Color.FromArgb(255, 255, 255, 255); // design-token-ok: a QR quiet zone is white in every theme (vauchi/private#450)
}
