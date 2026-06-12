// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Xunit;

namespace Vauchi.Tests;

// ComponentRenderer.CreateComponent is compiled only outside UNIT_TEST_BUILD
// (it returns a WinUI UIElement), so these smoke tests must live in the
// WinUI-context project. They run via test:a11y, which has no name filter.
public class SmokeTests
{
    [Fact]
    public void ComponentRenderer_ReturnsNull_ForUnknownType()
    {
        string json = """{"type": "unknown_component"}""";
        using var doc = JsonDocument.Parse(json);

        var result = CoreUI.ComponentRenderer.CreateComponent(doc.RootElement, null);

        Assert.Null(result);
    }

    [Fact]
    public void ComponentRenderer_ReturnsNull_ForMissingType()
    {
        string json = """{"content": "hello"}""";
        using var doc = JsonDocument.Parse(json);

        var result = CoreUI.ComponentRenderer.CreateComponent(doc.RootElement, null);

        Assert.Null(result);
    }
}
