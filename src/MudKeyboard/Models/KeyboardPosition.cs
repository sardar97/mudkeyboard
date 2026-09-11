namespace MudKeyboard.Models;

/// <summary>
/// Where the global docked keyboard (<c>MudKeyboardHost</c>) sits on screen while it is open.
/// </summary>
public enum KeyboardPosition
{
    /// <summary>Docked to the bottom edge of the viewport, sliding up into view. The default.</summary>
    Bottom,

    /// <summary>Docked to the top edge of the viewport, sliding down into view.</summary>
    Top,

    /// <summary>Floating, centred vertically and horizontally in the viewport, fading and scaling into view.</summary>
    Center,
}
