// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

public class ActionJsonExtendedTests
{
    [Fact]
    public void FieldVisibilityChanged_MatchesSerdeFormat()
    {
        string json = ActionJson.FieldVisibilityChanged("email", "friends", true);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("FieldVisibilityChanged");
        Assert.Equal("email", inner.GetProperty("field_id").GetString());
        Assert.Equal("friends", inner.GetProperty("group_id").GetString());
        Assert.True(inner.GetProperty("visible").GetBoolean());
    }

    [Fact]
    public void FieldVisibilityChanged_NullGroup()
    {
        string json = ActionJson.FieldVisibilityChanged("email", null, false);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("FieldVisibilityChanged");
        Assert.Equal(JsonValueKind.Null, inner.GetProperty("group_id").ValueKind);
        Assert.False(inner.GetProperty("visible").GetBoolean());
    }

    [Fact]
    public void GroupViewSelected_MatchesSerdeFormat()
    {
        string json = ActionJson.GroupViewSelected("family");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("GroupViewSelected");
        Assert.Equal("family", inner.GetProperty("group_name").GetString());
    }

    [Fact]
    public void GroupViewSelected_NullClearsSelection()
    {
        string json = ActionJson.GroupViewSelected(null);
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("GroupViewSelected");
        Assert.Equal(JsonValueKind.Null, inner.GetProperty("group_name").ValueKind);
    }

    [Fact]
    public void SearchChanged_MatchesSerdeFormat()
    {
        string json = ActionJson.SearchChanged("contact_list", "alice");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("SearchChanged");
        Assert.Equal("contact_list", inner.GetProperty("component_id").GetString());
        Assert.Equal("alice", inner.GetProperty("query").GetString());
    }

    [Fact]
    public void SearchChanged_EmptyQuery()
    {
        string json = ActionJson.SearchChanged("cl1", "");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("SearchChanged");
        Assert.Equal("", inner.GetProperty("query").GetString());
    }

    [Fact]
    public void UndoPressed_MatchesSerdeFormat()
    {
        string json = ActionJson.UndoPressed("undo_delete");
        var doc = JsonDocument.Parse(json);
        var inner = doc.RootElement.GetProperty("UndoPressed");
        Assert.Equal("undo_delete", inner.GetProperty("action_id").GetString());
    }

    [Fact]
    public void SearchChanged_UnicodeQuery()
    {
        string json = ActionJson.SearchChanged("cl1", "田中 🎌");
        var doc = JsonDocument.Parse(json);
        Assert.Equal("田中 🎌", doc.RootElement.GetProperty("SearchChanged").GetProperty("query").GetString());
    }
}
