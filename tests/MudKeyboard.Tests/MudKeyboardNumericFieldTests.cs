using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudKeyboard.Components;

namespace MudKeyboard.Tests;

/// <summary>
/// Covers the generic <see cref="MudKeyboardNumericField{T}"/> wrapper: it must resolve the docked
/// keyboard layout from its bound CLR type (decimal → money, double/float → decimal keypad, integers →
/// plain numpad), emit that as <c>data-mudkeyboard-layout</c> on the real input the focus-capture shim
/// watches, honour an explicit override, forward the numeric parameters, and — crucially — preserve the
/// underlying field's accessibility semantics.
/// </summary>
public class MudKeyboardNumericFieldTests : MudComponentTestContext
{
    private static string? Layout<TComponent>(IRenderedComponent<TComponent> cut) where TComponent : IComponent =>
        cut.Find("input").GetAttribute("data-mudkeyboard-layout");

    // ---- Layout resolved from the bound type -------------------------------------------------

    [Fact]
    public void Decimal_EmitsMoneyLayout() =>
        Assert.Equal("money", Layout(Render<MudKeyboardNumericField<decimal>>()));

    [Fact]
    public void NullableDecimal_EmitsMoneyLayout() =>
        Assert.Equal("money", Layout(Render<MudKeyboardNumericField<decimal?>>()));

    [Fact]
    public void Double_EmitsDecimalLayout() =>
        Assert.Equal("decimal", Layout(Render<MudKeyboardNumericField<double>>()));

    [Fact]
    public void NullableDouble_EmitsDecimalLayout() =>
        Assert.Equal("decimal", Layout(Render<MudKeyboardNumericField<double?>>()));

    [Fact]
    public void Float_EmitsDecimalLayout() =>
        Assert.Equal("decimal", Layout(Render<MudKeyboardNumericField<float>>()));

    [Fact]
    public void Int_EmitsNumpadLayout() =>
        Assert.Equal("numpad", Layout(Render<MudKeyboardNumericField<int>>()));

    [Fact]
    public void NullableInt_EmitsNumpadLayout() =>
        Assert.Equal("numpad", Layout(Render<MudKeyboardNumericField<int?>>()));

    [Fact]
    public void Long_EmitsNumpadLayout() =>
        Assert.Equal("numpad", Layout(Render<MudKeyboardNumericField<long>>()));

    [Fact]
    public void Short_EmitsNumpadLayout() =>
        Assert.Equal("numpad", Layout(Render<MudKeyboardNumericField<short>>()));

    [Fact]
    public void Byte_EmitsNumpadLayout() =>
        Assert.Equal("numpad", Layout(Render<MudKeyboardNumericField<byte>>()));

    // ---- Explicit override -------------------------------------------------------------------

    [Theory]
    [InlineData("numpad")]
    [InlineData("decimal")]
    [InlineData("qwerty")]
    [InlineData("price")]
    public void DockedKeyboardLayout_OverridesTheTypeInference(string layout)
    {
        // The decimal default (money) is overridden by whatever explicit layout is supplied.
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.DockedKeyboardLayout, layout));

        Assert.Equal(layout, Layout(cut));
    }

    [Fact]
    public void EmptyOverride_FallsBackToTypeInference()
    {
        var cut = Render<MudKeyboardNumericField<int>>(p => p.Add(c => c.DockedKeyboardLayout, string.Empty));

        Assert.Equal("numpad", Layout(cut));
    }

    // ---- Opt-in marker -----------------------------------------------------------------------

    [Theory]
    [InlineData("money")]
    [InlineData("decimal")]
    [InlineData("numpad")]
    public void LayoutMarker_IsAlwaysEmitted_EvenWithoutTheOptInFlag(string expected)
    {
        // The layout marker is independent of DockedKeyboard, so the right keypad shows in AllInputs mode
        // with zero extra configuration. (One representative render per token.)
        var input = expected switch
        {
            "money" => Render<MudKeyboardNumericField<decimal>>().Find("input"),
            "decimal" => Render<MudKeyboardNumericField<double>>().Find("input"),
            _ => Render<MudKeyboardNumericField<int>>().Find("input"),
        };

        Assert.False(input.HasAttribute("data-mudkeyboard"));
        Assert.Equal(expected, input.GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void DockedKeyboard_EmitsTheOptInMarkerOnTheInput()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.DockedKeyboard, true));
        var input = cut.Find("input");

        Assert.Equal("true", input.GetAttribute("data-mudkeyboard"));
        Assert.Equal("money", input.GetAttribute("data-mudkeyboard-layout"));
    }

    // ---- Type & parameter forwarding ---------------------------------------------------------

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
    public void Placeholder_IsForwardedToTheInput()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.Placeholder, "0.00"));

        Assert.Equal("0.00", cut.Find("input").GetAttribute("placeholder"));
    }

    [Fact]
    public void HelperText_IsRendered()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.HelperText, "Enter the price"));

        Assert.Contains("Enter the price", cut.Markup);
    }

    [Fact]
    public void MinMaxStep_AreForwardedToTheInput_WhenSet()
    {
        var cut = Render<MudKeyboardNumericField<int>>(p => p
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 100)
            .Add(c => c.Step, 5));
        var input = cut.Find("input");

        Assert.Equal("0", input.GetAttribute("min"));
        Assert.Equal("100", input.GetAttribute("max"));
        Assert.Equal("5", input.GetAttribute("step"));
    }

    [Fact]
    public void MinAndMax_AreNotForwarded_WhenUnset_SoValuesAreNotClampedToZero()
    {
        // Regression: for a value-type T the nullable Min/Max parameters default to default(T) (0), not
        // null. Forwarding them unconditionally made MudNumericField clamp every entered value to 0 — the
        // field "snapped back to 0" and never raised ValueChanged. When unset they must not be emitted.
        var input = Render<MudKeyboardNumericField<int>>().Find("input");

        Assert.False(input.HasAttribute("min"));
        Assert.False(input.HasAttribute("max"));
    }

    [Theory]
    // Regression for the same bug, observed end to end: editing must round-trip the real value through
    // the wrapper's two-way binding, not be clamped to 0. Covers immediate (oninput) and the default
    // commit-on-change path, for both an integer and a floating-point type.
    [InlineData(true)]
    [InlineData(false)]
    public void EditingTheInput_RoundTripsTheValue_NotClampedToZero(bool immediate)
    {
        int captured = -1;
        var cut = Render<MudKeyboardNumericField<int>>(p => p
            .Add(c => c.Immediate, immediate)
            .Add(c => c.ValueChanged, (int v) => captured = v));

        if (immediate)
        {
            cut.Find("input").Input("42");
        }
        else
        {
            cut.Find("input").Change("42");
        }

        Assert.Equal(42, captured);
    }

    [Fact]
    // GitHub #4 contract. With a non-immediate field (the default) the docked keyboard writes the value
    // through the native setter — which queues no native 'change' — so when the keyboard closes the
    // focus-capture shim synthesises a single 'change'. That change must do two things at once: COMMIT the
    // typed value into the binding AND run Min/Max validation — so an out-of-range "100" entered into a
    // Max=10 field lands as 10 (clamped) in the bound value and in the rendered input, never left as a
    // stuck, unbound "100". This pins the MudNumericField behaviour the shim's commit-on-close depends on.
    // (Immediate fields commit live on 'input', so they need no change-on-close.)
    public void ChangeOnClose_OutOfRangeValue_ClampsToMaxAndCommits()
    {
        decimal captured = -1m;
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p
            .Add(c => c.Min, -10m)
            .Add(c => c.Max, 10m)
            .Add(c => c.ValueChanged, (decimal v) => captured = v));

        cut.Find("input").Change("100");

        Assert.Equal(10m, captured);
        Assert.Equal("10", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void EditingADecimalField_RoundTripsTheValue()
    {
        decimal captured = -1m;
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p
            .Add(c => c.Immediate, true)
            .Add(c => c.ValueChanged, (decimal v) => captured = v));

        cut.Find("input").Input("5.23");

        Assert.Equal(5.23m, captured);
    }

    [Fact]
    public void AdornmentIcon_IsRendered()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p
            .Add(c => c.Adornment, Adornment.Start)
            .Add(c => c.AdornmentIcon, Icons.Material.Filled.CurrencyPound));

        // MudBlazor renders adornments inside the input control as SVG icons.
        Assert.NotEmpty(cut.FindAll("svg"));
    }

    [Fact]
    public void Label_IsRendered()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.Label, "Price"));

        Assert.Contains("Price", cut.Markup);
    }

    // ---- Accessibility -----------------------------------------------------------------------
    // The wrapper must not weaken MudNumericField's accessibility: the input keeps spinbutton semantics,
    // exposes its value bounds, is programmatically associated with its visible label, and lets callers
    // supply an explicit aria-label that coexists with the docked-keyboard data attributes.

    [Fact]
    public void Input_KeepsTheSpinButtonRole()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>();

        Assert.Equal("spinbutton", cut.Find("input").GetAttribute("role"));
    }

    [Fact]
    public void Input_ExposesAriaValueNow()
    {
        var cut = Render<MudKeyboardNumericField<int>>();

        Assert.True(cut.Find("input").HasAttribute("aria-valuenow"));
    }

    [Fact]
    public void MinAndMax_AreExposedAsAriaValueMinAndMax_ForAssistiveTech()
    {
        var cut = Render<MudKeyboardNumericField<int>>(p => p
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 100));
        var input = cut.Find("input");

        Assert.Equal("0", input.GetAttribute("aria-valuemin"));
        Assert.Equal("100", input.GetAttribute("aria-valuemax"));
    }

    [Fact]
    public void Label_IsProgrammaticallyAssociatedWithTheInput()
    {
        // A <label for="{inputId}"> gives the field a programmatic accessible name — screen readers
        // announce "Price" when the input is focused.
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.Label, "Price"));
        var input = cut.Find("input");
        var label = cut.Find("label");

        Assert.Equal("Price", label.TextContent.Trim());
        Assert.Equal(input.GetAttribute("id"), label.GetAttribute("for"));
        Assert.False(string.IsNullOrEmpty(input.GetAttribute("id")));
    }

    [Fact]
    public void ExplicitAriaLabel_IsForwarded_AndCoexistsWithTheLayoutMarker()
    {
        // For a field with no visible label, the caller can supply an aria-label; it must reach the input
        // and must not be clobbered by the docked-keyboard data attributes.
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p
            .Add(c => c.DockedKeyboard, true)
            .AddUnmatched("aria-label", "Total price"));
        var input = cut.Find("input");

        Assert.Equal("Total price", input.GetAttribute("aria-label"));
        Assert.Equal("money", input.GetAttribute("data-mudkeyboard-layout"));
        Assert.Equal("true", input.GetAttribute("data-mudkeyboard"));
    }

    [Fact]
    public void Disabled_DisablesTheInput()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void ReadOnly_MarksTheInputReadOnly()
    {
        var cut = Render<MudKeyboardNumericField<decimal>>(p => p.Add(c => c.ReadOnly, true));

        Assert.True(cut.Find("input").HasAttribute("readonly"));
    }

    // The user's headline scenario binds via @bind-Value with a validation expression
    // (For="@(() => Model.Price)"). Forwarding For must not break rendering or the layout marker.
    private decimal _forProbe = 12.34m;

    [Fact]
    public void For_ValidationExpression_IsForwarded_AndDoesNotBreakRenderingOrTheLayoutMarker()
    {
        Expression<Func<decimal>> forExpression = () => _forProbe;

        var cut = Render<MudKeyboardNumericField<decimal>>(p => p
            .Add(c => c.For, forExpression)
            .Add(c => c.Format, "N2"));

        Assert.Equal("money", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }
}
