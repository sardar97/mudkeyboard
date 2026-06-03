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

    /// <summary>Whether the key is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the key casts a drop shadow. Maps directly to <see cref="MudButton.DropShadow"/>.</summary>
    [Parameter]
    public bool DropShadow { get; set; } = true;

    // Command keys (space, backspace, shift…) get the primary accent; literals stay neutral.
    private Color KeyColor => Key.IsEnter ? Color.Primary : Color.Default;
    

    // Grow proportionally to the key's width multiplier within its flex row.
    private string KeyStyle =>
        $"flex:{Key.WidthMultiplier.ToString(CultureInfo.InvariantCulture)} 1 0;min-width:0;min-height:3rem;";

    private Task HandleClickAsync() => OnKeyPress.InvokeAsync(Key);

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
        builder.CloseComponent();
    }

    private void BuildLabel(RenderTreeBuilder builder) => builder.AddContent(0, Key.DisplayLabel);
}
