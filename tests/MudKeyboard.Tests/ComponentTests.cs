using Bunit;
using MudBlazor.Services;
using MudKeyboard.Components;
using MudKeyboard.Models;

namespace MudKeyboard.Tests;

/// <summary>
/// bUnit component tests. MudBlazor needs its services registered and a loose JS interop
/// (MudButton's ripple etc. call into JS, which we no-op in the test renderer).
/// </summary>
public abstract class MudComponentTestContext : BunitContext, IAsyncLifetime
{
    protected MudComponentTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    // Some MudBlazor components (for example MudNumericField) register services that implement ONLY
    // IAsyncDisposable, such as KeyInterceptorService. bUnit's synchronous Dispose() cannot tear those
    // down — the DI container throws "type only implements IAsyncDisposable". Disposing the context
    // asynchronously here lets the container dispose them correctly; the subsequent synchronous Dispose
    // then finds an already-disposed provider and is a no-op.
    async Task IAsyncLifetime.DisposeAsync() => await ((IAsyncDisposable)this).DisposeAsync();
}

public class MudNumpadComponentTests : MudComponentTestContext
{
    [Fact]
    public void PressingDigit_FiresValueChangedWithThatDigit()
    {
        string? captured = null;
        var cut = Render<MudNumpad>(p => p
            .Add(c => c.ValueChanged, v => captured = v));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "7").Click();

        Assert.Equal("7", captured);
    }

    [Fact]
    public void PressingDigitsInSequence_AccumulatesValue()
    {
        string? captured = null;
        var cut = Render<MudNumpad>(p => p
            .Add(c => c.ValueChanged, v => captured = v));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "1").Click();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "2").Click();

        Assert.Equal("12", captured);
    }

    [Fact]
    public void PressingEnter_FiresOnEnter_NotValueChanged()
    {
        var enterFired = false;
        var valueChangedFired = false;
        var cut = Render<MudNumpad>(p => p
            .Add(c => c.OnEnter, () => enterFired = true)
            .Add(c => c.ValueChanged, _ => valueChangedFired = true));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "⏎").Click();

        Assert.True(enterFired);
        Assert.False(valueChangedFired);
    }
}

public class MudKeyboardCustomLayoutTests : MudComponentTestContext
{
    [Fact]
    public void CustomLayout_RendersCorrectNumberOfRowsAndKeys()
    {
        var layout = new KeyboardLayout
        {
            Rows = new string[][]
            {
                ["a", "b", "c"],
                ["d", "e"],
                [KeyTokens.Space, KeyTokens.Enter],
            },
        };

        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p
            .Add(c => c.Variant, KeyboardVariant.Custom)
            .Add(c => c.Layout, layout));

        Assert.Equal(3, cut.FindAll(".mudkeyboard-row").Count);
        Assert.Equal(7, cut.FindAll("button").Count); // 3 + 2 + 2
    }
}
