namespace MudKeyboard.Internal;

/// <summary>
/// State of the keyboard's shift key: off, armed for a single keypress (one-shot), or locked
/// on (caps lock, entered by double-pressing shift).
/// </summary>
internal enum ShiftState
{
    /// <summary>Shift is off; letters render lowercase.</summary>
    Off,

    /// <summary>Shift is armed for exactly one keypress, then releases automatically.</summary>
    OneShot,

    /// <summary>Caps lock: letters stay uppercase until shift is pressed again.</summary>
    Locked,
}
