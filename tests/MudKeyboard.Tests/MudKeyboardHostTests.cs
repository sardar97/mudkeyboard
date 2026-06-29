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

    [Fact]
    public void OpeningDockedPanel_AsAStandaloneIsland_DoesNotThrow()
    {
        var cut = Render<MudKeyboardHost>();
        var interop = Services.GetRequiredService<KeyboardInteropService>();

        // Simulate a JS focus callback: this flips IsOpen and renders the open panel (toolbar with
        // MudTooltip/MudIconButton plus the MudKeyboard surface).
        cut.InvokeAsync(() => interop.OnFocusIn("qwerty", 1000));

        cut.WaitForAssertion(() => Assert.Contains("mudkeyboard-dock--open", cut.Markup));
    }
}
