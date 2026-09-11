namespace MudKeyboard.Docs.Pages.Features;

/// <summary>
/// Code-behind for the Theming page holding its code samples. They live here, not in the
/// <c>@code</c> block of <c>Theming.razor</c>, on purpose: the samples are Razor snippets that contain
/// <c>@code {</c> and <c>@* … *@</c> at the start of a line, and Rider's Razor parser treats those inside
/// a C# raw string literal as real directives — closing the code block early and flagging dozens of
/// phantom errors even though the Razor compiler (and the build) is perfectly happy. A plain .cs file
/// has no Razor parser, so the snippets are just strings.
/// </summary>
public partial class Theming
{
    private string LiveCode =>
        $$"""
        <MudKeyboard @bind-Value="_value" Palette="Brand" />

        @code {
            private static readonly KeyboardPalette Brand = new()
            {
                AccentColor = "{{_chosen}}",
                AccentTextColor = "#ffffff",
            };
        }
        """;

    private const string AmbientCode = """
@* No palette — inherits the MudBlazor theme, dark/light and all *@
<MudKeyboard @bind-Value="_value" Variant="KeyboardVariant.Numpad" />
""";

    private const string AccentCode = """
<MudKeyboard @bind-Value="_value" Palette="Brand" />

@code {
    private static readonly KeyboardPalette Brand = new()
    {
        AccentColor = "#00897b",      // Enter / active-shift keys
        AccentTextColor = "#ffffff",
        // Surface, KeyColor, KeyTextColor unset → follow the theme
    };
}
""";

    private const string FullCode = """
<MudKeyboard @bind-Value="_value" Variant="KeyboardVariant.Numpad" Palette="Custom" />

@code {
    private static readonly KeyboardPalette Custom = new()
    {
        Surface = "#1a1a2e",
        KeyColor = "#16213e",
        KeyTextColor = "#e7e7ff",
        AccentColor = "#e94560",
        AccentTextColor = "#ffffff",
    };
}
""";
}
