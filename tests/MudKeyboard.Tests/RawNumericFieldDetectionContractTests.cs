using Bunit;
using MudBlazor;

namespace MudKeyboard.Tests;

/// <summary>
/// The "raw field" half of type-aware detection: a plain <see cref="MudNumericField{T}"/> (no wrapper)
/// must still get the right keypad on the docked keyboard. The focus-capture shim can't see the CLR type,
/// so it infers the layout from the rendered <c>inputmode</c> — floating-point types render
/// <c>inputmode="decimal"</c> (→ decimal keypad) and integer types render <c>inputmode="numeric"</c>
/// (→ plain numpad). These tests pin that MudBlazor contract so the raw-field detection can't break
/// silently if MudBlazor ever changes how it renders numeric inputs.
/// </summary>
public class RawNumericFieldDetectionContractTests : MudComponentTestContext
{
    [Fact]
    public void RawDecimalField_RendersDecimalInputMode() =>
        Assert.Equal("decimal", Render<MudNumericField<decimal>>().Find("input").GetAttribute("inputmode"));

    [Fact]
    public void RawDoubleField_RendersDecimalInputMode() =>
        Assert.Equal("decimal", Render<MudNumericField<double>>().Find("input").GetAttribute("inputmode"));

    [Fact]
    public void RawFloatField_RendersDecimalInputMode() =>
        Assert.Equal("decimal", Render<MudNumericField<float>>().Find("input").GetAttribute("inputmode"));

    [Fact]
    public void RawIntField_RendersNumericInputMode() =>
        Assert.Equal("numeric", Render<MudNumericField<int>>().Find("input").GetAttribute("inputmode"));

    [Fact]
    public void RawLongField_RendersNumericInputMode() =>
        Assert.Equal("numeric", Render<MudNumericField<long>>().Find("input").GetAttribute("inputmode"));

    [Fact]
    public void RawNumericField_ForwardsAForcedLayoutDataAttributeOntoTheInput()
    {
        // The documented escape hatch: force money on a raw field with data-mudkeyboard-layout="money".
        // It must land on the actual <input> the shim watches (MudNumericField splats unmatched attributes
        // onto the input), so an explicit override always works without the wrapper.
        var cut = Render<MudNumericField<decimal>>(p => p.AddUnmatched("data-mudkeyboard-layout", "money"));

        Assert.Equal("money", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }
}
