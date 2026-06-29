namespace MudKeyboard.Models;

/// <summary>
/// The well-known special key tokens understood by the built-in layouts and components.
/// </summary>
/// <remarks>
/// A token is a value placed inside a <see cref="KeyboardLayout"/> row. Tokens wrapped in
/// braces (for example <c>"{bksp}"</c>) are <em>commands</em> handled by the component;
/// any other token is a <em>literal</em> character that is appended to the value verbatim.
/// </remarks>
public static class KeyTokens
{
    /// <summary>Deletes the character to the left of the caret. Token: <c>{bksp}</c>.</summary>
    public const string Backspace = "{bksp}";

    /// <summary>Commits the current value / submits. Token: <c>{enter}</c>.</summary>
    public const string Enter = "{enter}";

    /// <summary>Inserts a single space. Token: <c>{space}</c>.</summary>
    public const string Space = "{space}";

    /// <summary>Toggles shift for the next key press. Token: <c>{shift}</c>.</summary>
    public const string Shift = "{shift}";

    /// <summary>Toggles caps lock. Token: <c>{caps}</c>.</summary>
    public const string Caps = "{caps}";

    /// <summary>
    /// Flips the keyboard between its letter face and its numbers/symbols face. Token:
    /// <c>{sym}</c>. The key shows <c>123</c> on the letter face and <c>ABC</c> on the symbol face.
    /// </summary>
    public const string SymbolToggle = "{sym}";

    /// <summary>Dismisses / cancels. Token: <c>{esc}</c>. Not present in built-in layouts.</summary>
    public const string Escape = "{esc}";

    /// <summary>
    /// Cancels the current edit: reverts to the value held before editing began and dismisses. Token:
    /// <c>{cancel}</c>. Used by the global docked keyboard, which renders it next to the Enter key.
    /// </summary>
    public const string Cancel = "{cancel}";

    /// <summary>
    /// Toggles the sign of a numeric value (positive ↔ negative). Token: <c>{sign}</c>. Shown as
    /// <c>±</c> on the signed numeric keypads; opt-in, so it is absent from the default keypads.
    /// </summary>
    public const string Sign = "{sign}";

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="token"/> is a command token
    /// (wrapped in braces) rather than a literal character.
    /// </summary>
    /// <param name="token">The key token to test.</param>
    public static bool IsCommand(string token) =>
        token is ['{', _, ..] && token[^1] == '}' && token is not Enter;
    
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="token"/> is enter token
    /// </summary>
    /// <param name="token">The key token to test.</param>
    public static bool IsEnter(string token) =>
       token is Enter;
}
