using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudKeyboard.Internal;
using MudKeyboard.Models;

namespace MudKeyboard.Components;

/// <summary>
/// Layout surface: arranges resolved <see cref="KeyboardKey"/> rows into a flexbox grid of
/// keys and bubbles presses up to the owning component. Public only because Razor markup can
/// reference public components — the per-key button it renders stays internal.
/// </summary>
public sealed class KeyGrid : ComponentBase
{
    /// <summary>The resolved key rows to render, top to bottom.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<IReadOnlyList<KeyboardKey>> Keys { get; set; } = [];

    /// <summary>Raised with the pressed key.</summary>
    [Parameter]
    public EventCallback<KeyboardKey> OnKeyPress { get; set; }

    /// <summary>Raised with the key when it is double-clicked.</summary>
    [Parameter]
    public EventCallback<KeyboardKey> OnKeyDoublePress { get; set; }

    /// <summary>Whether every key is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether keys cast a drop shadow. Forwarded to each key button.</summary>
    [Parameter]
    public bool DropShadow { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, a short click sound plays on every key press. The sound is a
    /// Blazor-rendered <c>&lt;audio&gt;</c> element (no JavaScript), so it works in every render mode.
    /// </summary>
    [Parameter]
    public bool Sound { get; set; }

    /// <summary>
    /// Optional source for the click sound (any URL or <c>data:</c> URI). When unset the built-in
    /// synthesised click (<see cref="ClickSound.DataUri"/>) is used. Ignored unless <see cref="Sound"/> is set.
    /// </summary>
    [Parameter]
    public string? SoundSrc { get; set; }

    // Incremented on every key press. Used as the <audio> element's key so each press mounts a fresh,
    // autoplaying element — the browser plays it once on insertion (sticky activation from the tap
    // permits autoplay). Starts at 0 so nothing plays on first render.
    private int _clickTick;

    private string EffectiveSoundSrc => string.IsNullOrEmpty(SoundSrc) ? ClickSound.DataUri : SoundSrc;

    // Wrap the bubbled press so we can re-mount the click sound before forwarding it to the owner.
    private async Task HandleKeyPressAsync(KeyboardKey key)
    {
        if (Sound)
        {
            _clickTick++;
        }

        await OnKeyPress.InvokeAsync(key);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Constant sequence numbers inside the loops are intentional: Blazor's diff handles
        // repeated regions correctly when each iteration reuses the same hardcoded numbers.
        var pressCallback = EventCallback.Factory.Create<KeyboardKey>(this, HandleKeyPressAsync);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "mudkeyboard-grid");
        builder.AddAttribute(2, "style", "display:flex;flex-direction:column;gap:0.4rem;");

        foreach (var row in Keys)
        {
            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "class", "mudkeyboard-row");
            builder.AddAttribute(5, "style", "display:flex;gap:0.4rem;width:100%;");

            foreach (var key in row)
            {
                builder.OpenComponent<KeyButton>(6);
                builder.AddComponentParameter(7, nameof(KeyButton.Key), key);
                builder.AddComponentParameter(8, nameof(KeyButton.Disabled), Disabled);
                builder.AddComponentParameter(9, nameof(KeyButton.DropShadow), DropShadow);
                builder.AddComponentParameter(10, nameof(KeyButton.OnKeyPress), pressCallback);
                builder.AddComponentParameter(11, nameof(KeyButton.OnKeyDoublePress), OnKeyDoublePress);
                builder.CloseComponent();
            }

            builder.CloseElement();
        }

        // Click sound: a fresh, keyed <audio autoplay> per press. Keying on _clickTick makes Blazor
        // replace the element each press, so the browser plays it once on insertion. Rendered only
        // after the first press (tick > 0) so nothing sounds on initial load. No JavaScript involved.
        if (Sound && _clickTick > 0)
        {
            builder.OpenElement(12, "audio");
            builder.SetKey(_clickTick);
            builder.AddAttribute(13, "src", EffectiveSoundSrc);
            builder.AddAttribute(14, "autoplay", true);
            builder.AddAttribute(15, "preload", "auto");
            builder.AddAttribute(16, "aria-hidden", "true");
            builder.AddAttribute(17, "style", "display:none;");
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
