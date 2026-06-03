using Microsoft.JSInterop;
using MudKeyboard.Layouts;
using MudKeyboard.Models;

namespace MudKeyboard.Services;

/// <summary>
/// The bridge between the JavaScript focus-capture shim (<c>mudKeyboard.js</c>) and the docked
/// keyboard UI (<see cref="MudKeyboard.Components.MudKeyboardHost"/>). It owns the loaded JS module,
/// receives focus callbacks from the browser, tracks which layout the docked keyboard should show,
/// and forwards key presses back to JS so they land at the focused field's caret.
/// </summary>
/// <remarks>
/// This is the one place in the library that performs JS interop. It uses constructor injection for
/// <see cref="IJSRuntime"/> (per the project's DI rules) and only ever passes primitive strings across
/// the interop boundary, so it stays AOT/trim-safe (no reflection-based serialisation).
/// </remarks>
public sealed class KeyboardInteropService : IAsyncDisposable
{
    private const string ModulePath = "./_content/MudKeyboard/mudKeyboard.js";

    private readonly IJSRuntime _js;
    private readonly MudKeyboardOptions _options;
    private IJSObjectReference? _module;
    private DotNetObjectReference<KeyboardInteropService>? _selfRef;

    /// <summary>Creates the service. <paramref name="js"/> and <paramref name="options"/> are injected.</summary>
    /// <param name="js">The JS runtime used to load and call the focus-capture module.</param>
    /// <param name="options">Global keyboard configuration.</param>
    public KeyboardInteropService(IJSRuntime js, MudKeyboardOptions options)
    {
        _js = js;
        _options = options;
    }

    /// <summary>Raised whenever <see cref="IsOpen"/> or the active layout changes, so the host can re-render.</summary>
    public event Action? StateChanged;

    /// <summary>Whether the docked keyboard should currently be shown.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The base layout to show for the focused field, or <see langword="null"/> before first focus.</summary>
    public KeyboardLayout? CurrentLayout { get; private set; }

    /// <summary>The symbol/numbers face paired with <see cref="CurrentLayout"/>, when it has one.</summary>
    public KeyboardLayout? CurrentSymbolLayout { get; private set; }

    /// <summary>
    /// Loads the JS module and starts global focus capture. Safe to call repeatedly — only the first
    /// call does work. Invoke from the host's first interactive render.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _selfRef = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initialize", _selfRef, _options.AttachMode.ToString());
    }

    /// <summary>Called from JS when an attachable field gains focus. <paramref name="layoutKind"/> is the inferred face.</summary>
    /// <param name="layoutKind">A layout hint such as <c>"qwerty"</c>, <c>"numpad"</c> or <c>"decimal"</c>.</param>
    [JSInvokable]
    public void OnFocusIn(string layoutKind)
    {
        (CurrentLayout, CurrentSymbolLayout) = ResolveLayout(layoutKind);
        IsOpen = true;
        StateChanged?.Invoke();
    }

    /// <summary>Called from JS when focus leaves all attachable fields and the keyboard.</summary>
    [JSInvokable]
    public void OnFocusOut()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        StateChanged?.Invoke();
    }

    /// <summary>Inserts <paramref name="text"/> at the focused field's caret.</summary>
    /// <param name="text">The text to insert.</param>
    public ValueTask InsertTextAsync(string text) =>
        _module is null ? ValueTask.CompletedTask : _module.InvokeVoidAsync("insertText", text);

    /// <summary>Deletes the character before the focused field's caret.</summary>
    public ValueTask BackspaceAsync() =>
        _module is null ? ValueTask.CompletedTask : _module.InvokeVoidAsync("backspace");

    /// <summary>Emulates Enter on the focused field (newline for a textarea, otherwise a submit/commit).</summary>
    public ValueTask EnterAsync() =>
        _module is null ? ValueTask.CompletedTask : _module.InvokeVoidAsync("enter");

    /// <summary>Blurs the focused field and hides the keyboard.</summary>
    public async Task CloseAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("blurActive");
        }

        if (IsOpen)
        {
            IsOpen = false;
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Maps a JS layout hint to the built-in <see cref="KeyboardLayout"/> (and its symbol face, if any).
    /// Unknown hints fall back to the full QWERTY keyboard.
    /// </summary>
    /// <param name="kind">The layout hint from <c>inferLayout</c> in the JS shim.</param>
    internal static (KeyboardLayout Layout, KeyboardLayout? Symbol) ResolveLayout(string? kind) => kind switch
    {
        "numpad" or "numeric" or "tel" => (LayoutLibrary.Numpad, null),
        "decimal" or "money" or "price" => (LayoutLibrary.NumpadWithDecimal, null),
        _ => (LayoutLibrary.Qwerty, LayoutLibrary.Symbols),
    };

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone (e.g. browser closed) — nothing to clean up on the JS side.
            }
        }

        _selfRef?.Dispose();
    }
}
