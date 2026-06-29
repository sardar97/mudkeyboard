namespace MudKeyboard.Internal;

/// <summary>
/// Maps a numeric CLR type to the docked-keyboard layout token the focus-capture shim understands, so
/// <see cref="MudKeyboard.Components.MudKeyboardNumericField{T}"/> can choose the right keypad straight
/// from its bound type:
/// <list type="bullet">
///   <item><description><see cref="decimal"/> → <c>"money"</c> — the currency keypad (pence-first entry).</description></item>
///   <item><description><see cref="double"/> / <see cref="float"/> → <c>"decimal"</c> — the numeric keypad with a "." key.</description></item>
///   <item><description>Integer types (<see cref="int"/>, <see cref="long"/>, <see cref="short"/>, …) → <c>"numpad"</c> — the numeric keypad with no "." key.</description></item>
/// </list>
/// Nullable numeric types (for example <c>decimal?</c>) resolve the same as their underlying type.
/// </summary>
/// <remarks>
/// The <c>typeof</c> equality checks below resolve to cheap type-handle comparisons, not member
/// reflection, so they pull no metadata the trimmer would strip — this stays AOT/trim-safe. (They are
/// the one unavoidable type inspection: a <see cref="decimal"/> and a <see cref="double"/> render the
/// same HTML <c>inputmode="decimal"</c>, so the bound type is the only thing that tells them apart.)
/// </remarks>
internal static class NumericLayoutResolver
{
    /// <summary>The currency keypad token (pence-first), used for <see cref="decimal"/>.</summary>
    public const string Money = "money";

    /// <summary>The numeric keypad token with a decimal-point key, used for <see cref="double"/>/<see cref="float"/>.</summary>
    public const string Decimal = "decimal";

    /// <summary>The numeric keypad token without a decimal-point key, used for integer types.</summary>
    public const string Numpad = "numpad";

    /// <summary>
    /// Resolves the docked-keyboard layout token for the numeric type <paramref name="type"/>.
    /// Unrecognised types fall back to <see cref="Decimal"/> (a safe numeric keypad with a "." key).
    /// </summary>
    /// <param name="type">The bound CLR type; may be a <see cref="Nullable{T}"/> of a numeric type.</param>
    /// <returns><see cref="Money"/>, <see cref="Decimal"/> or <see cref="Numpad"/>.</returns>
    public static string Resolve(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(decimal))
        {
            return Money;
        }

        if (t == typeof(double) || t == typeof(float))
        {
            return Decimal;
        }

        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
            || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) || t == typeof(sbyte)
            || t == typeof(nint) || t == typeof(nuint))
        {
            return Numpad;
        }

        return Decimal;
    }
}
