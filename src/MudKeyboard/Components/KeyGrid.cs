using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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

    /// <summary>Whether every key is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Constant sequence numbers inside the loops are intentional: Blazor's diff handles
        // repeated regions correctly when each iteration reuses the same hardcoded numbers.
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
                builder.AddComponentParameter(9, nameof(KeyButton.OnKeyPress), OnKeyPress);
                builder.CloseComponent();
            }

            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
