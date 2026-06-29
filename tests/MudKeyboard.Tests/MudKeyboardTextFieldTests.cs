using Bunit;
using MudBlazor;
using MudKeyboard.Components;

namespace MudKeyboard.Tests;

/// <summary>
/// Covers the generic <see cref="MudKeyboardTextField{T}"/> wrapper: it must forward the common
/// MudTextField parameters and arbitrary attributes onto the real input, and emit the docked-keyboard
/// data-* markers (and only those) onto the element the focus-capture shim watches.
/// </summary>
public class MudKeyboardTextFieldTests : MudComponentTestContext
{
    [Fact]
    public void Default_MarksNothing_NoDataMudkeyboardAttributes()
    {
        var cut = Render<MudKeyboardTextField<string>>();
        var input = cut.Find("input");

        Assert.False(input.HasAttribute("data-mudkeyboard"));
        Assert.False(input.HasAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void DockedKeyboard_EmitsDataMudkeyboardOnTheInput()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p.Add(c => c.DockedKeyboard, true));

        Assert.Equal("true", cut.Find("input").GetAttribute("data-mudkeyboard"));
    }

    [Fact]
    public void DockedKeyboardLayout_EmitsDataMudkeyboardLayout()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p
            .Add(c => c.DockedKeyboard, true)
            .Add(c => c.DockedKeyboardLayout, "money"));

        Assert.Equal("money", cut.Find("input").GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void DockedKeyboardLayout_Empty_OmitsLayoutAttribute()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p.Add(c => c.DockedKeyboard, true));

        Assert.False(cut.Find("input").HasAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void DockedKeyboardLayout_WithoutDockedKeyboardFlag_StillEmitsLayoutButNotMarker()
    {
        // The layout marker is independent of the opt-in flag (useful in AllInputs mode).
        var cut = Render<MudKeyboardTextField<string>>(p => p.Add(c => c.DockedKeyboardLayout, "numpad"));
        var input = cut.Find("input");

        Assert.False(input.HasAttribute("data-mudkeyboard"));
        Assert.Equal("numpad", input.GetAttribute("data-mudkeyboard-layout"));
    }

    [Fact]
    public void PassthroughAttributes_AreForwardedToTheInput_AndMergedWithMarkers()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p
            .Add(c => c.DockedKeyboard, true)
            .AddUnmatched("name", "username")
            .AddUnmatched("autocomplete", "off"));
        var input = cut.Find("input");

        Assert.Equal("true", input.GetAttribute("data-mudkeyboard"));
        Assert.Equal("username", input.GetAttribute("name"));
        Assert.Equal("off", input.GetAttribute("autocomplete"));
    }

    [Fact]
    public void Label_IsRendered()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p.Add(c => c.Label, "Email address"));

        Assert.Contains("Email address", cut.Markup);
    }

    [Fact]
    public void InputType_Password_RendersAPasswordInput()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p.Add(c => c.InputType, InputType.Password));

        Assert.Equal("password", cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void Disabled_DisablesTheInput()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p.Add(c => c.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Value_IsRenderedIntoTheInput()
    {
        var cut = Render<MudKeyboardTextField<string>>(p => p.Add(c => c.Value, "hello"));

        Assert.Equal("hello", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void EditingTheInput_RaisesValueChanged()
    {
        string? captured = null;
        var cut = Render<MudKeyboardTextField<string>>(p => p
            .Add(c => c.Immediate, true)
            .Add(c => c.ValueChanged, (string v) => captured = v));

        cut.Find("input").Input("typed by user");

        Assert.Equal("typed by user", captured);
    }

    [Fact]
    public void GenericTypeParameter_BindsNonStringValues()
    {
        int captured = 0;
        var cut = Render<MudKeyboardTextField<int>>(p => p
            .Add(c => c.Value, 5)
            .Add(c => c.Immediate, true)
            .Add(c => c.ValueChanged, (int v) => captured = v));

        Assert.Equal("5", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Input("42");

        Assert.Equal(42, captured);
    }
}
