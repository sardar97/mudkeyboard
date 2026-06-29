using System.Linq;
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
            [KeyTokens.SymbolToggle, KeyTokens.Space, KeyTokens.Enter],
        },
    };

    /// <summary>
    /// The numbers-and-symbols face of the full keyboard, reached from <see cref="Qwerty"/> via
    /// the <see cref="KeyTokens.SymbolToggle"/> key. Its toggle key returns to the letters.
    /// </summary>
    public static KeyboardLayout Symbols { get; } = new()
    {
        Rows = new string[][]
        {
            ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"],
            ["@", "#", "£", "$", "€", "_", "&", "-", "+", "(", ")"],
            ["/", ".", "*", "\"", "'", ":", ";", "!", "?", KeyTokens.Backspace],
            [KeyTokens.SymbolToggle, KeyTokens.Space, KeyTokens.Enter],
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

    /// <summary>Calculator-style numeric pad: 7-8-9 / 4-5-6 / 1-2-3 / 0-00-backspace / enter.</summary>
    public static KeyboardLayout Numpad { get; } = new()
    {
        Rows = new string[][]
        {
            ["7", "8", "9"],
            ["4", "5", "6"],
            ["1", "2", "3"],
            ["0", "00", KeyTokens.Backspace],
            [KeyTokens.Enter],
        },
    };

    /// <summary>Like <see cref="Numpad"/> but with an extra decimal-point key in the bottom number row.</summary>
    public static KeyboardLayout NumpadWithDecimal { get; } = new()
    {
        Rows = new string[][]
        {
            ["7", "8", "9"],
            ["4", "5", "6"],
            ["1", "2", "3"],
            ["0", "00", ".", KeyTokens.Backspace],
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
            ["0","00", KeyTokens.Backspace],
            [KeyTokens.Enter],
        },
    };

    /// <summary>
    /// <see cref="Numpad"/> with a <see cref="KeyTokens.Sign"/> (<c>±</c>) key alongside Enter, for
    /// entering negative numbers. Opt-in (e.g. <c>MudNumpad.AllowNegative</c>).
    /// </summary>
    public static KeyboardLayout NumpadSigned { get; } = WithSignKey(Numpad);

    /// <summary><see cref="NumpadWithDecimal"/> with a <see cref="KeyTokens.Sign"/> (<c>±</c>) key alongside Enter.</summary>
    public static KeyboardLayout NumpadWithDecimalSigned { get; } = WithSignKey(NumpadWithDecimal);

    /// <summary><see cref="Price"/> with a <see cref="KeyTokens.Sign"/> (<c>±</c>) key alongside Enter.</summary>
    public static KeyboardLayout PriceSigned { get; } = WithSignKey(Price);

    // Returns a copy of a keypad layout with a ± key placed left of the (full-width) Enter key. Keeping
    // the digit rows untouched avoids crowding the narrow keypad; the sign key sits on the Enter row.
    private static KeyboardLayout WithSignKey(KeyboardLayout baseLayout)
    {
        var rows = baseLayout.Rows.Select(r => r.ToArray()).ToArray();
        rows[^1] = [KeyTokens.Sign, .. rows[^1]];
        return new KeyboardLayout { Rows = rows };
    }

    /// <summary>True when <paramref name="layout"/> is the money/price keypad (signed or not).</summary>
    internal static bool IsMoney(KeyboardLayout? layout) =>
        ReferenceEquals(layout, Price) || ReferenceEquals(layout, PriceSigned);

    /// <summary>True when <paramref name="layout"/> is any built-in numeric keypad (signed or not).</summary>
    internal static bool IsKeypad(KeyboardLayout? layout) =>
        IsMoney(layout)
        || ReferenceEquals(layout, Numpad) || ReferenceEquals(layout, NumpadSigned)
        || ReferenceEquals(layout, NumpadWithDecimal) || ReferenceEquals(layout, NumpadWithDecimalSigned);

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

    /// <summary>
    /// Returns the numbers/symbols face paired with <paramref name="variant"/>, or
    /// <see langword="null"/> when the variant has none. Only <see cref="KeyboardVariant.Full"/>
    /// ships a built-in symbol face (<see cref="Symbols"/>); the numeric pads have nothing to flip to.
    /// </summary>
    /// <param name="variant">The variant whose symbol face is required.</param>
    public static KeyboardLayout? SymbolsForVariant(KeyboardVariant variant) =>
        variant == KeyboardVariant.Full ? Symbols : null;
}
