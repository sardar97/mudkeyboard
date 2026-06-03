using MudKeyboard.Models;

namespace MudKeyboard.Internal;

/// <summary>
/// Pure, UI-free helpers for applying key presses to a text value and for projecting a
/// layout into display keys. Kept out of the components so the behaviour is unit-testable
/// and the <c>.razor</c> files hold no business logic. No reflection, no allocation tricks —
/// AOT and trim safe.
/// </summary>
internal static class KeyboardEngine
{
    /// <summary>Appends <paramref name="text"/> to <paramref name="current"/>, truncating to
    /// <paramref name="maxLength"/> when supplied. Null current is treated as empty.</summary>
    public static string Append(string? current, string text, int? maxLength)
    {
        current ??= string.Empty;
        var combined = current + text;
        return maxLength is { } max && combined.Length > max ? combined[..max] : combined;
    }

    /// <summary>Removes the last character. Safe on null/empty (returns empty).</summary>
    public static string Backspace(string? current) =>
        string.IsNullOrEmpty(current) ? string.Empty : current[..^1];

    /// <summary>True when the token is a single ASCII letter (a–z or A–Z).</summary>
    public static bool IsLetterToken(string token) =>
        token is [(>= 'a' and <= 'z') or (>= 'A' and <= 'Z')];

    /// <summary>Upper-cases a single-letter token when <paramref name="upper"/> is set; otherwise
    /// returns it unchanged. Non-letter tokens are always returned unchanged.</summary>
    public static string ApplyCase(string token, bool upper) =>
        upper && IsLetterToken(token) ? token.ToUpperInvariant() : token;

    /// <summary>
    /// Projects <paramref name="layout"/> into rendered key rows, upper-casing letter keys'
    /// labels when <paramref name="upper"/> is set (shift/caps display). The action token is
    /// left untouched — casing of the emitted character is decided at press time via
    /// <see cref="ApplyCase"/>.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<KeyboardKey>> BuildDisplayKeys(KeyboardLayout layout, bool upper)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var rows = new IReadOnlyList<KeyboardKey>[layout.Rows.Count];
        for (var r = 0; r < layout.Rows.Count; r++)
        {
            var src = layout.Rows[r];
            var keys = new KeyboardKey[src.Count];
            for (var c = 0; c < src.Count; c++)
            {
                var token = src[c];
                var key = KeyboardKey.FromToken(token);
                if (upper && IsLetterToken(token))
                {
                    key = key with { DisplayLabel = token.ToUpperInvariant() };
                }

                keys[c] = key;
            }

            rows[r] = keys;
        }

        return rows;
    }
}
