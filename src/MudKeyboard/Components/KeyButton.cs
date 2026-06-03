using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudKeyboard.Models;

namespace MudKeyboard.Components;

/// <summary>
/// Internal building block: renders a single <see cref="KeyboardKey"/> as a themed
/// <see cref="MudButton"/> whose colour comes entirely from the ambient MudBlazor theme.
/// Authored in C# (not <c>.razor</c>) so it can be genuinely <see langword="internal"/> and
/// stay off the public API surface.
/// </summary>
internal sealed class KeyButton : ComponentBase
{
    /// <summary>The key to render.</summary>
    [Parameter]
    [EditorRequired]
    public required KeyboardKey Key { get; set; }

    /// <summary>Raised with the pressed <see cref="KeyboardKey"/> when the button is clicked.</summary>
    [Parameter]
    public EventCallback<KeyboardKey> OnKeyPress { get; set; }

    /// <summary>Raised with the key when the button is double-clicked (used for shift → caps lock).</summary>
    [Parameter]
    public EventCallback<KeyboardKey> OnKeyDoublePress { get; set; }

    /// <summary>Whether the key is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the key casts a drop shadow. Maps directly to <c>MudButton.DropShadow</c>.</summary>
    [Parameter]
    public bool DropShadow { get; set; } = true;

    // Command keys (space, backspace, shift…) get the primary accent; literals stay neutral.
    private Color KeyColor => Key.IsEnter || Key.Highlighted ? Color.Primary : Color.Default;
    

    // Grow proportionally to the key's width multiplier within its flex row.
    private string KeyStyle =>
        $"flex:{Key.WidthMultiplier.ToString(CultureInfo.InvariantCulture)} 1 0;min-width:0;min-height:3rem;";

    // Applied to the label itself so it wins over MudButton's own rules: cancel the Material
    // uppercase transform, enlarge a touch, and bold shifted letters.
    private string LabelStyle =>
        $"text-transform:none !important;font-size:1.15rem !important;{(Key.Bold ? "font-weight:700 !important;" : string.Empty)}";

    private Task HandleClickAsync() => OnKeyPress.InvokeAsync(Key);

    private Task HandleDoubleClickAsync() => OnKeyDoublePress.InvokeAsync(Key);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<MudButton>(0);
        builder.AddComponentParameter(1, nameof(MudButton.Variant), Variant.Filled);
        builder.AddComponentParameter(2, nameof(MudButton.Color), KeyColor);
        builder.AddComponentParameter(3, nameof(MudButton.Disabled), Disabled);
        builder.AddComponentParameter(4, nameof(MudButton.DropShadow), DropShadow);
        builder.AddComponentParameter(5, nameof(MudButton.Style), KeyStyle);
        builder.AddComponentParameter(
            6,
            nameof(MudButton.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
        builder.AddComponentParameter(7, nameof(MudButton.ChildContent), (RenderFragment)BuildLabel);
        // Native double-click (no JS interop) → caps lock; splatted onto MudButton's root button.
        builder.AddAttribute(8, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleDoubleClickAsync));
        builder.CloseComponent();
    }

    private void BuildLabel(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "style", LabelStyle);
        builder.AddContent(2, Key.DisplayLabel);
        builder.CloseElement();
    }
}
