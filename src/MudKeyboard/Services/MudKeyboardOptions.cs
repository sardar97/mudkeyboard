using MudKeyboard.Extensions;

namespace MudKeyboard.Services;

/// <summary>
/// Controls which fields the global docked keyboard (<see cref="MudKeyboard.Components.MudKeyboardHost"/>)
/// attaches to when an input is focused.
/// </summary>
public enum KeyboardAttachMode
{
    /// <summary>
    /// Attach to every editable text/number field on the page. Exclude individual fields by adding a
    /// <c>data-mudkeyboard-ignore</c> attribute. Best for touchscreen/kiosk apps where every field
    /// should raise the keyboard.
    /// </summary>
    AllInputs,

    /// <summary>
    /// Attach only to fields explicitly marked with a <c>data-mudkeyboard</c> attribute. Everything
    /// else is left alone.
    /// </summary>
    OptIn,
}

/// <summary>
/// Global configuration for MudKeyboard, supplied once via
/// <see cref="MudKeyboardServiceCollectionExtensions.AddMudKeyboard"/>.
/// </summary>
public sealed class MudKeyboardOptions
{
    /// <summary>
    /// Which fields the docked keyboard attaches to. Default <see cref="KeyboardAttachMode.AllInputs"/>.
    /// </summary>
    public KeyboardAttachMode AttachMode { get; set; } = KeyboardAttachMode.AllInputs;
}
