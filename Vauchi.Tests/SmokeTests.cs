// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Xunit;

namespace Vauchi.Tests;

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

    [Fact]
    public void VauchiNative_LibName_IsCorrect()
    {
        // Verify the native library name constant is set correctly.
        // Actual P/Invoke calls require the native DLL at runtime.
        Assert.Equal("vauchi_cabi", GetLibName());
    }

    private static string GetLibName()
    {
        // Reflection to verify the constant without calling into native code
        var field = typeof(Interop.VauchiNative)
            .GetField("LibName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        var value = field!.GetValue(null) as string;
        Assert.NotNull(value);
        return value!;
    }
}
