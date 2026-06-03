namespace MudKeyboard.Models;

/// <summary>
/// A keyboard layout expressed purely as data: an ordered collection of rows, where each
/// row is an ordered collection of key tokens. Tokens are either literal characters
/// (appended to the value when pressed) or command tokens wrapped in braces — see
/// <see cref="KeyTokens"/>.
/// </summary>
public sealed record KeyboardLayout
{
    /// <summary>
    /// The rows of the layout, top to bottom. Each inner list is one row of key tokens,
    /// ordered left to right.
    /// </summary>
    // Special key tokens: "{bksp}", "{enter}", "{space}", "{shift}", "{caps}"
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}
