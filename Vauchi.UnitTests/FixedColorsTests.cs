// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

public class FixedColorsTests
{
    // A peer's camera finds the code by its bright square, whatever theme
    // this window paints (vauchi/private#450).
    [Fact]
    public void TheQrSquareIsOpaqueWhite()
    {
        Assert.Equal(Windows.UI.Color.FromArgb(255, 255, 255, 255), FixedColors.QrSquare);
    }
}
