using System.Text;

namespace MudKeyboard.Internal;

/// <summary>
/// Formats a raw run of digits as a fixed-point currency string and back. The pricepad keeps
/// the last <c>decimalPlaces</c> digits as the fractional part (pence-first entry), so typing
/// 1-2-3 with two decimal places yields <c>1.23</c>. Culture-invariant and allocation-light —
/// AOT and trim safe.
/// </summary>
internal static class PricepadFormatter
{
    /// <summary>Returns only the ASCII digits found in <paramref name="value"/> (e.g. the digits
    /// already shown in a formatted price), or empty for null/empty input.</summary>
    public static string ExtractDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is >= '0' and <= '9')
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats <paramref name="digits"/> as <c>{symbol}{integer}.{fraction}</c>. Non-digit
    /// characters in <paramref name="digits"/> are ignored, the integer part is zero-trimmed,
    /// and the fraction is left-padded to <paramref name="decimalPlaces"/>.
    /// </summary>
    public static string Format(string? digits, string? currencySymbol, int decimalPlaces)
    {
        currencySymbol ??= string.Empty;
        if (decimalPlaces < 0)
        {
            decimalPlaces = 0;
        }

        var clean = ExtractDigits(digits);

        if (decimalPlaces == 0)
        {
            return currencySymbol + TrimLeadingZeros(clean);
        }

        string integerPart;
        string fractionalPart;
        if (clean.Length <= decimalPlaces)
        {
            integerPart = "0";
            fractionalPart = clean.PadLeft(decimalPlaces, '0');
        }
        else
        {
            integerPart = TrimLeadingZeros(clean[..^decimalPlaces]);
            fractionalPart = clean[^decimalPlaces..];
        }

        return currencySymbol + integerPart + "." + fractionalPart;
    }

    /// <summary>
    /// Formats <paramref name="digits"/> like <see cref="Format(string?, string?, int)"/>, prefixing a
    /// minus sign (before the currency symbol, e.g. <c>-£1.23</c>) when <paramref name="negative"/> is
    /// set. The sign is suppressed for a zero amount, so there is never a <c>-£0.00</c>.
    /// </summary>
    public static string Format(string? digits, string? currencySymbol, int decimalPlaces, bool negative)
    {
        var formatted = Format(digits, currencySymbol, decimalPlaces);
        return negative && HasNonZeroDigit(digits) ? "-" + formatted : formatted;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="value"/> is a negative amount — i.e. its first
    /// non-whitespace character is a minus sign (as produced by <see cref="Format(string?, string?, int, bool)"/>).
    /// </summary>
    public static bool IsNegative(string? value) =>
        value is not null && value.AsSpan().TrimStart().StartsWith("-");

    private static bool HasNonZeroDigit(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (ch is >= '1' and <= '9')
            {
                return true;
            }
        }

        return false;
    }

    private static string TrimLeadingZeros(string s)
    {
        if (s.Length == 0)
        {
            return "0";
        }

        var i = 0;
        while (i < s.Length - 1 && s[i] == '0')
        {
            i++;
        }

        return s[i..];
    }
}
