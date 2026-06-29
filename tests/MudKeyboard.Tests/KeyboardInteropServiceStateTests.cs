using Microsoft.JSInterop;
using MudKeyboard.Layouts;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

/// <summary>
/// The focus state machine driven by JS callbacks, exercised without a real JS module: the interop
/// methods that forward to JS must no-op safely before <c>InitializeAsync</c> has loaded the module.
/// </summary>
public class KeyboardInteropServiceStateTests
{
    private static KeyboardInteropService NewService() =>
        new(new NoopJsRuntime(), new MudKeyboardOptions());

    [Fact]
    public void OnFocusIn_OpensWithLayoutSymbolFaceAndZIndex_AndNotifies()
    {
        var service = NewService();
        var notifications = 0;
        service.StateChanged += () => notifications++;

        service.OnFocusIn("qwerty", 1500);

        Assert.True(service.IsOpen);
        Assert.Same(LayoutLibrary.Qwerty, service.CurrentLayout);
        Assert.Same(LayoutLibrary.Symbols, service.CurrentSymbolLayout);
        Assert.Equal(1500, service.PageMaxZIndex);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void OnFocusIn_NumpadKind_HasNoSymbolFace()
    {
        var service = NewService();

        service.OnFocusIn("numpad", 0);

        Assert.Same(LayoutLibrary.Numpad, service.CurrentLayout);
        Assert.Null(service.CurrentSymbolLayout);
    }

    [Fact]
    public void OnFocusOut_WhenOpen_ClosesAndNotifies()
    {
        var service = NewService();
        service.OnFocusIn("qwerty", 0);
        var notifications = 0;
        service.StateChanged += () => notifications++;

        service.OnFocusOut();

        Assert.False(service.IsOpen);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void OnFocusOut_WhenAlreadyClosed_IsANoOp()
    {
        var service = NewService();
        var notifications = 0;
        service.StateChanged += () => notifications++;

        service.OnFocusOut();

        Assert.False(service.IsOpen);
        Assert.Equal(0, notifications);
    }

    [Fact]
    public async Task InteropCalls_BeforeInitialize_DoNotThrow()
    {
        var service = NewService();

        // No module is loaded (InitializeAsync was never called), so every forwarder must be a safe no-op.
        await service.InsertTextAsync("a");
        await service.BackspaceAsync();
        await service.EnterAsync();
        await service.ClearAsync();
        await service.CopyAsync();
        await service.PasteAsync();
        await service.MoveCaretAsync(-1);
        await service.AppendMoneyDigitsAsync("5");
        await service.BackspaceMoneyAsync();
        await service.CloseAsync();

        Assert.False(service.IsOpen);
    }

    [Fact]
    public async Task DisposeAsync_WithoutInitialize_DoesNotThrow()
    {
        var service = NewService();

        await service.DisposeAsync();
    }

    // A JS runtime that returns defaults. The module is never loaded in these tests, so its members
    // are not actually invoked — this just satisfies the constructor dependency.
    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult<TValue>(default!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult<TValue>(default!);
    }
}
