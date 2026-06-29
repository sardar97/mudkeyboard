namespace MudKeyboard.Models;

/// <summary>
/// The kind of input produced by a key press when a <see cref="MudKeyboard.Components.MudKeyboard"/>
/// runs in emit mode (see <see cref="MudKeyboard.Components.MudKeyboard.OnInput"/>).
/// </summary>
public enum KeyboardInputKind
{
    /// <summary>A literal character (or space) to insert, carried in <see cref="KeyboardInput.Text"/>.</summary>
    Text,

    /// <summary>Delete the character before the caret.</summary>
    Backspace,

    /// <summary>The Enter key was pressed (commit / newline).</summary>
    Enter,

    /// <summary>The Escape key was pressed (dismiss).</summary>
    Escape,

    /// <summary>Toggle the sign of the numeric value (positive ↔ negative).</summary>
    Sign,
}

/// <summary>
/// A single emitted key press. Used by emit mode so a host (such as the docked
/// <see cref="MudKeyboard.Components.MudKeyboardHost"/>) can route input to a target other than the
/// keyboard's own <see cref="MudKeyboard.Components.MudKeyboard.Value"/> — for example the caret of
/// the currently focused field.
/// </summary>
/// <param name="Kind">What the press represents.</param>
/// <param name="Text">The literal text to insert when <see cref="Kind"/> is <see cref="KeyboardInputKind.Text"/>; otherwise empty.</param>
public readonly record struct KeyboardInput(KeyboardInputKind Kind, string Text = "")
{
    /// <summary>Creates a literal-text input.</summary>
    /// <param name="text">The character(s) to insert.</param>
    public static KeyboardInput Char(string text) => new(KeyboardInputKind.Text, text);

    /// <summary>A backspace input.</summary>
    public static KeyboardInput Backspace { get; } = new(KeyboardInputKind.Backspace);

    /// <summary>An Enter input.</summary>
    public static KeyboardInput Enter { get; } = new(KeyboardInputKind.Enter);

    /// <summary>An Escape input.</summary>
    public static KeyboardInput Escape { get; } = new(KeyboardInputKind.Escape);

    /// <summary>A sign-toggle input (the <c>±</c> key).</summary>
    public static KeyboardInput Sign { get; } = new(KeyboardInputKind.Sign);
}
