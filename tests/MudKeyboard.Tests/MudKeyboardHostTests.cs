using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudKeyboard.Components;
using MudKeyboard.Extensions;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

/// <summary>
/// Guards the docked-keyboard host when it is placed in its own interactive island (in App.razor,
/// outside &lt;Routes&gt;) so it works on static-SSR pages. In that position it has no MudBlazor
/// providers as ancestors, so opening the panel must still render without tearing down the circuit.
/// </summary>
/// <remarks>
/// Implements <see cref="IAsyncLifetime"/> so xUnit tears the context down via bUnit's async disposal
/// path — the host pulls in the <see cref="KeyboardInteropService"/>, which is async-disposable, and
/// bUnit's synchronous <c>Dispose</c> rejects async-only services.
/// </remarks>
public class MudKeyboardHostTests : MudComponentTestContext, IAsyncLifetime
{
    public MudKeyboardHostTests() => Services.AddMudKeyboard();

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private (IRenderedComponent<MudKeyboardHost> Cut, KeyboardInteropService Interop) RenderHost()
    {
        var cut = Render<MudKeyboardHost>();
        return (cut, Services.GetRequiredService<KeyboardInteropService>());
    }

    [Fact]
    public void BeforeAnyFocus_PanelIsClosed()
    {
        var cut = Render<MudKeyboardHost>();

        Assert.DoesNotContain("mudkeyboard-dock--open", cut.Markup);
    }

    [Fact]
    public void OpeningDockedPanel_AsAStandaloneIsland_DoesNotThrow()
    {
        var (cut, interop) = RenderHost();

        // Simulate a JS focus callback: this flips IsOpen and renders the open panel (toolbar with
        // MudIconButtons plus the MudKeyboard surface). It must not throw for want of a MudPopoverProvider.
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));
    }

    [Fact]
    public void OpenPanel_UsesNativeTitleTooltips_NotMudTooltip()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.Contains("Clear", titles);
            Assert.Contains("Copy", titles);
            Assert.Contains("Paste", titles);
            Assert.Contains("Hide keyboard", titles);
            // No MudTooltip means no popover markup — which is exactly why no MudPopoverProvider is needed.
            Assert.DoesNotContain("mud-tooltip", cut.Markup);
        });
    }

    [Fact]
    public void OpenPanel_Qwerty_ShowsCursorArrows()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.Contains("Move cursor left", titles);
            Assert.Contains("Move cursor right", titles);
        });
    }

    [Fact]
    public void OpenPanel_MoneyLayout_HidesCursorArrows()
    {
        var (cut, interop) = RenderHost();

        // Pence-first money entry always appends at the end, so the cursor controls are suppressed.
        cut.InvokeAsync(() => interop.OnFocusIn("money", 1000));

        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.Contains("Clear", titles);
            Assert.DoesNotContain("Move cursor left", titles);
            Assert.DoesNotContain("Move cursor right", titles);
        });
    }

    [Fact]
    public void OnFocusOut_ClosesThePanel()
    {
        var (cut, interop) = RenderHost();
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));
        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));

        cut.InvokeAsync(() => interop.OnFocusOut());

        cut.WaitForAssertion(() => Assert.DoesNotContain("mudkeyboard-dock--open", cut.Markup));
    }

    [Fact]
    public void PressingEnterInThePanel_ClosesTheKeyboard()
    {
        var (cut, interop) = RenderHost();
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));
        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));

        // The host runs the inner keyboard in emit mode; Enter commits then closes the panel.
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "⏎").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("mudkeyboard-dock--open", cut.Markup));
    }
}
