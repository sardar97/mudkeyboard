using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudKeyboard.Models;

namespace MudKeyboard.Tests;

/// <summary>
/// Fills out end-to-end click coverage for the inline <see cref="MudKeyboard.Components.MudKeyboard"/>:
/// backspace, space, escape, the shift one-shot/caps-lock distinction, the symbol toggle clearing shift,
/// and emit-mode command kinds. Keys are located by their <c>aria-label</c> so the assertions read like
/// the spoken control names.
/// </summary>
public class MudKeyboardBehaviorTests : MudComponentTestContext
{
    private static void Press(IRenderedComponent<MudKeyboard.Components.MudKeyboard> cut, string ariaLabel) =>
        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == ariaLabel).Click();

    private static bool HasKey(IRenderedComponent<MudKeyboard.Components.MudKeyboard> cut, string ariaLabel) =>
        cut.FindAll("button").Any(b => b.GetAttribute("aria-label") == ariaLabel);

    [Fact]
    public void Backspace_RemovesTheLastCharacter()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.Value, "ab")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Backspace");

        Assert.Equal("a", captured);
    }

    [Fact]
    public void Backspace_OnEmptyValue_DoesNotRaiseValueChanged()
    {
        var fired = false;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.ValueChanged, (string _) => fired = true));

        Press(cut, "Backspace");

        Assert.False(fired);
    }

    [Fact]
    public void Space_AppendsASpace()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.Value, "a")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Space");

        Assert.Equal("a ", captured);
    }

    [Fact]
    public void Escape_FiresOnEscape()
    {
        var escaped = false;
        var layout = new KeyboardLayout { Rows = new string[][] { ["a", KeyTokens.Escape] } };
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.Variant, KeyboardVariant.Custom)
            .Add(c => c.Layout, layout)
            .Add(c => c.OnEscape, () => escaped = true));

        Press(cut, "Escape");

        Assert.True(escaped);
    }

    [Fact]
    public void SingleClickShift_IsOneShot_UppercasesThenReleases()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Shift"); // arm one-shot
        Press(cut, "A");      // shifted letter renders/announces as "A"

        Assert.Equal("A", captured);
        // Released after the single press: the letter is lowercase again and shift reads not-pressed.
        Assert.True(HasKey(cut, "a"));
        var shift = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Shift");
        Assert.Equal("false", shift.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void DoubleClickShift_LocksCaps_AndStaysLockedAfterTyping()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.ValueChanged, (string v) => captured = v));

        var shift = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Shift");
        shift.TriggerEvent("ondblclick", new MouseEventArgs()); // lock caps

        Press(cut, "A"); // uppercase while locked
        Assert.Equal("A", captured);

        // Caps lock persists (a one-shot would have reset): the key reads "Caps lock" + pressed, and
        // letters keep rendering uppercase.
        var caps = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Caps lock");
        Assert.Equal("true", caps.GetAttribute("aria-pressed"));
        Assert.True(HasKey(cut, "A"));
    }

    [Fact]
    public void SymbolToggle_ClearsAnArmedShiftWhenReturningToLetters()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>();

        Press(cut, "Shift");                  // arm shift on the letter face
        Press(cut, "Numbers and symbols");    // flip to symbols (clears shift)
        Press(cut, "Letters");                // flip back to letters

        // Shift was cleared on the flip, so letters are lowercase again.
        Assert.True(HasKey(cut, "a"));
        Assert.False(HasKey(cut, "A"));
    }

    [Theory]
    [InlineData("Backspace", KeyboardInputKind.Backspace)]
    [InlineData("Enter", KeyboardInputKind.Enter)]
    public void EmitMode_CommandKeys_RaiseTheMatchingInputKind(string ariaLabel, KeyboardInputKind expected)
    {
        KeyboardInput emitted = default;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.OnInput, (KeyboardInput i) => emitted = i));

        Press(cut, ariaLabel);

        Assert.Equal(expected, emitted.Kind);
    }

    [Fact]
    public void EmitMode_Space_EmitsASpaceCharacter()
    {
        KeyboardInput emitted = default;
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.OnInput, (KeyboardInput i) => emitted = i));

        Press(cut, "Space");

        Assert.Equal(KeyboardInputKind.Text, emitted.Kind);
        Assert.Equal(" ", emitted.Text);
    }

    [Fact]
    public void EmitMode_Escape_EmitsEscape()
    {
        KeyboardInput emitted = default;
        var layout = new KeyboardLayout { Rows = new string[][] { ["a", KeyTokens.Escape] } };
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.Variant, KeyboardVariant.Custom)
            .Add(c => c.Layout, layout)
            .Add(c => c.OnInput, (KeyboardInput i) => emitted = i));

        Press(cut, "Escape");

        Assert.Equal(KeyboardInputKind.Escape, emitted.Kind);
    }
}

/// <summary>Extra <see cref="MudKeyboard.Components.MudNumpad"/> coverage: backspace, the "00" key, max length and disabled.</summary>
public class MudNumpadBehaviorTests : MudComponentTestContext
{
    private static void Press(IRenderedComponent<MudKeyboard.Components.MudNumpad> cut, string ariaLabel) =>
        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == ariaLabel).Click();

    [Fact]
    public void Backspace_RemovesTheLastDigit()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.Value, "12")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Backspace");

        Assert.Equal("1", captured);
    }

    [Fact]
    public void DoubleZeroKey_AppendsTwoZeros()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.Value, "5")
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "Double zero");

        Assert.Equal("500", captured);
    }

    [Fact]
    public void MaxLength_StopsAppendingPastTheCap()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p
            .Add(c => c.MaxLength, 2)
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "1");
        Press(cut, "2");
        Press(cut, "3"); // capped — value unchanged, no further ValueChanged

        Assert.Equal("12", captured);
    }

    [Fact]
    public void Disabled_DisablesEveryKey()
    {
        var cut = Render<MudKeyboard.Components.MudNumpad>(p => p.Add(c => c.Disabled, true));

        Assert.NotEmpty(cut.FindAll("button"));
        Assert.All(cut.FindAll("button"), b => Assert.True(b.HasAttribute("disabled")));
    }
}

/// <summary>Extra <see cref="MudKeyboard.Components.MudPricepad"/> coverage: backspace, "00", max length and decimal places.</summary>
public class MudPricepadBehaviorTests : MudComponentTestContext
{
    private static void Press(IRenderedComponent<MudKeyboard.Components.MudPricepad> cut, string ariaLabel) =>
        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == ariaLabel).Click();

    [Fact]
    public void Backspace_RemovesTheLastEnteredDigit_PenceFirst()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "5"); // £0.05
        Press(cut, "2"); // £0.52
        Press(cut, "3"); // £5.23
        Press(cut, "Backspace"); // back to £0.52

        Assert.Equal("£0.52", captured);
    }

    [Fact]
    public void DoubleZeroKey_ShiftsInTwoPenceDigits()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "5");           // £0.05
        Press(cut, "Double zero"); // digits 5 → 500 → £5.00

        Assert.Equal("£5.00", captured);
    }

    [Fact]
    public void MaxLength_CapsTheFormattedDigitRun()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.MaxLength, 3)
            .Add(c => c.ValueChanged, (string v) => captured = v));

        // MaxLength counts the formatted, zero-padded digit run: "£0.01" already holds the digits
        // "001" (3), so with a cap of 3 the next keystroke would overflow and is rejected.
        Press(cut, "1"); // £0.01  → digit run "001" (already at the cap of 3)
        Press(cut, "2"); // rejected: appending would make a 4th digit, so the value is unchanged

        Assert.Equal("£0.01", captured);
    }

    [Fact]
    public void DecimalPlacesZero_FormatsAsWholeCurrency()
    {
        string? captured = null;
        var cut = Render<MudKeyboard.Components.MudPricepad>(p => p
            .Add(c => c.DecimalPlaces, 0)
            .Add(c => c.ValueChanged, (string v) => captured = v));

        Press(cut, "5");

        Assert.Equal("£5", captured);
    }
}
