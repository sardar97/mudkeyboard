using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudKeyboard.Components;
using MudKeyboard.Extensions;
using MudKeyboard.Models;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

/// <summary>
/// Pure tests for <see cref="KeyboardKey.AccessibleLabel"/> — the spoken-friendly name surfaced as the
/// rendered key's <c>aria-label</c>. No rendering needed: it is a side-effect-free projection of the key.
/// </summary>
public class KeyboardKeyAccessibleLabelTests
{
    [Theory]
    [InlineData("{bksp}", "Backspace")]
    [InlineData("{enter}", "Enter")]
    [InlineData("{space}", "Space")]
    [InlineData("{shift}", "Shift")]
    [InlineData("{caps}", "Caps lock")]
    [InlineData("{sym}", "Numbers and symbols")]
    [InlineData("{esc}", "Escape")]
    public void CommandTokens_GetReadableNames(string token, string expected) =>
        Assert.Equal(expected, KeyboardKey.FromToken(token).AccessibleLabel);

    [Theory]
    [InlineData("a")]
    [InlineData("Z")]
    [InlineData("5")]
    [InlineData("£")]
    [InlineData("@")]
    public void LiteralTokens_FallBackToTheVisibleGlyph(string token) =>
        Assert.Equal(token, KeyboardKey.FromToken(token).AccessibleLabel);

    [Fact]
    public void DoubleZeroKey_ReadsAsDoubleZero() =>
        Assert.Equal("Double zero", KeyboardKey.FromToken("00").AccessibleLabel);

    [Fact]
    public void DecimalPointKey_ReadsAsDecimalPoint() =>
        Assert.Equal("Decimal point", KeyboardKey.FromToken(".").AccessibleLabel);

    [Fact]
    public void Shift_WhenLockedGlyphIsShown_ReadsAsCapsLock()
    {
        // The engine swaps the shift glyph to ⇪ once caps lock engages; the spoken name follows suit.
        var locked = KeyboardKey.FromToken(KeyTokens.Shift) with { DisplayLabel = "⇪" };

        Assert.Equal("Caps lock", locked.AccessibleLabel);
    }

    [Fact]
    public void SymbolToggle_WhileSymbolsAreShown_ReadsAsLetters()
    {
        var onSymbolFace = KeyboardKey.FromToken(KeyTokens.SymbolToggle) with { DisplayLabel = "ABC" };

        Assert.Equal("Letters", onSymbolFace.AccessibleLabel);
    }

    [Fact]
    public void ShiftedLetter_ReadsAsItsUppercaseGlyph()
    {
        var shiftedA = KeyboardKey.FromToken("a") with { DisplayLabel = "A", Bold = true };

        Assert.Equal("A", shiftedA.AccessibleLabel);
    }
}

/// <summary>
/// Verifies the inline keyboards expose a labelled grouping role and that every key carries an
/// <c>aria-label</c> (and toggle keys an <c>aria-pressed</c>) so assistive tech can announce them —
/// the on-screen glyphs (⌫, ⏎, the blank space bar) are meaningless to a screen reader on their own.
/// </summary>
public class KeyboardAccessibilityMarkupTests : MudComponentTestContext
{
    private static string? AriaLabel(IRenderedComponent<MudKeyboard.Components.MudKeyboard> cut) =>
        cut.Find("div[role='group']").GetAttribute("aria-label");

    [Fact]
    public void MudKeyboard_Root_HasGroupRoleWithDefaultAccessibleName() =>
        Assert.Equal("On-screen keyboard", AriaLabel(Render<MudKeyboard.Components.MudKeyboard>()));

    [Fact]
    public void MudKeyboard_AriaLabel_OverridesTheDefault()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p.Add(c => c.AriaLabel, "PIN entry"));

        Assert.Equal("PIN entry", AriaLabel(cut));
    }

    [Fact]
    public void MudNumpad_Root_HasGroupRoleWithDefaultAccessibleName()
    {
        var cut = Render<MudNumpad>();

        var group = cut.Find("div[role='group']");
        Assert.Equal("Numeric keypad", group.GetAttribute("aria-label"));
    }

    [Fact]
    public void MudPricepad_Root_HasGroupRoleWithDefaultAccessibleName()
    {
        var cut = Render<MudPricepad>();

        var group = cut.Find("div[role='group']");
        Assert.Equal("Price entry keypad", group.GetAttribute("aria-label"));
    }

    [Fact]
    public void EveryKey_HasANonEmptyAriaLabel()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>();

        var buttons = cut.FindAll("button");
        Assert.NotEmpty(buttons);
        Assert.All(buttons, b => Assert.False(string.IsNullOrWhiteSpace(b.GetAttribute("aria-label"))));
    }

    [Fact]
    public void GlyphCommandKeys_AreLabelledWithWords()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>();
        var labels = cut.FindAll("button").Select(b => b.GetAttribute("aria-label")).ToList();

        Assert.Contains("Backspace", labels);
        Assert.Contains("Enter", labels);
        Assert.Contains("Space", labels);     // the blank space bar would otherwise announce as nothing
        Assert.Contains("Shift", labels);
    }

    [Fact]
    public void ShiftKey_ExposesPressedState_AndTracksIt()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>();

        var shift = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Shift");
        Assert.Equal("false", shift.GetAttribute("aria-pressed"));

        shift.Click(); // one-shot shift armed
        var armed = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Shift");
        Assert.Equal("true", armed.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void SymbolToggle_ExposesPressedState_AndRelabelsWithTheFace()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>();

        var toggle = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Numbers and symbols");
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));

        toggle.Click(); // flip to the symbols face

        var flipped = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Letters");
        Assert.Equal("true", flipped.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void NonToggleKeys_DoNotCarryAriaPressed()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>();

        var letter = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "a");
        Assert.False(letter.HasAttribute("aria-pressed"));

        var enter = cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Enter");
        Assert.False(enter.HasAttribute("aria-pressed"));
    }
}

/// <summary>
/// Accessibility guarantees for the docked <see cref="MudKeyboardHost"/>: while closed it must be
/// removed from the tab order and the accessibility tree (<c>inert</c> + <c>aria-hidden</c>), and when
/// open it must expose proper grouping/toolbar roles and labelled action buttons.
/// </summary>
public class MudKeyboardHostAccessibilityTests : MudComponentTestContext, IAsyncLifetime
{
    public MudKeyboardHostAccessibilityTests() => Services.AddMudKeyboard();

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private (IRenderedComponent<MudKeyboardHost> Cut, KeyboardInteropService Interop) RenderHost()
    {
        var cut = Render<MudKeyboardHost>();
        return (cut, Services.GetRequiredService<KeyboardInteropService>());
    }

    [Fact]
    public void Dock_HasGroupRoleAndAccessibleName()
    {
        var dock = Render<MudKeyboardHost>().Find(".mudkeyboard-dock");

        Assert.Equal("group", dock.GetAttribute("role"));
        Assert.Equal("On-screen keyboard", dock.GetAttribute("aria-label"));
    }

    [Fact]
    public void ClosedDock_IsInertAndHiddenFromAssistiveTech()
    {
        var dock = Render<MudKeyboardHost>().Find(".mudkeyboard-dock");

        Assert.True(dock.HasAttribute("inert"));
        Assert.Equal("true", dock.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void OpenDock_DropsInertAndAriaHidden()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var dock = cut.Find(".mudkeyboard-dock");
            Assert.False(dock.HasAttribute("inert"));
            Assert.False(dock.HasAttribute("aria-hidden"));
        });
    }

    [Fact]
    public void OpenDock_ActionBar_HasToolbarRoleAndName()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var bar = cut.Find(".mudkeyboard-dock__bar");
            Assert.Equal("toolbar", bar.GetAttribute("role"));
            Assert.Equal("Keyboard actions", bar.GetAttribute("aria-label"));
        });
    }

    [Fact]
    public void OpenDock_ToolbarButtons_AllCarryAriaLabels()
    {
        // ShowCancel is on by default and suppresses the Hide button, so turn it off here to exercise the
        // full toolbar (Clear … Hide) and confirm every button carries an aria-label.
        var cut = Render<MudKeyboardHost>(p => p.Add(c => c.ShowCancel, false));
        var interop = Services.GetRequiredService<KeyboardInteropService>();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var labels = cut.FindAll(".mudkeyboard-dock__bar button")
                .Select(b => b.GetAttribute("aria-label"))
                .ToList();
            Assert.All(labels, l => Assert.False(string.IsNullOrWhiteSpace(l)));
            Assert.Contains("Clear field", labels);
            Assert.Contains("Hide keyboard", labels);
        });
    }

    [Fact]
    public void OpenDock_InnerKeyboard_HasItsOwnAccessibleName()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            // The dock groups everything as "On-screen keyboard"; the keys themselves are "Keyboard keys".
            var labels = cut.FindAll("[role='group']").Select(g => g.GetAttribute("aria-label")).ToList();
            Assert.Contains("On-screen keyboard", labels);
            Assert.Contains("Keyboard keys", labels);
        });
    }

    [Fact]
    public void DockedKeys_CarryReadableAriaLabels()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var labels = cut.FindAll(".mudkeyboard-grid button")
                .Select(b => b.GetAttribute("aria-label"))
                .ToList();
            Assert.Contains("Space", labels);
            Assert.Contains("Backspace", labels);
            Assert.All(labels, l => Assert.False(string.IsNullOrWhiteSpace(l)));
        });
    }
}

/// <summary>
/// Accessibility of the numeric keypads the type-detection feature surfaces on the docked keyboard
/// (money for <c>decimal</c>, the decimal-point pad for <c>double</c>/<c>float</c>, the plain pad for
/// integers). These layouts have no visible text on several keys (⌫, ⏎, the "00" and "." keys), so each
/// must carry a spoken <c>aria-label</c> and the panel must keep its grouping/toolbar semantics.
/// </summary>
public class DockedNumericKeypadAccessibilityTests : MudComponentTestContext, IAsyncLifetime
{
    public DockedNumericKeypadAccessibilityTests() => Services.AddMudKeyboard();

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private List<string?> OpenAndReadKeyLabels(string layoutKind)
    {
        var cut = Render<MudKeyboardHost>();
        var interop = Services.GetRequiredService<KeyboardInteropService>();

        cut.InvokeAsync(() => interop.OnFocusIn(layoutKind, 1000));

        List<string?> labels = [];
        cut.WaitForAssertion(() =>
        {
            labels = cut.FindAll(".mudkeyboard-grid button")
                .Select(b => b.GetAttribute("aria-label"))
                .ToList();
            Assert.NotEmpty(labels);
        });
        return labels;
    }

    [Theory]
    [InlineData("money")]
    [InlineData("decimal")]
    [InlineData("numpad")]
    public void EveryKeyOnEveryNumericKeypad_HasANonEmptyAriaLabel(string layoutKind) =>
        Assert.All(OpenAndReadKeyLabels(layoutKind), l => Assert.False(string.IsNullOrWhiteSpace(l)));

    [Theory]
    [InlineData("money")]
    [InlineData("decimal")]
    [InlineData("numpad")]
    public void EveryNumericKeypad_LabelsItsGlyphlessCommandKeysWithWords(string layoutKind)
    {
        var labels = OpenAndReadKeyLabels(layoutKind);

        // ⌫, ⏎ and the "00" key have no meaningful spoken text on their own.
        Assert.Contains("Backspace", labels);
        Assert.Contains("Enter", labels);
        Assert.Contains("Double zero", labels);
    }

    [Fact]
    public void DecimalKeypad_HasASpokenDecimalPointKey()
    {
        var labels = OpenAndReadKeyLabels("decimal");

        Assert.Contains("Decimal point", labels);
    }

    [Theory]
    // The money pad (pence-first) and the integer pad have no decimal-point key — so none should be
    // announced. This guards both the layout choice and its accessible projection at once.
    [InlineData("money")]
    [InlineData("numpad")]
    public void MoneyAndIntegerKeypads_DoNotExposeADecimalPointKey(string layoutKind)
    {
        var labels = OpenAndReadKeyLabels(layoutKind);

        Assert.DoesNotContain("Decimal point", labels);
    }

    [Theory]
    [InlineData("money")]
    [InlineData("decimal")]
    [InlineData("numpad")]
    public void NumericDock_KeepsGroupAndToolbarSemantics(string layoutKind)
    {
        var cut = Render<MudKeyboardHost>();
        var interop = Services.GetRequiredService<KeyboardInteropService>();

        cut.InvokeAsync(() => interop.OnFocusIn(layoutKind, 1000));

        cut.WaitForAssertion(() =>
        {
            var dock = cut.Find(".mudkeyboard-dock");
            Assert.Equal("group", dock.GetAttribute("role"));
            Assert.False(dock.HasAttribute("inert"));            // open → reachable
            Assert.False(dock.HasAttribute("aria-hidden"));

            var bar = cut.Find(".mudkeyboard-dock__bar");
            Assert.Equal("toolbar", bar.GetAttribute("role"));

            // The inner keypad is its own labelled group, and every action button is labelled.
            var groupLabels = cut.FindAll("[role='group']").Select(g => g.GetAttribute("aria-label")).ToList();
            Assert.Contains("Keyboard keys", groupLabels);
            Assert.All(
                cut.FindAll(".mudkeyboard-dock__bar button").Select(b => b.GetAttribute("aria-label")),
                l => Assert.False(string.IsNullOrWhiteSpace(l)));
        });
    }
}
