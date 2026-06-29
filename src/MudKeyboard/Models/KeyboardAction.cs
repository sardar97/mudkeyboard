namespace MudKeyboard.Models;

/// <summary>
/// The toolbar action buttons of the global docked keyboard
/// (<see cref="MudKeyboard.Components.MudKeyboardHost"/>). A <see cref="System.FlagsAttribute"/> set, so
/// members combine with <c>|</c> and are removed with <c>&amp; ~</c>. Drives the host's
/// <c>VisibleActions</c> (which buttons render) and <c>DisabledActions</c> (which render greyed-out)
/// parameters, letting a consumer hide or disable any button — globally or one at a time.
/// </summary>
/// <example>
/// <code>
/// @* Hide the Copy and Paste buttons, keep the rest *@
/// &lt;MudKeyboardHost VisibleActions="@(KeyboardAction.All &amp; ~(KeyboardAction.Copy | KeyboardAction.Paste))" /&gt;
///
/// @* Show the Clear button but render it disabled *@
/// &lt;MudKeyboardHost DisabledActions="KeyboardAction.Clear" /&gt;
/// </code>
/// </example>
[Flags]
public enum KeyboardAction
{
    /// <summary>No toolbar buttons. Use as <c>VisibleActions</c> to hide the whole toolbar.</summary>
    None = 0,

    /// <summary>The <em>Clear</em> button — empties the focused field.</summary>
    Clear = 1 << 0,

    /// <summary>The <em>Copy</em> button — copies the focused field to the clipboard.</summary>
    Copy = 1 << 1,

    /// <summary>The <em>Paste</em> button — pastes the clipboard into the focused field.</summary>
    Paste = 1 << 2,

    /// <summary>The <em>move-cursor-left</em> arrow. Never shown on the money keypad.</summary>
    CursorLeft = 1 << 3,

    /// <summary>The <em>move-cursor-right</em> arrow. Never shown on the money keypad.</summary>
    CursorRight = 1 << 4,

    /// <summary>The <em>Hide</em> (chevron-down) button — collapses the docked keyboard.</summary>
    Hide = 1 << 5,

    /// <summary>Both cursor arrows (<see cref="CursorLeft"/> and <see cref="CursorRight"/>).</summary>
    CursorControl = CursorLeft | CursorRight,

    /// <summary>Every toolbar button. The default for <c>VisibleActions</c>.</summary>
    All = Clear | Copy | Paste | CursorControl | Hide,
}
