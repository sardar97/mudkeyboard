using System.Text;

namespace MudKeyboard.Models;

/// <summary>
/// Optional per-keyboard colour overrides. Each slot holds a CSS colour value — a hex string
/// (<c>#1565c0</c>), an <c>rgb()</c>/<c>hsl()</c> expression, or even a <c>var(--…)</c> reference —
/// that recolours one part of the keyboard. Any slot you leave unset falls through to the ambient
/// MudBlazor theme, so dark/light mode keeps working automatically for everything you do not override.
/// </summary>
/// <remarks>
/// Overrides are emitted as MudBlazor palette CSS variables scoped to the keyboard's own root
/// element (see <see cref="Compose"/>), so they recolour only this keyboard — the rest of the app
/// keeps the app theme. The library hardcodes no colours of its own: when a slot is unset the key
/// resolves to the live <c>--mud-palette-*</c> value, exactly as before.
/// </remarks>
public sealed record KeyboardPalette
{
    /// <summary>Background of the keyboard surface. Drives <c>--mud-palette-surface</c>.</summary>
    public string? Surface { get; init; }

    /// <summary>Background of literal (non-accent) keys. Drives <c>--mud-palette-action-default-hover</c>.</summary>
    public string? KeyColor { get; init; }

    /// <summary>Label colour of literal keys. Drives <c>--mud-palette-text-primary</c>.</summary>
    public string? KeyTextColor { get; init; }

    /// <summary>Background of accent keys (Enter and an active shift). Drives <c>--mud-palette-primary</c>.</summary>
    public string? AccentColor { get; init; }

    /// <summary>Label colour of accent keys. Drives <c>--mud-palette-primary-text</c>.</summary>
    public string? AccentTextColor { get; init; }

    /// <summary>
    /// Composes the inline <c>style</c> for a keyboard root: this palette's CSS-variable overrides
    /// (if any) followed by the caller's own <paramref name="style"/>, so an explicit style always
    /// wins. Returns <paramref name="style"/> unchanged when <paramref name="palette"/> is
    /// <see langword="null"/> or sets no slots.
    /// </summary>
    /// <param name="palette">The palette to apply, or <see langword="null"/> for none.</param>
    /// <param name="style">The caller's existing inline style, appended after the overrides.</param>
    public static string? Compose(KeyboardPalette? palette, string? style)
    {
        if (palette is null)
        {
            return style;
        }

        var sb = new StringBuilder();
        palette.AppendVariables(sb);
        if (sb.Length == 0)
        {
            return style;
        }

        if (!string.IsNullOrWhiteSpace(style))
        {
            sb.Append(style);
        }

        return sb.ToString();
    }

    // Each slot drives the MudBlazor variable(s) that MudButton actually reads for that key.
    // AccentColor also sets the *-darken twin so hover/active stays on the chosen colour instead
    // of jumping to the theme's derived shade. The disabled-state background variable is left
    // untouched so disabled keys still read as disabled under a custom palette.
    private void AppendVariables(StringBuilder sb)
    {
        Append(sb, "--mud-palette-surface", Surface);
        Append(sb, "--mud-palette-action-default-hover", KeyColor);
        Append(sb, "--mud-palette-text-primary", KeyTextColor);
        Append(sb, "--mud-palette-primary", AccentColor);
        Append(sb, "--mud-palette-primary-darken", AccentColor);
        Append(sb, "--mud-palette-primary-text", AccentTextColor);
    }

    private static void Append(StringBuilder sb, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.Append(name).Append(':').Append(value).Append(';');
        }
    }
}
