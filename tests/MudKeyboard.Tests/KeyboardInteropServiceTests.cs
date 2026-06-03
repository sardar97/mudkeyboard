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

    [Theory]
    [InlineData("decimal")]
    [InlineData("money")]
    [InlineData("price")]
    public void ResolveLayout_DecimalKinds_MapToNumpadWithDecimal(string kind)
    {
        var (layout, symbol) = KeyboardInteropService.ResolveLayout(kind);

        Assert.Same(LayoutLibrary.NumpadWithDecimal, layout);
        Assert.Null(symbol);
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
