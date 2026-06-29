using MudKeyboard.Internal;
using MudKeyboard.Layouts;
using MudKeyboard.Models;

namespace MudKeyboard.Tests;

public class KeyboardEngineCasingTests
{
    [Theory]
    [InlineData("a", true, "A")]
    [InlineData("a", false, "a")]
    [InlineData("z", true, "Z")]
    [InlineData("A", true, "A")]
    [InlineData("1", true, "1")]      // non-letters unchanged
    [InlineData("£", true, "£")]
    [InlineData("{bksp}", true, "{bksp}")]
    public void ApplyCase_UpperCasesOnlyLettersWhenUpper(string token, bool upper, string expected) =>
        Assert.Equal(expected, KeyboardEngine.ApplyCase(token, upper));

    [Theory]
    [InlineData("a", true)]
    [InlineData("Z", true)]
    [InlineData("1", false)]
    [InlineData("ab", false)]
    [InlineData("{sym}", false)]
    [InlineData("", false)]
    public void IsLetterToken_DetectsSingleAsciiLetters(string token, bool expected) =>
        Assert.Equal(expected, KeyboardEngine.IsLetterToken(token));
}

public class KeyTokensTests
{
    [Theory]
    [InlineData("{bksp}", true)]
    [InlineData("{shift}", true)]
    [InlineData("{sym}", true)]
    [InlineData("{space}", true)]
    [InlineData("{esc}", true)]
    [InlineData("a", false)]
    [InlineData("1", false)]
    public void IsCommand_DetectsBraceWrappedTokens(string token, bool expected) =>
        Assert.Equal(expected, KeyTokens.IsCommand(token));

    [Fact]
    public void IsCommand_TreatsEnterAsNotACommand() =>
        // Enter is brace-wrapped but is deliberately excluded so it is handled as a commit, not skipped.
        Assert.False(KeyTokens.IsCommand(KeyTokens.Enter));

    [Theory]
    [InlineData("{enter}", true)]
    [InlineData("{bksp}", false)]
    [InlineData("a", false)]
    public void IsEnter_DetectsOnlyTheEnterToken(string token, bool expected) =>
        Assert.Equal(expected, KeyTokens.IsEnter(token));
}

public class BuildDisplayKeysShiftTests
{
    private static readonly KeyboardLayout Layout = new()
    {
        Rows = new string[][]
        {
            [KeyTokens.Shift, "a", "b"],
            [KeyTokens.SymbolToggle, KeyTokens.Space, KeyTokens.Enter],
        },
    };

    [Fact]
    public void ShiftOff_LettersStayLowercase()
    {
        var a = KeyboardEngine.BuildDisplayKeys(Layout, ShiftState.Off)[0][1];

        Assert.Equal("a", a.DisplayLabel);
        Assert.Equal("a", a.ActionToken);
        Assert.False(a.Bold);
    }

    [Fact]
    public void ShiftOneShot_LettersDisplayUppercaseBold_ButActionTokenStaysLowercase() =>
        AssertLetterUppercasedInDisplayOnly(ShiftState.OneShot);

    [Fact]
    public void ShiftLocked_LettersDisplayUppercaseBold_ButActionTokenStaysLowercase() =>
        AssertLetterUppercasedInDisplayOnly(ShiftState.Locked);

    // ShiftState is internal, so it cannot appear in a public [Theory] signature — assert via a helper.
    private static void AssertLetterUppercasedInDisplayOnly(ShiftState state)
    {
        var a = KeyboardEngine.BuildDisplayKeys(Layout, state)[0][1];

        Assert.Equal("A", a.DisplayLabel);
        Assert.True(a.Bold);
        Assert.Equal("a", a.ActionToken); // casing of the emitted char is decided at press time
    }

    [Fact]
    public void ShiftLocked_ShiftKeyShowsCapsGlyphAndIsHighlighted()
    {
        var shift = KeyboardEngine.BuildDisplayKeys(Layout, ShiftState.Locked)[0][0];

        Assert.Equal("⇪", shift.DisplayLabel);
        Assert.True(shift.Highlighted);
    }

    [Fact]
    public void ShiftOneShot_ShiftKeyHighlightedWithoutCapsGlyph()
    {
        var shift = KeyboardEngine.BuildDisplayKeys(Layout, ShiftState.OneShot)[0][0];

        Assert.True(shift.Highlighted);
        Assert.NotEqual("⇪", shift.DisplayLabel);
    }

    [Theory]
    [InlineData(false, "123")]
    [InlineData(true, "ABC")]
    public void SymbolToggleKey_ShowsTheFaceItSwitchesTo(bool symbolMode, string expectedLabel)
    {
        var toggle = KeyboardEngine.BuildDisplayKeys(Layout, ShiftState.Off, symbolMode)[1][0];

        Assert.Equal(expectedLabel, toggle.DisplayLabel);
    }
}

public class NumpadLayoutTests
{
    [Fact]
    public void Numpad_HasNoDecimalPointKey() =>
        Assert.DoesNotContain(".", LayoutLibrary.Numpad.Rows.SelectMany(r => r));

    [Fact]
    public void NumpadWithDecimal_HasADecimalPointKey() =>
        Assert.Contains(".", LayoutLibrary.NumpadWithDecimal.Rows.SelectMany(r => r));

    [Fact]
    public void Numpad_AndNumpadWithDecimal_AreDistinctLayouts() =>
        Assert.NotEqual(
            LayoutLibrary.Numpad.Rows.SelectMany(r => r),
            LayoutLibrary.NumpadWithDecimal.Rows.SelectMany(r => r));
}

public class LayoutLibraryVariantTests
{
    [Fact]
    public void ForVariant_Full_IsQwerty() =>
        Assert.Same(LayoutLibrary.Qwerty, LayoutLibrary.ForVariant(KeyboardVariant.Full));

    [Fact]
    public void ForVariant_Numpad_IsNumpad() =>
        Assert.Same(LayoutLibrary.Numpad, LayoutLibrary.ForVariant(KeyboardVariant.Numpad));

    [Fact]
    public void ForVariant_Pricepad_IsPrice() =>
        Assert.Same(LayoutLibrary.Price, LayoutLibrary.ForVariant(KeyboardVariant.Pricepad));

    [Fact]
    public void ForVariant_Custom_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => LayoutLibrary.ForVariant(KeyboardVariant.Custom));

    [Fact]
    public void SymbolsForVariant_Full_IsSymbols() =>
        Assert.Same(LayoutLibrary.Symbols, LayoutLibrary.SymbolsForVariant(KeyboardVariant.Full));

    [Theory]
    [InlineData(KeyboardVariant.Numpad)]
    [InlineData(KeyboardVariant.Pricepad)]
    [InlineData(KeyboardVariant.Custom)]
    public void SymbolsForVariant_NonFull_IsNull(KeyboardVariant variant) =>
        Assert.Null(LayoutLibrary.SymbolsForVariant(variant));
}
