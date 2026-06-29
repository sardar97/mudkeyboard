using Bunit;
using MudKeyboard.Internal;
using MudKeyboard.Layouts;
using MudKeyboard.Models;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

// GitHub #3 — optional ± sign-toggle key so the numeric keypads can enter negative numbers.

/// <summary>The pure sign-toggle helper used by the plain/decimal numeric keypads.</summary>
public class ToggleSignEngineTests
{
    [Theory]
    [InlineData(null, "-")]
    [InlineData("", "-")]
    [InlineData("5", "-5")]
    [InlineData("12.5", "-12.5")]
    [InlineData("-5", "5")]
    [InlineData("-", "")]
    public void ToggleSign_FlipsLeadingMinus(string? current, string expected) =>
        Assert.Equal(expected, KeyboardEngine.ToggleSign(current));
}

/// <summary>Sign support in the money formatter: a leading minus, suppressed for a zero amount.</summary>
public class PricepadFormatterSignTests
{
    [Theory]
    [InlineData("523", true, "-£5.23")]
    [InlineData("523", false, "£5.23")]
    [InlineData("5", true, "-£0.05")]
    // A zero amount is never shown as negative — no "-£0.00".
    [InlineData("", true, "£0.00")]
    [InlineData("0", true, "£0.00")]
    [InlineData("00", true, "£0.00")]
    public void Format_WithNegative_PrefixesMinusExceptForZero(string digits, bool negative, string expected) =>
        Assert.Equal(expected, PricepadFormatter.Format(digits, "£", 2, negative));

    [Theory]
    [InlineData("-£5.23", true)]
    [InlineData("  -£5.23", true)]
    [InlineData("£5.23", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsNegative_DetectsLeadingMinus(string? value, bool expected) =>
        Assert.Equal(expected, PricepadFormatter.IsNegative(value));
}

/// <summary>The <c>{sign}</c> token resolves to a ± key with a spoken name.</summary>
public class SignKeyTokenTests
{
    [Fact]
    public void FromToken_Sign_ShowsPlusMinusGlyph()
    {
        var key = KeyboardKey.FromToken(KeyTokens.Sign);

        Assert.Equal(KeyTokens.Sign, key.ActionToken);
        Assert.Equal("±", key.DisplayLabel);
        Assert.True(key.IsCommand);
    }

    [Fact]
    public void Sign_AccessibleLabel_IsToggleSign() =>
        Assert.Equal("Toggle sign", KeyboardKey.FromToken(KeyTokens.Sign).AccessibleLabel);
}

/// <summary>The signed keypad layouts add a ± key; the kind helpers recognise them.</summary>
public class SignedLayoutTests
{
    public static IEnumerable<object[]> SignedLayouts() =>
    [
        [LayoutLibrary.NumpadSigned],
        [LayoutLibrary.NumpadWithDecimalSigned],
        [LayoutLibrary.PriceSigned],
    ];

    public static IEnumerable<object[]> UnsignedLayouts() =>
    [
        [LayoutLibrary.Numpad],
        [LayoutLibrary.NumpadWithDecimal],
        [LayoutLibrary.Price],
    ];

    [Theory]
    [MemberData(nameof(SignedLayouts))]
    public void SignedLayouts_ContainTheSignKey(KeyboardLayout layout) =>
        Assert.Contains(KeyTokens.Sign, layout.Rows.SelectMany(r => r));

    [Theory]
    [MemberData(nameof(UnsignedLayouts))]
    public void DefaultLayouts_DoNotContainTheSignKey(KeyboardLayout layout) =>
        Assert.DoesNotContain(KeyTokens.Sign, layout.Rows.SelectMany(r => r));

    [Fact]
    public void SignedLayout_AddsSignKeyToTheEnterRow_LeavingDigitRowsUnchanged()
    {
        var baseRows = LayoutLibrary.Numpad.Rows;
        var signedRows = LayoutLibrary.NumpadSigned.Rows;

        Assert.Equal(baseRows.Count, signedRows.Count);
        // Digit rows (all but the last/Enter row) are identical to the base layout.
        Assert.Equal(baseRows.SkipLast(1).SelectMany(r => r), signedRows.SkipLast(1).SelectMany(r => r));
        // The Enter row gains a leading ± key.
        Assert.Equal(KeyTokens.Sign, signedRows[^1][0]);
    }

    [Fact]
    public void KindHelpers_RecogniseSignedVariants()
    {
        Assert.True(LayoutLibrary.IsMoney(LayoutLibrary.PriceSigned));
        Assert.True(LayoutLibrary.IsKeypad(LayoutLibrary.NumpadSigned));
        Assert.True(LayoutLibrary.IsKeypad(LayoutLibrary.NumpadWithDecimalSigned));
        Assert.False(LayoutLibrary.IsMoney(LayoutLibrary.NumpadSigned));
        Assert.False(LayoutLibrary.IsKeypad(LayoutLibrary.Qwerty));
    }
}

/// <summary>Inline <see cref="MudKeyboard.Components.MudNumpad"/> with <c>AllowNegative</c>.</summary>
public class MudNumpadNegativeTests : MudComponentTestContext
{
    private static bool HasSignKey(IRenderedComponent<MudKeyboard.Components.MudNumpad> cut) =>
        cut.FindAll("button").Any(b => b.GetAttribute("aria-label") == "Toggle sign");

    private static void Press(IRenderedComponent<MudKeyboard.Components.MudNumpad> cut, string ariaLabel) =>
        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == ariaLabel).Click();

    [Fact]
    public void Default_HasNoSignKey() =>
        Assert.False(HasSignKey(Render<MudKeyboard.Components.MudNumpad>()));

    [Fact]
    public void AllowNegative_ShowsTheSignKey() =>
        Assert.True(HasSignKey(Render<MudKeyboard.Components.MudNumpad>(p => p.Add(c => c.AllowNegative, true))));

    [Fact]
    public void SignKey_MakesAPositiveValueNegative()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.AllowNegative, true)
            .Add(c => c.Value, "5")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Toggle sign");

        Assert.Equal("-5", captured);
    }

    [Fact]
    public void SignKey_OnANegativeValue_MakesItPositive()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.AllowNegative, true)
            .Add(c => c.Value, "-5")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Toggle sign");

        Assert.Equal("5", captured);
    }

    [Fact]
    public void AllowNegative_WithDecimal_ShowsBothSignAndDecimalKeys()
    {
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.AllowNegative, true)
            .Add(c => c.AllowDecimal, true));

        Assert.True(HasSignKey(cut));
        Assert.Contains(cut.FindAll("button"), b => b.GetAttribute("aria-label") == "Decimal point");
    }
}

/// <summary>Inline <see cref="MudKeyboard.Components.MudPricepad"/> with <c>AllowNegative</c>.</summary>
public class MudPricepadNegativeTests : MudComponentTestContext
{
    private static void Press(IRenderedComponent<MudKeyboard.Components.MudPricepad> cut, string ariaLabel) =>
        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == ariaLabel).Click();

    [Fact]
    public void Default_HasNoSignKey() =>
        Assert.DoesNotContain(
            Render<MudKeyboard.Components.MudPricepad>().FindAll("button"),
            b => b.GetAttribute("aria-label") == "Toggle sign");

    [Fact]
    public void SignKey_MakesTheAmountNegative()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.AllowNegative, true)
            .Add(c => c.Value, "£5.23")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Toggle sign");

        Assert.Equal("-£5.23", captured);
    }

    [Fact]
    public void Negative_SignIsPreservedWhenTypingMoreDigits()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.AllowNegative, true)
            .Add(c => c.Value, "-£0.52")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "3"); // digits 52 → 523 → 5.23, sign preserved

        Assert.Equal("-£5.23", captured);
    }
}

/// <summary>Docked keyboard: layout resolution and the per-field / global negative opt-in.</summary>
public class DockedNegativeTests
{
    private static KeyboardInteropService NewService() =>
        new(new NoopJsRuntime(), new MudKeyboardOptions());

    [Theory]
    [InlineData("numpad")]
    [InlineData("decimal")]
    [InlineData("money")]
    public void ResolveLayout_AllowNegative_ReturnsTheSignedVariant(string kind)
    {
        var (layout, _) = KeyboardInteropService.ResolveLayout(kind, allowNegative: true);

        Assert.Contains(KeyTokens.Sign, layout.Rows.SelectMany(r => r));
    }

    [Fact]
    public void OnFocusIn_FieldAttributeTrue_ShowsTheSignedKeypad()
    {
        var service = NewService();

        service.OnFocusIn("numpad", 0, currentValue: "", allowNegative: "true");

        Assert.Same(LayoutLibrary.NumpadSigned, service.CurrentLayout);
    }

    [Fact]
    public void OnFocusIn_FieldAttributeFalse_OverridesAGlobalDefaultOfTrue()
    {
        var service = NewService();
        service.AllowNegativeDefault = true;

        service.OnFocusIn("numpad", 0, currentValue: "", allowNegative: "false");

        Assert.Same(LayoutLibrary.Numpad, service.CurrentLayout);
    }

    [Fact]
    public void OnFocusIn_NoAttribute_FallsBackToTheGlobalDefault()
    {
        var service = NewService();
        service.AllowNegativeDefault = true;

        service.OnFocusIn("decimal", 0, currentValue: "", allowNegative: "");

        Assert.Same(LayoutLibrary.NumpadWithDecimalSigned, service.CurrentLayout);
    }

    [Fact]
    public void OnFocusIn_MoneyFieldWithNegativeValue_ResolvesSignedPriceKeypad()
    {
        var service = NewService();

        service.OnFocusIn("money", 0, currentValue: "-£1.23", allowNegative: "true");

        Assert.Same(LayoutLibrary.PriceSigned, service.CurrentLayout);
    }

    // A JS runtime that returns defaults; the module is never loaded so its members are not invoked.
    private sealed class NoopJsRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult<TValue>(default!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult<TValue>(default!);
    }
}
