// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Vauchi.CoreUI;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// <c>RenderChoice</c> drew a <c>ComboBox</c> for every option count, while
/// the design canvas (ADR-066) draws a segmented control for two- and
/// three-option choices such as a contact's Perspective or a group's
/// Members / Visibility switch.
///
/// The decisions are asserted here rather than on the rendered element:
/// a WinUI <c>SelectorBar</c> cannot be constructed without a XAML host,
/// and this project deliberately has none.
/// </summary>
public class PresentationChoiceShapeTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void TwoAndThreeOptionsBecomeASegmentedControl(int optionCount)
    {
        Assert.Equal(
            PresentationChoiceControl.Segmented,
            PresentationChoiceShape.Control(optionCount));
    }

    /// <summary>
    /// Settings Theme carries fifteen options; a segmented row of fifteen
    /// does not fit any pane. A single option is not a choice at all.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(15)]
    public void OtherCountsStayADropDown(int optionCount)
    {
        Assert.Equal(
            PresentationChoiceControl.DropDown,
            PresentationChoiceShape.Control(optionCount));
    }

    [Fact]
    public void OptionsKeepCoreIdsAndLabelsInOrder()
    {
        var options = PresentationChoiceShape.Options(Payload(
            """{"options":[{"id":"theirs","label":"Their Info"},{"id":"mine","label":"My Info for Them"}]}"""));

        Assert.Equal(
            new[]
            {
                new PresentationChoiceOption("theirs", "Their Info"),
                new PresentationChoiceOption("mine", "My Info for Them"),
            },
            options);
    }

    [Fact]
    public void MissingOptionsRenderAsAnEmptyList()
    {
        Assert.Empty(PresentationChoiceShape.Options(Payload("""{"binding_id":"x"}""")));
    }

    [Fact]
    public void TheSelectedOptionIsFoundById()
    {
        var options = PresentationChoiceShape.Options(Payload(
            """{"options":[{"id":"members","label":"Members"},{"id":"visibility","label":"Visibility"}]}"""));

        Assert.Equal(1, PresentationChoiceShape.SelectedIndex(options, "visibility"));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void AnUnknownSelectionSelectsNothing(string? selected)
    {
        var options = PresentationChoiceShape.Options(Payload(
            """{"options":[{"id":"members","label":"Members"}]}"""));

        Assert.Equal(-1, PresentationChoiceShape.SelectedIndex(options, selected));
    }

    /// <summary>
    /// The value handed to <c>EmitChoice</c> is Core's option id, never
    /// the displayed label; a cleared selection emits <c>null</c>.
    /// </summary>
    [Fact]
    public void TheEmittedValueIsTheOptionId()
    {
        var options = PresentationChoiceShape.Options(Payload(
            """{"options":[{"id":"theirs","label":"Their Info"},{"id":"mine","label":"My Info for Them"}]}"""));

        Assert.Equal("mine", PresentationChoiceShape.EmittedValue(options, 1));
        Assert.Null(PresentationChoiceShape.EmittedValue(options, -1));
        Assert.Null(PresentationChoiceShape.EmittedValue(options, 2));
    }

    private static JsonElement Payload(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();
}
