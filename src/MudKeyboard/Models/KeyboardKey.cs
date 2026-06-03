namespace MudKeyboard.Models;

/// <summary>
/// The rendered representation of a single key: what the user sees, what it does when
/// pressed, and how wide it is relative to a standard key.
/// </summary>
/// <param name="ActionToken">
/// The token emitted when the key is pressed — either a literal character (for example
/// <c>"a"</c> or <c>"5"</c>) that is appended to the value, or a command token such as
/// <see cref="KeyTokens.Backspace"/>.
/// </param>
/// <param name="DisplayLabel">The glyph or text shown on the key face.</param>
/// <param name="WidthMultiplier">
/// The key's width relative to a standard single key. <c>1.0</c> is a normal key; the
/// space bar and command keys are typically wider. Expected to be greater than zero.
/// </param>
public sealed record KeyboardKey(string ActionToken, string DisplayLabel, double WidthMultiplier = 1.0)
{
    /// <summary>
    /// <see langword="true"/> when this key is a command (such as backspace or space)
    /// rather than a literal character to append to the value.
    /// </summary>
    public bool IsCommand => KeyTokens.IsCommand(ActionToken);
    /// <summary>
    /// <see langword="true"/> when this key is Enter
    /// rather than a literal character to append to the value.
    /// </summary>
    public bool IsEnter => KeyTokens.IsEnter(ActionToken);

    /// <summary>When set, the key renders with the primary accent (for example an active shift).</summary>
    public bool Highlighted { get; init; }

    /// <summary>When set, the key's label renders in bold (for example shifted letters).</summary>
    public bool Bold { get; init; }

    /// <summary>
    /// Creates a <see cref="KeyboardKey"/> from a layout token, resolving a sensible display
    /// label and width for the well-known command tokens and treating anything else as a
    /// literal character (label equal to the token, standard width).
    /// </summary>
    /// <param name="token">The layout token to resolve.</param>
    public static KeyboardKey FromToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token switch
        {
            KeyTokens.Backspace => new KeyboardKey(token, "⌫", 1.5d), // ⌫
            KeyTokens.Enter => new KeyboardKey(token, "⏎", 1.5d),     // ⏎
            KeyTokens.Space => new KeyboardKey(token, "⎵", 5.0d),    // ⎵
            KeyTokens.Shift => new KeyboardKey(token, "⇧", 1.5d),     // ⇧
            KeyTokens.Caps => new KeyboardKey(token, "⇪", 1.5d),      // ⇪
            KeyTokens.Escape => new KeyboardKey(token, "Esc", 1.5d),
            _ => new KeyboardKey(token, token, 1.0d),
        };
    }

    /// <summary>
    /// Projects a layout's raw token rows into rendered <see cref="KeyboardKey"/> rows using
    /// <see cref="FromToken(string)"/>. Kept here (rather than in a component) so the
    /// token-resolution logic lives in plain C#.
    /// </summary>
    /// <param name="rows">The raw token rows, typically <see cref="KeyboardLayout.Rows"/>.</param>
    public static IReadOnlyList<IReadOnlyList<KeyboardKey>> FromRows(
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var result = new IReadOnlyList<KeyboardKey>[rows.Count];
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var keys = new KeyboardKey[row.Count];
            for (var c = 0; c < row.Count; c++)
            {
                keys[c] = FromToken(row[c]);
            }

            result[r] = keys;
        }

        return result;
    }
}
