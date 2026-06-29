using MudKeyboard.Internal;
using MudKeyboard.Layouts;
using MudKeyboard.Models;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

public class KeyboardInteropServiceTests
{
    [Theory]
    [InlineData("numpad")]
    [InlineData("numeric")]
    [InlineData("tel")]
    public void ResolveLayout_NumericKinds_MapToNumpadWithNoSymbolFace(string kind)
    {
        var (layout, symbol) = KeyboardInteropService.ResolveLayout(kind);

        Assert.Same(LayoutLibrary.Numpad, layout);
        Assert.Null(symbol);
    }

    [Fact]
    public void ResolveLayout_Decimal_MapsToNumpadWithDecimal()
    {
        var (layout, symbol) = KeyboardInteropService.ResolveLayout("decimal");

        Assert.Same(LayoutLibrary.NumpadWithDecimal, layout);
        Assert.Null(symbol);
    }

    [Theory]
    [InlineData("money")]
    [InlineData("price")]
    public void ResolveLayout_MoneyKinds_MapToPriceWithNoDecimalKey(string kind)
    {
        var (layout, symbol) = KeyboardInteropService.ResolveLayout(kind);

        Assert.Same(LayoutLibrary.Price, layout);
        Assert.Null(symbol);
        Assert.DoesNotContain(".", layout.Rows.SelectMany(r => r));
    }

    [Theory]
    [InlineData("qwerty")]
    [InlineData("")]
    [InlineData("something-unknown")]
    [InlineData(null)]
    public void ResolveLayout_UnknownOrText_FallsBackToQwertyWithSymbols(string? kind)
    {
        var (layout, symbol) = KeyboardInteropService.ResolveLayout(kind);

        Assert.Same(LayoutLibrary.Qwerty, layout);
        Assert.Same(LayoutLibrary.Symbols, symbol);
    }
}

public class LayoutLibraryTests
{
    [Fact]
    public void Numpad_HasDoubleZeroNextToZero()
    {
        var row = LayoutLibrary.Numpad.Rows.First(r => r.Contains("0"));
        var zeroIndex = row.ToList().IndexOf("0");

        Assert.Contains("00", row);
        Assert.Equal("00", row[zeroIndex + 1]);
    }

    [Fact]
    public void NumpadWithDecimal_HasDoubleZeroNextToZero()
    {
        var decimalRow = LayoutLibrary.NumpadWithDecimal.Rows
            .First(r => r.Contains("0"));
        var zeroIndex = decimalRow.ToList().IndexOf("0");

        Assert.Contains("00", decimalRow);
        Assert.Equal("00", decimalRow[zeroIndex + 1]);
    }

    [Theory]
    [InlineData("qwerty")] // Qwerty's Enter row is [{sym} {space} {enter}]
    [InlineData("numpad")] // Numpad's Enter row is [{enter}]
    public void WithCancelKey_InsertsCancelImmediatelyBeforeEnter(string which)
    {
        var source = which == "numpad" ? LayoutLibrary.Numpad : LayoutLibrary.Qwerty;

        var result = LayoutLibrary.WithCancelKey(source)!;

        var enterRow = result.Rows.First(r => r.Contains(KeyTokens.Enter)).ToList();
        Assert.Equal(KeyTokens.Cancel, enterRow[enterRow.IndexOf(KeyTokens.Enter) - 1]);
    }

    [Fact]
    public void WithCancelKey_Null_ReturnsNull() =>
        Assert.Null(LayoutLibrary.WithCancelKey(null));

    [Fact]
    public void WithCancelKey_LeavesOnlyOneCancel_WhenCalledTwice()
    {
        var twice = LayoutLibrary.WithCancelKey(LayoutLibrary.WithCancelKey(LayoutLibrary.Numpad))!;

        Assert.Equal(1, twice.Rows.SelectMany(r => r).Count(t => t == KeyTokens.Cancel));
    }
}

public class MoneyEntryTests
{
    // Mirrors what KeyboardInteropService.AppendMoneyDigitsAsync does per key press: append the
    // pressed digit(s) to the existing digits, then re-format pence-first with no currency symbol.
    private static string Press(string current, string digit) =>
        PricepadFormatter.Format(
            PricepadFormatter.ExtractDigits(current) + digit, string.Empty, 2);

    [Fact]
    public void TypingFiveTwoThree_Yields_5_23()
    {
        var value = string.Empty;
        value = Press(value, "5"); // 0.05
        value = Press(value, "2"); // 0.52
        value = Press(value, "3"); // 5.23

        Assert.Equal("5.23", value);
    }

    [Theory]
    [InlineData("5", "0.05")]
    [InlineData("52", "0.52")]
    [InlineData("523", "5.23")]
    [InlineData("100", "1.00")]
    public void PenceFirstFormatting(string typed, string expected)
    {
        var value = string.Empty;
        foreach (var ch in typed)
        {
            value = Press(value, ch.ToString());
        }

        Assert.Equal(expected, value);
    }
}

public class KeyboardInputTests
{
    [Fact]
    public void Char_CarriesTextAsTextKind()
    {
        var input = KeyboardInput.Char("a");

        Assert.Equal(KeyboardInputKind.Text, input.Kind);
        Assert.Equal("a", input.Text);
    }

    [Fact]
    public void CommandFactories_HaveExpectedKindsAndEmptyText()
    {
        Assert.Equal(KeyboardInputKind.Backspace, KeyboardInput.Backspace.Kind);
        Assert.Equal(KeyboardInputKind.Enter, KeyboardInput.Enter.Kind);
        Assert.Equal(KeyboardInputKind.Escape, KeyboardInput.Escape.Kind);
        Assert.Equal(string.Empty, KeyboardInput.Backspace.Text);
    }
}
