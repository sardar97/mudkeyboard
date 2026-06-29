using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudKeyboard.Components;
using MudKeyboard.Extensions;
using MudKeyboard.Models;
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

    private (IRenderedComponent<MudKeyboardHost> Cut, KeyboardInteropService Interop) RenderHost(
        Action<ComponentParameterCollectionBuilder<MudKeyboardHost>>? parameters = null)
    {
        var cut = parameters is null ? Render<MudKeyboardHost>() : Render(parameters);
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
    public void VisibleActions_HidingButtons_RemovesThemFromTheToolbar()
    {
        // Hide Copy and Paste; everything else stays. ShowCancel is off so the Hide button is also present.
        var (cut, interop) = RenderHost(p => p
            .Add(c => c.ShowCancel, false)
            .Add(c => c.VisibleActions, KeyboardAction.All & ~(KeyboardAction.Copy | KeyboardAction.Paste)));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.DoesNotContain("Copy", titles);
            Assert.DoesNotContain("Paste", titles);
            Assert.Contains("Clear", titles);
            Assert.Contains("Hide keyboard", titles);
        });
    }

    [Fact]
    public void VisibleActions_None_RendersNoToolbarButKeepsTheKeys()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.VisibleActions, KeyboardAction.None));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            // Panel is open with its keys, but the action toolbar is gone entirely.
            Assert.Contains("mudkeyboard-dock--open", cut.Markup);
            Assert.DoesNotContain("mudkeyboard-dock__bar", cut.Markup);
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.DoesNotContain("Clear", titles);
            Assert.DoesNotContain("Hide keyboard", titles);
        });
    }

    [Fact]
    public void DisabledActions_GreysOutTheButtonButKeepsItVisible()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.DisabledActions, KeyboardAction.Clear));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var buttons = cut.FindAll("button");
            var clear = buttons.Single(b => b.GetAttribute("title") == "Clear");
            var copy = buttons.Single(b => b.GetAttribute("title") == "Copy");
            // Clear is rendered but disabled; Copy is untouched.
            Assert.True(clear.HasAttribute("disabled"));
            Assert.False(copy.HasAttribute("disabled"));
        });
    }

    [Fact]
    public void HidingWinsOverDisabling_WhenAButtonIsInBothSets()
    {
        var (cut, interop) = RenderHost(p => p
            .Add(c => c.VisibleActions, KeyboardAction.All & ~KeyboardAction.Clear)
            .Add(c => c.DisabledActions, KeyboardAction.Clear));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.DoesNotContain("Clear", titles);
        });
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

    [Fact]
    public void ShowValuePreview_ShowsTheFocusedFieldsCurrentValueAtTheTop()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.ShowValuePreview, true));

        // Focusing a field that already contains "sardar" seeds the preview bar with it.
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));

        cut.WaitForAssertion(() =>
            Assert.Equal("sardar", cut.Find(".mudkeyboard-dock__preview-text").TextContent.Trim()));
    }

    [Fact]
    public void ShowValuePreview_PreviewBarTracksLiveValueChanges()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.ShowValuePreview, true));
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sar"));

        // A value change reported from JS flows into the preview bar.
        cut.InvokeAsync(() => interop.OnValueChanged("sardar"));

        cut.WaitForAssertion(() =>
            Assert.Equal("sardar", cut.Find(".mudkeyboard-dock__preview-text").TextContent.Trim()));
    }

    [Fact]
    public void WithoutShowValuePreview_NoPreviewBarIsRendered()
    {
        // The preview bar is on by default now, so turn it off explicitly for this test.
        var (cut, interop) = RenderHost(p => p.Add(c => c.ShowValuePreview, false));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("mudkeyboard-dock--open", cut.Markup);
            Assert.Empty(cut.FindAll(".mudkeyboard-dock__preview"));
        });
    }

    [Fact]
    public void ShowBackdrop_RendersTheBackdrop_OnlyWhileOpen()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.ShowBackdrop, true));

        // Closed: no backdrop.
        Assert.Empty(cut.FindAll(".mudkeyboard-backdrop"));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mudkeyboard-backdrop")));
    }

    [Fact]
    public void BackdropClick_CancelsAndClosesThePanel()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.ShowBackdrop, true));
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));
        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));

        cut.Find(".mudkeyboard-backdrop").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("mudkeyboard-dock--open", cut.Markup));
    }

    [Fact]
    public void DisableBackdropClick_KeepsThePanelOpenOnBackdropClick()
    {
        var (cut, interop) = RenderHost(p => p
            .Add(c => c.ShowBackdrop, true)
            .Add(c => c.DisableBackdropClick, true));
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));
        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));

        // The backdrop click no longer closes; the user must press Enter or the Cancel key instead.
        cut.Find(".mudkeyboard-backdrop").Click();

        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));
    }

    [Fact]
    public void CancelKey_IsShownNextToEnterByDefault()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));

        cut.WaitForAssertion(() =>
        {
            var labels = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
            Assert.Contains("Cancel", labels);
            Assert.Contains("⏎", labels);
        });
    }

    [Fact]
    public void CancelKey_ClosesThePanel()
    {
        var (cut, interop) = RenderHost();
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));
        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));

        // The Cancel key (next to Enter) reverts to the focus-in value and closes the keyboard.
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("mudkeyboard-dock--open", cut.Markup));
    }

    [Fact]
    public void ShowCancelFalse_DropsTheCancelKey_AndRestoresTheHideButton()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.ShowCancel, false));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Cancel");
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.Contains("Hide keyboard", titles);
        });
    }

    [Fact]
    public void HideButton_IsSuppressed_WhileCancelKeyIsShown()
    {
        var (cut, interop) = RenderHost();

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll("button").Select(b => b.GetAttribute("title")).ToList();
            Assert.DoesNotContain("Hide keyboard", titles);
        });
    }

    [Fact]
    public void CustomCancelLabel_IsRenderedOnTheCancelKey()
    {
        var (cut, interop) = RenderHost(p => p.Add(c => c.CancelLabel, "Abandon"));

        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Abandon"));
    }

    [Fact]
    public void ShowValuePreview_RendersACaret_SplittingTheValueAtTheCaretPosition()
    {
        var (cut, interop) = RenderHost();

        // Caret at offset 3 of "sardar": preview shows "sar" + caret + "dar".
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000, "sardar"));
        cut.InvokeAsync(() => interop.OnValueChanged("sardar", 3));

        cut.WaitForAssertion(() =>
        {
            var caret = cut.Find(".mudkeyboard-dock__caret");
            Assert.Equal("sar", caret.PreviousSibling?.TextContent);
            Assert.Equal("dar", caret.NextSibling?.TextContent);
        });
    }
}
