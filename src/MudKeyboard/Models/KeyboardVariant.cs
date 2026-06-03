namespace MudKeyboard.Models;

/// <summary>
/// Identifies which keyboard surface a component should present.
/// </summary>
public enum KeyboardVariant
{
    /// <summary>A full QWERTY text keyboard.</summary>
    Full,

    /// <summary>A calculator-style numeric pad (digits, backspace, enter).</summary>
    Numpad,

    /// <summary>A numeric pad whose value is formatted as a currency string.</summary>
    Pricepad,

    /// <summary>A caller-supplied <see cref="KeyboardLayout"/> is used instead of a built-in one.</summary>
    Custom,
}
