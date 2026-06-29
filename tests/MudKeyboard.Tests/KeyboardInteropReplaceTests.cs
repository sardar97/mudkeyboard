using Microsoft.JSInterop;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

/// <summary>
/// The docked keyboard's caret tracking and the "first digit replaces the existing value" behaviour for
/// the numeric/money keypads. These exercise <see cref="KeyboardInteropService"/> against a recording JS
/// module so the exact value written back to the focused field can be asserted.
/// </summary>
public class KeyboardInteropReplaceTests
{
    private static async Task<(KeyboardInteropService Service, RecordingModule Module)> NewInitializedAsync()
    {
        var js = new RecordingJsRuntime();
        var service = new KeyboardInteropService(js, new MudKeyboardOptions());
        await service.InitializeAsync();
        return (service, js.Module);
    }

    [Fact]
    public void OnFocusIn_SeedsCaret_AtTheEndOfTheValue()
    {
        var service = new KeyboardInteropService(new RecordingJsRuntime(), new MudKeyboardOptions());

        service.OnFocusIn("qwerty", 0, "sardar");

        Assert.Equal("sardar".Length, service.CurrentCaret);
    }

    [Fact]
    public void OnValueChanged_UpdatesTheCaret()
    {
        var service = new KeyboardInteropService(new RecordingJsRuntime(), new MudKeyboardOptions());
        service.OnFocusIn("qwerty", 0, "sardar");

        service.OnValueChanged("sardar", 2);

        Assert.Equal(2, service.CurrentCaret);
    }

    [Fact]
    public void OnValueChanged_NegativeCaret_ClampsToTheEnd()
    {
        var service = new KeyboardInteropService(new RecordingJsRuntime(), new MudKeyboardOptions());
        service.OnFocusIn("qwerty", 0, "ab");

        // A negative caret (the default when JS does not pass one) means "place it at the end".
        service.OnValueChanged("abcd");

        Assert.Equal(4, service.CurrentCaret);
    }

    [Fact]
    public async Task MoneyKeypad_FirstDigitReplacesTheExistingAmount_ThenAppends()
    {
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("money", 0, "6.00"); // a pre-filled amount; the first digit should replace it
        module.Calls.Clear();

        await service.AppendMoneyDigitsAsync("5"); // replace → 0.05 (not 60.05)
        await service.AppendMoneyDigitsAsync("2"); // now appends → 0.52

        Assert.Equal(new[] { "0.05", "0.52" }, module.SetValues());
    }

    [Fact]
    public async Task NumericKeypad_FirstDigitReplaces_ThenInsertsAtTheCaret()
    {
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("numpad", 0, "6"); // pre-filled; first digit replaces
        module.Calls.Clear();

        await service.InsertNumericAsync("5"); // replace → setValue "5"
        await service.InsertNumericAsync("2"); // append → insertText "2"

        Assert.Equal("setValue", module.Calls[0].Identifier);
        Assert.Equal("5", module.Calls[0].FirstArg);
        Assert.Equal("insertText", module.Calls[1].Identifier);
        Assert.Equal("2", module.Calls[1].FirstArg);
    }

    [Fact]
    public async Task MoneyKeypad_BackspaceFirst_CancelsReplace_SoTheNextDigitAppends()
    {
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("money", 0, "6.00");
        module.Calls.Clear();

        await service.BackspaceMoneyAsync();       // editing the existing value: 600 → 60 → 0.60
        await service.AppendMoneyDigitsAsync("5"); // appends (replace was cancelled) → 605 → 6.05

        Assert.Equal(new[] { "0.60", "6.05" }, module.SetValues());
    }

    [Fact]
    public async Task TextField_NeverReplaces_OnTheFirstKey()
    {
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("qwerty", 0, "sardar"); // text layout → no replace
        module.Calls.Clear();

        await service.InsertTextAsync("x");

        Assert.Equal("insertText", Assert.Single(module.Calls).Identifier);
    }

    // A recording IJSObjectReference: captures every interop call so the value written to the field can be
    // asserted. Returned from the runtime's "import" call.
    private sealed class RecordingModule : IJSObjectReference
    {
        public List<(string Identifier, string? FirstArg)> Calls { get; } = [];

        public string[] SetValues() =>
            Calls.Where(c => c.Identifier == "setValue").Select(c => c.FirstArg ?? string.Empty).ToArray();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add((identifier, args is { Length: > 0 } ? args[0] as string : null));
            return ValueTask.FromResult<TValue>(default!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // A JS runtime whose "import" hands back the recording module; nothing else is invoked on it directly.
    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public RecordingModule Module { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            identifier == "import"
                ? ValueTask.FromResult((TValue)(object)Module)
                : ValueTask.FromResult<TValue>(default!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
