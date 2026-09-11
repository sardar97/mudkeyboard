using Microsoft.JSInterop;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

/// <summary>
/// Value changes that arrive from <em>outside</em> the on-screen keys — a <c>MudNumericField</c> spin
/// button / ArrowUp / ArrowDown, hardware typing, app code setting the bound value — reported by the JS
/// shim as <c>OnValueChanged(value, caret, external: true)</c>. They must update the live preview, cancel
/// a pending "first digit replaces the value" and re-seed the pence-first money accumulator, so the next
/// digit continues from what is really in the field (GitHub #9). Pure caret reports and the keyboard's own
/// edits (<c>external: false</c>) must leave that state alone.
/// </summary>
public class KeyboardInteropExternalChangeTests
{
    private static async Task<(KeyboardInteropService Service, RecordingModule Module)> NewInitializedAsync()
    {
        var js = new RecordingJsRuntime();
        var service = new KeyboardInteropService(js, new MudKeyboardOptions());
        await service.InitializeAsync();
        return (service, js.Module);
    }

    [Fact]
    public async Task NumericKeypad_CaretOnlyReport_KeepsTheFirstDigitReplacing()
    {
        // Tapping into a field snaps the caret to the end and reports it (same value, new caret). That is
        // not an edit: the first digit pressed afterwards must still replace the pre-filled value.
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("numpad", 0, "6");
        service.OnValueChanged("6", 1);
        module.Calls.Clear();

        await service.InsertNumericAsync("5");

        Assert.Equal("setValue", Assert.Single(module.Calls).Identifier);
    }

    [Fact]
    public async Task NumericKeypad_ExternalChange_CancelsTheFirstDigitReplace()
    {
        // A spin button took the field from 6 to 7: the user is editing that value, so the next digit
        // appends (7 → 75) instead of wiping it (→ 5).
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("numpad", 0, "6");
        service.OnValueChanged("7", 1, external: true);
        module.Calls.Clear();

        await service.InsertNumericAsync("5");

        var call = Assert.Single(module.Calls);
        Assert.Equal("insertText", call.Identifier);
        Assert.Equal("5", call.FirstArg);
    }

    [Fact]
    public async Task NumericKeypad_OwnEditReport_DoesNotCancelTheReplace()
    {
        // The keyboard's own writes are accounted for by the methods that made them; a non-external report
        // of a different value must not flip the replace state on its own.
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("numpad", 0, "6");
        service.OnValueChanged("7", 1, external: false);
        module.Calls.Clear();

        await service.InsertNumericAsync("5");

        Assert.Equal("setValue", Assert.Single(module.Calls).Identifier);
    }

    [Fact]
    public async Task MoneyKeypad_ExternalChange_ReseedsTheAmount_SoTheNextDigitAppendsToIt()
    {
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("money", 0, "6.00");
        service.OnValueChanged("7.00", 4, external: true); // a spin button (or app code) changed the amount
        module.Calls.Clear();

        await service.AppendMoneyDigitsAsync("5"); // continues from 7.00 → 700 + 5 → 70.05

        Assert.Equal("70.05", Assert.Single(module.SetValues()));
    }

    [Fact]
    public async Task MoneyKeypad_ExternalNegativeChange_KeepsTheSign()
    {
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("money", 0, "6.00");
        service.OnValueChanged("-7.00", 5, external: true);
        module.Calls.Clear();

        await service.AppendMoneyDigitsAsync("5");

        Assert.Equal("-70.05", Assert.Single(module.SetValues()));
    }

    [Fact]
    public async Task MoneyKeypad_OwnWriteReport_DoesNotReseedTheAmount()
    {
        // The ± key on an empty amount writes "0.00" (the sign is suppressed for zero). If that own write
        // re-seeded the accumulator, the sign would be lost and the next digit would give 0.05, not -0.05.
        var (service, module) = await NewInitializedAsync();
        service.OnFocusIn("money", 0, "");
        await service.ToggleMoneySignAsync();
        service.OnValueChanged("0.00", 4, external: false); // the shim mirroring that write for the preview
        module.Calls.Clear();

        await service.AppendMoneyDigitsAsync("5");

        Assert.Equal("-0.05", Assert.Single(module.SetValues()));
    }

    [Fact]
    public void OnValueChanged_ExternalChange_UpdatesTheLivePreview_AndNotifies()
    {
        var service = new KeyboardInteropService(new RecordingJsRuntime(), new MudKeyboardOptions());
        service.OnFocusIn("numpad", 0, "0");
        var notifications = 0;
        service.StateChanged += () => notifications++;

        service.OnValueChanged("1", 1, external: true);

        Assert.Equal("1", service.CurrentValue);
        Assert.Equal(1, service.CurrentCaret);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void OnValueChanged_ExternalChange_KeepsTheOriginalValueForCancel()
    {
        // Cancel must still revert to the value the field held at focus-in, not to the spun value.
        var service = new KeyboardInteropService(new RecordingJsRuntime(), new MudKeyboardOptions());
        service.OnFocusIn("numpad", 0, "0");

        service.OnValueChanged("1", 1, external: true);

        Assert.Equal("0", service.OriginalValue);
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
