using Bunit;
using MudKeyboard.Models;

namespace MudKeyboard.Tests;

/// <summary>End-to-end click behaviour of the inline <see cref="MudKeyboard.Components.MudKeyboard"/>.</summary>
public class MudKeyboardInteractionTests : MudComponentTestContext
{
    private static void Press(IRenderedComponent<MudKeyboard.Components.MudKeyboard> cut, string label) =>
        cut.FindAll("button").Single(b => b.TextContent.Trim() == label).Click();

    [Fact]
    public void PressingALetter_AppendsItToValue()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "a");

        Assert.Equal("a", captured);
    }

    [Fact]
    public void PressingEnter_FiresOnEnter()
    {
        var entered = false;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.OnEnter, () => entered = true));

        Press(cut, "⏎");

        Assert.True(entered);
    }

    [Fact]
    public void SymbolToggle_FlipsToTheNumbersFace()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>();

        // The letter face has no digit keys.
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "1");

        Press(cut, "123");

        Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "1");
    }

    [Fact]
    public void MaxLength_StopsAppendingPastTheCap()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.MaxLength, 2)
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "a"); // "a"
        Press(cut, "s"); // "as"
        Press(cut, "d"); // capped at 2 → value is unchanged, so no further ValueChanged

        Assert.Equal("as", captured);
    }

    [Fact]
    public void Disabled_DisablesEveryKey()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p.Add(c => c.Disabled, true));

        Assert.NotEmpty(cut.FindAll("button"));
        Assert.All(cut.FindAll("button"), b => Assert.True(b.HasAttribute("disabled")));
    }

    [Fact]
    public void EmitMode_PressingAKey_RaisesOnInput_NotValueChanged()
    {
        KeyboardInput emitted = default;
        var emitFired = false;
        var valueChangedFired = false;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.OnInput, (KeyboardInput i) => { emitted = i; emitFired = true; })
            .Add(c => c.ValueChanged, _ => valueChangedFired = true));

        Press(cut, "a");

        Assert.True(emitFired);
        Assert.Equal(KeyboardInputKind.Text, emitted.Kind);
        Assert.Equal("a", emitted.Text);
        Assert.False(valueChangedFired);
    }
}

/// <summary>Click behaviour of <see cref="MudKeyboard.Components.MudPricepad"/> (pence-first currency entry).</summary>
public class MudPricepadInteractionTests : MudComponentTestContext
{
    [Fact]
    public void TypingDigits_FormatsPenceFirstWithDefaultSymbol()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.ValueChanged, (string v) => captured = v));

        foreach (var digit in new[] { "5", "2", "3" })
        {
            cut.FindAll("button").Single(b => b.TextContent.Trim() == digit).Click();
        }

        Assert.Equal("£5.23", captured);
    }

    [Fact]
    public void CustomCurrencySymbol_IsUsedInTheFormattedValue()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.CurrencySymbol, "$")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "5").Click();

        Assert.Equal("$0.05", captured);
    }
}

/// <summary>Click behaviour of <see cref="MudKeyboard.Components.MudNumpad"/>, focusing on the decimal option.</summary>
public class MudNumpadDecimalTests : MudComponentTestContext
{
    [Fact]
    public void WithoutAllowDecimal_HasNoDecimalKey()
    {
        var cut = Render<MudKeyboard.Components.MudNumpad>();

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == ".");
    }

    [Fact]
    public void AllowDecimal_ShowsDecimalKey_AndAppendsThePoint()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.AllowDecimal, true)
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Click(cut, "1");
        Click(cut, ".");
        Click(cut, "5");

        Assert.Equal("1.5", captured);
    }

    [Fact]
    public void AllowDecimal_RejectsASecondDecimalPoint()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.AllowDecimal, true)
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Click(cut, "1");
        Click(cut, ".");
        Click(cut, "2");
        Click(cut, ".");   // ignored — already has a decimal point
        Click(cut, "3");

        Assert.Equal("1.23", captured);
    }

    private static void Click(IRenderedComponent<MudKeyboard.Components.MudNumpad> cut, string label) =>
        cut.FindAll("button").Single(b => b.TextContent.Trim() == label).Click();
}
