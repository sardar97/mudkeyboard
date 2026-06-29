using Bunit;
using MudBlazor;
using MudKeyboard.Components;

namespace MudKeyboard.Tests;

/// <summary>
/// Covers the generic <see cref="MudKeyboardNumericField{T}"/> wrapper: it must resolve the docked
/// keyboard layout from its bound CLR type (decimal → money, double/float → decimal keypad, integers →
/// plain numpad), emit that as <c>data-mudkeyboard-layout</c> on the real input the focus-capture shim
/// watches, honour an explicit override, and forward arbitrary attributes.
/// </summary>
public class MudKeyboardNumericFieldTests : MudComponentTestContext
{
    [Fact]
    public void Decimal_EmitsMoneyLayout()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>();

        Assert.Equal("money", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void NullableDecimal_EmitsMoneyLayout()
    {
        var cut = Render<MudKeyboardNumericField<decimal?>>();

        Assert.Equal("money", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void Double_EmitsDecimalLayout()
    {
        var cut = Render<MudKeyboardNumericField<double>>();

        Assert.Equal("decimal", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void Float_EmitsDecimalLayout()
    {
        var cut = Render<MudKeyboardNumericField<float>>();

        Assert.Equal("decimal", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void Int_EmitsNumpadLayout()
    {
        var cut = Render<MudKeyboardNumericField<int>>();

        Assert.Equal("numpad", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void Long_EmitsNumpadLayout()
    {
        var cut = Render<MudKeyboardNumericField<long>>();

        Assert.Equal("numpad", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void DockedKeyboardLayout_OverridesTheTypeInference()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p
            .Add(c => c.DockedKeyboardLayout, "numpad"));

        // The decimal default (money) is overridden by the explicit layout.
        Assert.Equal("numpad", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void Default_DoesNotEmitTheOptInMarker_ButStillEmitsLayout()
    {
        var cut = Render<MudKeyboardNumericField<int>>();
        var input = cut.Find("input");

        Assert.False(input.HasAttribute("data-mudkeyboard"));
        Assert.Equal("numpad", input.GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void DockedKeyboard_EmitsTheOptInMarkerOnTheInput()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.DockedKeyboard, true));
        var input = cut.Find("input");

        Assert.Equal("true", input.GetAttribute("data-mudkeyboard"));
        Assert.Equal("money", input.GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void PassthroughAttributes_AreForwardedToTheInput_AndMergedWithMarkers()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p
            .Add(c => c.DockedKeyboard, true)
            .AddUnmatched("name", "Model.Price"));
        var input = cut.Find("input");

        Assert.Equal("true", input.GetAttribute("data-mudkeyboard"));
        Assert.Equal("money", input.GetAttribute("data-mudkeyboard-layout"));
        Assert.Equal("Model.Price", input.GetAttribute("name"));
    }

    [Fact]
    public void Label_IsRendered()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.Label, "Price"));

        Assert.Contains("Price", cut.Markup);
    }

    // The bound type T is forwarded to the underlying MudNumericField, which renders inputmode per
    // type: floating-point → "decimal", integer → "numeric". This proves T flows through the wrapper.
    [Fact]
    public void DecimalType_ForwardsToMudNumericField_AsDecimalInputMode() =>
        Assert.Equal("decimal", Render<MudKeyboardNumericField<decimal>>().Find("input").GetAttribute("inputmode"));

    [Fact]
    public void DoubleType_ForwardsToMudNumericField_AsDecimalInputMode() =>
        Assert.Equal("decimal", Render<MudKeyboardNumericField<double>>().Find("input").GetAttribute("inputmode"));

    [Fact]
    public void IntType_ForwardsToMudNumericField_AsNumericInputMode() =>
        Assert.Equal("numeric", Render<MudKeyboardNumericField<int>>().Find("input").GetAttribute("inputmode"));
}
