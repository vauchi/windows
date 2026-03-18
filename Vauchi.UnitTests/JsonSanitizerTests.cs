// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

public class JsonSanitizerTests
{
    [Fact]
    public void Null_Returns_Null_String()
    {
        Assert.Equal("null", JsonSanitizer.SafeType(null));
    }

    [Fact]
    public void Object_Returns_First_Property_Name()
    {
        Assert.Equal("ActionPressed", JsonSanitizer.SafeType("{\"ActionPressed\":{\"action_id\":\"cancel\"}}"));
    }

    [Fact]
    public void Tagged_Variant_Returns_Key_Name()
    {
        Assert.Equal("TextChanged", JsonSanitizer.SafeType("{\"TextChanged\":{\"component_id\":\"name\",\"value\":\"Alice\"}}"));
    }

    [Fact]
    public void Does_Not_Leak_PII_Values()
    {
        string result = JsonSanitizer.SafeType("{\"TextChanged\":{\"component_id\":\"name\",\"value\":\"Secret Name\"}}");
        Assert.DoesNotContain("Secret", result);
        Assert.DoesNotContain("Name", result);
    }

    [Fact]
    public void Bare_String_Returns_String_Kind()
    {
        Assert.Equal("String", JsonSanitizer.SafeType("\"Complete\""));
    }

    [Fact]
    public void Empty_Object_Returns_Braces()
    {
        Assert.Equal("{}", JsonSanitizer.SafeType("{}"));
    }

    [Fact]
    public void Array_Returns_Array_Kind()
    {
        Assert.Equal("Array", JsonSanitizer.SafeType("[1,2,3]"));
    }

    [Fact]
    public void Number_Returns_Number_Kind()
    {
        Assert.Equal("Number", JsonSanitizer.SafeType("42"));
    }

    [Fact]
    public void Boolean_Returns_Kind()
    {
        string result = JsonSanitizer.SafeType("true");
        Assert.Contains("True", result);
    }

    [Fact]
    public void Malformed_Json_Returns_Unparseable()
    {
        Assert.Equal("unparseable", JsonSanitizer.SafeType("{broken"));
    }

    [Fact]
    public void Empty_String_Returns_Unparseable()
    {
        Assert.Equal("unparseable", JsonSanitizer.SafeType(""));
    }
}
