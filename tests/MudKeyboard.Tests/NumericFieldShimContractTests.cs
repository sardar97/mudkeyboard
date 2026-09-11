using Bunit;
using MudBlazor;

namespace MudKeyboard.Tests;

/// <summary>
/// Pins the parts of <see cref="MudNumericField{T}"/>'s markup that the focus-capture shim
/// (<c>wwwroot/mudKeyboard.js</c>) relies on, so they can't change silently under a MudBlazor upgrade:
/// the <c>role="spinbutton"</c> input (per-keystroke 'change' suppression, settled-text sync), the
/// <c>aria-valuenow</c> / <c>aria-valuetext</c> mirror of the field's settled text (GitHub #8), and the
/// ▲/▼ spin buttons living in <c>.mud-input-numeric-spin</c> inside the field's <c>.mud-input</c> wrapper
/// (so a spin-button press can be tied to its input and never opens the keyboard — GitHub #9).
/// </summary>
public class NumericFieldShimContractTests : MudComponentTestContext
{
    [Fact]
    public void NumericField_RendersASpinbuttonInput_WithAriaValueNow()
    {
        var cut = Render<MudNumericField<double>>(p => p.Add(x => x.Value, 30));

        var input = cut.Find("input");
        Assert.Equal("spinbutton", input.GetAttribute("role"));
        Assert.Equal("30", input.GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void NumericField_MirrorsTextThatDiffersFromTheValue_IntoAriaValueText()
    {
        // With a Format the displayed text ("30.00") is not the plain number ("30"), so it is exposed as
        // aria-valuetext — the attribute the shim prefers when syncing the settled text.
        var cut = Render<MudNumericField<double>>(p => p.Add(x => x.Value, 30).Add(x => x.Format, "F2"));

        var input = cut.Find("input");
        Assert.Equal("30", input.GetAttribute("aria-valuenow"));
        Assert.Equal("30.00", input.GetAttribute("aria-valuetext"));
    }

    [Fact]
    public void NumericField_SpinButtons_LiveInTheSpinWrapper_InsideTheInputWrapper()
    {
        var cut = Render<MudNumericField<int>>();

        var buttons = cut.FindAll(".mud-input .mud-input-numeric-spin button");
        Assert.Equal(2, buttons.Count);
        // The input the shim resolves from a spin-button press: the closest .mud-input's <input>.
        var resolved = cut.Find(".mud-input-numeric-spin").Closest(".mud-input")!.QuerySelector("input");
        Assert.NotNull(resolved);
        Assert.Equal(cut.Find("input").GetAttribute("id"), resolved.GetAttribute("id"));
        Assert.Equal("spinbutton", resolved.GetAttribute("role"));
    }
}
