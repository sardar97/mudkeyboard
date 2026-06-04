using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudKeyboard.Docs.Services;

/// <summary>
/// Thin wrapper over the docs site's <c>docs.js</c> module: syntax-highlights code blocks and copies
/// text to the clipboard. Used only by the documentation site — the MudKeyboard library itself ships
/// no JavaScript beyond its single focus-capture shim.
/// </summary>
public sealed class DocsInterop : IAsyncDisposable
{
    private const string ModulePath = "./docs.js";

    private readonly IJSRuntime _js;
    private Task<IJSObjectReference>? _module;

    public DocsInterop(IJSRuntime js) => _js = js;

    private Task<IJSObjectReference> ModuleAsync() =>
        _module ??= _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();

    /// <summary>Syntax-highlights a single <c>&lt;code&gt;</c> element.</summary>
    public async ValueTask HighlightAsync(ElementReference element)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("highlight", element);
    }

    /// <summary>Copies <paramref name="text"/> to the clipboard. Returns <see langword="true"/> on success.</summary>
    public async ValueTask<bool> CopyAsync(string text)
    {
        var module = await ModuleAsync();
        return await module.InvokeAsync<bool>("copy", text);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            var module = await _module;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit/runtime already gone — nothing to dispose on the JS side.
        }
    }
}
