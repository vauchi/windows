// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace Vauchi.UnitTests;

// Headless: VauchiNative.LibName is an unconditional const, so this runs in
// the auto unit-test lane (test:unit) without the native DLL loaded.
public class VauchiNativeTests
{
    [Fact]
    public void VauchiNative_LibName_IsCorrect()
    {
        // Verify the native library name constant is set correctly.
        // Actual P/Invoke calls require the native DLL at runtime.
        Assert.Equal("vauchi_cabi", GetLibName());
    }

    [Theory]
    [InlineData("AppInitialCommands")]
    [InlineData("AppDispatch")]
    public void GenericPresentationBoundary_IsExposed(string methodName)
    {
        var method = typeof(Interop.VauchiNative).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        Assert.Equal(typeof(string), method!.ReturnType);
    }

    [Theory]
    [InlineData("AppCurrentScreen")]
    [InlineData("AppHandleAction")]
    [InlineData("AppHandleHardwareEvent")]
    [InlineData("AppNavigateTo")]
    [InlineData("AppNavigateBack")]
    [InlineData("AppAvailableScreens")]
    [InlineData("AppDefaultScreen")]
    [InlineData("AppTabInfo")]
    [InlineData("AppSidebarItems")]
    public void LegacyScreenAndActionBoundary_IsNotExposed(string methodName)
    {
        var method = typeof(Interop.VauchiNative).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.Null(method);
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
