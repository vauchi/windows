// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI.Components;
using Xunit;

namespace Vauchi.UnitTests;

// Core's Component::Preview ships both `fields` (raw — every field, incl.
// Hidden) and `visible_fields` (pre-filtered by build_visible_fields: the
// selected variant applied and Hidden fields dropped). The renderer must
// read `visible_fields`; rendering raw `fields` leaked Hidden values on
// Windows (problem 2026-05-21-component-preview-legacy-fields, Phase 2).
public class PreviewFieldsTests
{
    // "phone" is present in raw `fields` but absent from `visible_fields`
    // (i.e. Hidden / not in the selected variant) — it must never render.
    private const string CardWithHiddenPhone = """
        {
          "name": "Alice",
          "fields": [
            { "id": "email", "label": "Email", "value": "alice@example.com" },
            { "id": "phone", "label": "Phone", "value": "555-SECRET" }
          ],
          "visible_fields": [
            { "id": "email", "label": "Email", "value": "alice@example.com" }
          ],
          "variants": [],
          "selected_variant": null
        }
        """;

    [Fact]
    public void Resolve_RendersOnlyVisibleFields()
    {
        using var doc = JsonDocument.Parse(CardWithHiddenPhone);

        var fields = PreviewFields.Resolve(doc.RootElement);

        Assert.Single(fields);
        Assert.Equal("Email", fields[0].Label);
        Assert.Equal("alice@example.com", fields[0].Value);
    }

    [Fact]
    public void Resolve_DoesNotLeakHiddenFieldLabelOrValue()
    {
        using var doc = JsonDocument.Parse(CardWithHiddenPhone);

        var fields = PreviewFields.Resolve(doc.RootElement);

        Assert.DoesNotContain(fields, f => f.Label == "Phone");
        Assert.DoesNotContain(fields, f => f.Value == "555-SECRET");
    }

    [Fact]
    public void Resolve_EmptyVisibleFields_RendersNothing()
    {
        const string allHidden = """
            {
              "name": "Bob",
              "fields": [
                { "id": "email", "label": "Email", "value": "bob@example.com" }
              ],
              "visible_fields": [],
              "variants": [],
              "selected_variant": null
            }
            """;
        using var doc = JsonDocument.Parse(allHidden);

        var fields = PreviewFields.Resolve(doc.RootElement);

        Assert.Empty(fields);
    }
}
