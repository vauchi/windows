// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.Platform;
using Xunit;

namespace Vauchi.Tests;

public class SystemTrayManagerTests
{
    [Fact]
    public void Dispose_WithoutInitialize_DoesNotThrow()
    {
        var manager = new SystemTrayManager(null!);
        manager.Dispose();
    }
}
