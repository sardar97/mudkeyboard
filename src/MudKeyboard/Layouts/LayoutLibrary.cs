using MudKeyboard.Models;

namespace MudKeyboard.Layouts;

/// <summary>
/// The built-in <see cref="KeyboardLayout"/> definitions shipped with MudKeyboard, plus a
/// helper to resolve the default layout for a <see cref="KeyboardVariant"/>.
/// </summary>
public static class LayoutLibrary
{
    /// <summary>Standard English QWERTY layout with shift, backspace, space and enter.</summary>
    public static KeyboardLayout Qwerty { get; } = new()
    {
        Rows = new string[][]
        {
            ["q", "w", "e", "r", "t", "y", "u", "i", "o", "p"],
            ["a", "s", "d", "f", "g", "h", "j", "k", "l"],
            [KeyTokens.Shift, "z", "x", "c", "v", "b", "n", "m", KeyTokens.Backspace],
            [KeyTokens.Space, KeyTokens.Enter],
        },
    };

    /// <summary>A single row of the digits 0–9.</summary>
    public static KeyboardLayout Numeric { get; } = new()
    {
        Rows = new string[][]
        {
            ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"],
        },
    };

    /// <summary>Calculator-style numeric pad: 7-8-9 / 4-5-6 / 1-2-3 / backspace-0-enter.</summary>
    public static KeyboardLayout Numpad { get; } = new()
    {
        Rows = new string[][]
        {
            ["7", "8", "9"],
            ["4", "5", "6"],
            ["1", "2", "3"],
            [KeyTokens.Backspace, "0", KeyTokens.Enter],
        },
    };

    /// <summary>Calculator-style numeric pad with an extra decimal-point key.</summary>
    public static KeyboardLayout NumpadWithDecimal { get; } = new()
    {
        Rows = new string[][]
        {
            ["7", "8", "9"],
            ["4", "5", "6"],
            ["1", "2", "3"],
            [".", "0", KeyTokens.Backspace],
            [KeyTokens.Enter],
        },
    };

    /// <summary>
    /// Money-entry pad: digits plus a "00" quick key, backspace and enter. There is no decimal
    /// point — currency components (such as the pricepad) place the decimal automatically.
    /// </summary>
    public static KeyboardLayout Price { get; } = new()
    {
        Rows = new string[][]
        {
            ["7", "8", "9"],
            ["4", "5", "6"],
            ["1", "2", "3"],
            ["00", "0", KeyTokens.Backspace],
            [KeyTokens.Enter],
        },
    };

    /// <summary>
    /// Returns the default built-in layout for <paramref name="variant"/>.
    /// </summary>
    /// <param name="variant">The variant whose default layout is required.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown for <see cref="KeyboardVariant.Custom"/> — which has no built-in layout and
    /// requires a caller-supplied one — or for an unrecognised value.
    /// </exception>
    public static KeyboardLayout ForVariant(KeyboardVariant variant) => variant switch
    {
        KeyboardVariant.Full => Qwerty,
        KeyboardVariant.Numpad => Numpad,
        KeyboardVariant.Pricepad => Price,
        _ => throw new ArgumentOutOfRangeException(
            nameof(variant),
            variant,
            "KeyboardVariant.Custom has no built-in layout; supply a KeyboardLayout instead."),
    };
}
