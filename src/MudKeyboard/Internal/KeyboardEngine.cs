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

    /// <summary>
    /// Toggles a leading minus sign on a numeric string: prepends <c>-</c> when absent, strips it when
    /// present (so <c>"5"</c> ↔ <c>"-5"</c>, and an empty value becomes <c>"-"</c> to allow sign-first
    /// entry). Null is treated as empty.
    /// </summary>
    public static string ToggleSign(string? current)
    {
        current ??= string.Empty;
        return current.StartsWith('-') ? current[1..] : "-" + current;
    }

    /// <summary>True when the token is a single ASCII letter (a–z or A–Z).</summary>
    public static bool IsLetterToken(string token) =>
        token is [(>= 'a' and <= 'z') or (>= 'A' and <= 'Z')];

    /// <summary>Upper-cases a single-letter token when <paramref name="upper"/> is set; otherwise
    /// returns it unchanged. Non-letter tokens are always returned unchanged.</summary>
    public static string ApplyCase(string token, bool upper) =>
        upper && IsLetterToken(token) ? token.ToUpperInvariant() : token;

    /// <summary>
    /// Projects <paramref name="layout"/> into rendered key rows for the given shift
    /// <paramref name="state"/>: letter keys are upper-cased and bolded while shift is active,
    /// and the shift key is highlighted (showing the caps-lock glyph when locked). The action
    /// token is left untouched — casing of the emitted character is decided at press time via
    /// <see cref="ApplyCase"/>. The symbol-toggle key shows <c>ABC</c> when
    /// <paramref name="symbolMode"/> is set (the symbol face is showing) and <c>123</c> otherwise.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<KeyboardKey>> BuildDisplayKeys(
        KeyboardLayout layout, ShiftState state, bool symbolMode = false)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var upper = state != ShiftState.Off;
        var rows = new IReadOnlyList<KeyboardKey>[layout.Rows.Count];
        for (var r = 0; r < layout.Rows.Count; r++)
        {
            var src = layout.Rows[r];
            var keys = new KeyboardKey[src.Count];
            for (var c = 0; c < src.Count; c++)
            {
                var token = src[c];
                var key = KeyboardKey.FromToken(token);

                if (token == KeyTokens.Shift)
                {
                    // The shift key reflects its own state: highlighted when armed, caps glyph when locked.
                    key = state switch
                    {
                        ShiftState.Locked => key with { DisplayLabel = "⇪", Highlighted = true },
                        ShiftState.OneShot => key with { Highlighted = true },
                        _ => key,
                    };
                }
                else if (token == KeyTokens.SymbolToggle)
                {
                    // Shows the face you switch *to*: "ABC" while symbols show, "123" while letters show.
                    key = key with { DisplayLabel = symbolMode ? "ABC" : "123" };
                }
                else if (upper && IsLetterToken(token))
                {
                    key = key with { DisplayLabel = token.ToUpperInvariant(), Bold = true };
                }

                keys[c] = key;
            }

            rows[r] = keys;
        }

        return rows;
    }
}
