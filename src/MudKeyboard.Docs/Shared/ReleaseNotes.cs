namespace MudKeyboard.Docs.Shared;

/// <summary>How a release is labelled (and coloured) on the Releases page.</summary>
public enum ReleaseStatus
{
    /// <summary>Merged on <c>master</c> but not yet tagged / published.</summary>
    Unreleased,

    /// <summary>The most recent published, stable release.</summary>
    Latest,

    /// <summary>An earlier stable release.</summary>
    Stable,

    /// <summary>A pre-release / preview (alpha, beta, rc).</summary>
    PreRelease,
}

/// <summary>A single bullet in a release's change list.</summary>
/// <param name="Title">Short, bold lead-in (the headline of the change).</param>
/// <param name="Detail">The rest of the description.</param>
public sealed record ChangeItem(string Title, string Detail);

/// <summary>A group of changes under one Keep a Changelog heading (<c>Added</c> / <c>Changed</c> / <c>Fixed</c>).</summary>
public sealed record ChangeGroup(string Heading, IReadOnlyList<ChangeItem> Items);

/// <summary>One released (or not-yet-released) version of the library.</summary>
/// <param name="Version">The semantic version, e.g. <c>1.0.1</c>, or <c>"Unreleased"</c>.</param>
/// <param name="Date">ISO date the version shipped, or <see langword="null"/> when unreleased.</param>
/// <param name="Status">How to badge the entry.</param>
/// <param name="Groups">The grouped change lists (may be empty).</param>
/// <param name="Summary">Optional prose shown above the groups.</param>
public sealed record ReleaseNote(
    string Version,
    string? Date,
    ReleaseStatus Status,
    IReadOnlyList<ChangeGroup> Groups,
    string? Summary = null)
{
    /// <summary>Whether this entry has been published (i.e. carries a date).</summary>
    public bool IsReleased => Status is not ReleaseStatus.Unreleased;

    /// <summary>A stable URL fragment for deep-linking to this release.</summary>
    public string Anchor => IsReleased ? $"v{Version}" : "unreleased";
}

/// <summary>
/// The MudKeyboard release history shown on the <c>/releases</c> page. This mirrors
/// <c>CHANGELOG.md</c> at the repository root — keep the two in sync when cutting a release
/// (see <c>RELEASE.md</c>). The full, verbatim changelog lives on GitHub.
/// </summary>
public static class ReleaseNotes
{
    /// <summary>Link to the verbatim changelog in the repository.</summary>
    public const string ChangelogUrl = "https://github.com/sardar97/mudkeyboard/blob/master/CHANGELOG.md";

    /// <summary>Link to the GitHub Releases page.</summary>
    public const string GitHubReleasesUrl = "https://github.com/sardar97/mudkeyboard/releases";

    /// <summary>Every release, newest first.</summary>
    public static IReadOnlyList<ReleaseNote> All { get; } =
    [
        new ReleaseNote(
            Version: "1.2.0",
            Date: "2026-06-29",
            Status: ReleaseStatus.Latest,
            Groups:
            [
                new ChangeGroup("Added",
                [
                    new ChangeItem("Live value preview with edit & cancel on the docked keyboard",
                        "Set MudKeyboardHost.ShowValuePreview=\"true\" to show a bar at the top of the docked keyboard with the focused field's live value — so the user always sees what they're editing, even when the field sits behind the panel. Focusing a field that already contains text (say \"sardar\") shows it immediately; the keys edit it live, preserving all existing binding, EditForm validation and SSR-form behaviour. Backed by a new OnValueChanged interop callback (the JS shim reports the value back on every change — on-screen and hardware typing — gated on the feature, so there's no overhead when it's off) and KeyboardInteropService.CurrentValue."),
                    new ChangeItem("Cancel / revert and an optional backdrop",
                        "ShowBackdrop renders a dimming backdrop behind the docked keyboard (themed via the MudBlazor --mud-palette-overlay-dark variable); a backdrop click cancels the edit — reverting the field to the value it held when it was focused (OriginalValue / CancelAsync) — and closes. DisableBackdropClick keeps the backdrop from dismissing and shows a Cancel button in the preview bar instead, so the keyboard stays open until the user confirms (⏎ or the ⌄ Hide button) or cancels; rename it with CancelLabel. All opt-in."),
                    new ChangeItem("Key click sound on every keyboard — still 100% JavaScript-free",
                        "A new Sound parameter on MudKeyboard, MudNumpad, MudPricepad and MudKeyboardHost plays a short click on every key press, rendered as a Blazor <audio> element (re-mounted per press so the browser autoplays it) — no IJSRuntime, no JS file, so it works for the inline keyboards and the docked keyboard and keeps the JS-free-core rule intact. The default click is synthesised in pure C# (a windowed sine burst exposed as a data: URI — no shipped asset, AOT/trim-safe); point SoundSrc at any URL or data: URI to use your own. Off by default."),
                ]),
            ]),

        new ReleaseNote(
            Version: "1.1.0",
            Date: "2026-06-29",
            Status: ReleaseStatus.Stable,
            Groups:
            [
                new ChangeGroup("Added",
                [
                    new ChangeItem("Negative numbers on the numeric keypads — an optional ± key (#3)",
                        "The number, decimal and money keypads are positive-only by default; opt in to a ± sign-toggle key. Inline MudNumpad/MudPricepad gain an AllowNegative parameter (the pricepad formats negatives as -£1.23, and zero is never shown negative). For the docked keyboard, MudKeyboardNumericField gains AllowNegative per field (via a data-mudkeyboard-allow-negative attribute) with a global MudKeyboardHost.AllowNegative default. Backed by a new {sign} token, signed layout variants and money-formatter sign support — AOT/trim-safe, with runnable examples on the Numpad, Pricepad and Docked keyboard pages."),
                    new ChangeItem("Show, hide or disable any docked-keyboard toolbar button (#5)",
                        "MudKeyboardHost gains VisibleActions (default All) and DisabledActions (default None), both typed as a new [Flags] enum KeyboardAction (Clear, Copy, Paste, CursorLeft, CursorRight, CursorControl, Hide, None, All). Hide a single button with VisibleActions=\"@(KeyboardAction.All & ~KeyboardAction.Paste)\", drop the whole toolbar with KeyboardAction.None, or grey one out with DisabledActions=\"KeyboardAction.Clear\" — globally or one at a time. Hiding wins over disabling, and hiding every action removes the role=\"toolbar\" element entirely."),
                    new ChangeItem("MudKeyboardNumericField",
                        "Type-aware numeric keypads for the docked keyboard. A generic wrapper over MudNumericField<T> that picks the docked keypad from the bound CLR type: decimal → the money keypad (pence-first, like MudPricepad), double/float → the numeric keypad with a decimal point, and integer types → the numeric keypad without one. AOT/trim friendly (a single trim-safe typeof comparison)."),
                    new ChangeItem("Accessibility, end to end",
                        "Every keyboard surface is now a labelled role=\"group\" and each key is a real <button> with a spoken aria-label (⌫ reads \"Backspace\", the space bar reads \"Space\", 123/ABC reads \"Numbers and symbols\"/\"Letters\"). Shift/caps and the symbol toggle expose aria-pressed. New AriaLabel parameter and KeyboardKey.AccessibleLabel, plus a dedicated Accessibility docs page."),
                    new ChangeItem("Static SSR support for the docked keyboard",
                        "The global docked keyboard now works on Blazor static Server-Side Rendering pages (.NET 8+). Place <MudKeyboardHost> in App.razor outside <Routes> with its own interactive render mode and it works on every page, statically rendered ones included."),
                    new ChangeItem("MudKeyboardTextField",
                        "A generic wrapper over MudTextField<T> that opts a field into the docked keyboard via DockedKeyboard=\"true\" (emitting data-mudkeyboard) and an optional DockedKeyboardLayout. Forwards the common text-field parameters and any extra attributes, and is AOT/trim friendly."),
                ]),
                new ChangeGroup("Changed",
                [
                    new ChangeItem("Docked keyboard hidden while closed",
                        "When the panel is off-screen, MudKeyboardHost sets inert and aria-hidden=\"true\" so its keys leave the tab order and the accessibility tree until a field is focused; both clear automatically on open. Its action bar is now a labelled role=\"toolbar\"."),
                    new ChangeItem("Better on every device",
                        "The panel clamps to 100vw (no horizontal scrolling on phones), pads past device safe-area insets, scrolls its keys instead of overflowing on short/landscape screens, shows a theme-coloured focus ring, keeps comfortable touch targets and strengthens key edges under high/forced-contrast modes."),
                    new ChangeItem("Writes through the native input setter",
                        "Every keystroke sets the field's value via the native value setter and dispatches both input and change events, so the typed value flows correctly into static-SSR Blazor forms, plain HTML form POSTs and non-Blazor inputs as well as MudBlazor bindings."),
                    new ChangeItem("Self-contained toolbar tooltips",
                        "The docked keyboard's toolbar now uses the native title attribute instead of MudTooltip, so MudKeyboardHost needs no MudPopoverProvider ancestor and can live in its own interactive island (as the static-SSR setup requires) without rendering duplicate popovers."),
                ]),
                new ChangeGroup("Fixed",
                [
                    new ChangeItem("Commit + validate the field on close (#4)",
                        "The docked keyboard edits the field through the native value setter, which queues no native 'change' — so a non-immediate field (e.g. MudNumericField with Min/Max) used to keep the typed text on screen without committing or validating it (typing 100 then tapping away left 100 visible while the bound value never updated). The shim now mirrors a hardware keyboard's change-before-blur order: it commits the field the instant you press outside it, before the browser blurs it, as well as when the Hide/Enter buttons close the panel. That lets MudNumericField's own blur handler run, so the value commits, validation runs, and the displayed text is re-formatted to the validated value — 100 becomes the clamped 10 in the binding and on screen, even when the clamp equals the value already bound. The per-keystroke behaviour is unchanged, so the money keypad still types cleanly."),
                    new ChangeItem("Safe during prerendering",
                        "MudKeyboardHost guards its JavaScript initialization, so if the JS runtime is not yet available or the circuit has already disconnected it silently does nothing instead of throwing."),
                ]),
            ]),

        new ReleaseNote(
            Version: "1.0.1",
            Date: "2026-06-04",
            Status: ReleaseStatus.Stable,
            Groups:
            [
                new ChangeGroup("Fixed",
                [
                    new ChangeItem("No longer ships Blazor scoped CSS",
                        "The docked keyboard's styles previously lived in MudKeyboardHost.razor.css, which made the package contribute a scoped-CSS bundle and could crash a consuming app's build with \"Sequence contains more than one element\". The styles now ship as a plain static asset (_content/MudKeyboard/MudKeyboard.css) loaded via a <link>, so the library contributes no scoped CSS. No code changes are required when upgrading."),
                ]),
            ]),

        new ReleaseNote(
            Version: "1.0.0",
            Date: "2026-06-04",
            Status: ReleaseStatus.Stable,
            Summary: "First stable release. Promotes the 0.1.0-alpha preview to a stable 1.0.0 under Semantic Versioning — the public API surface is now considered stable and subsequent changes follow SemVer. No functional changes from 0.1.0-alpha.",
            Groups: []),

        new ReleaseNote(
            Version: "0.1.0-alpha",
            Date: "2026-06-03",
            Status: ReleaseStatus.PreRelease,
            Summary: "First public preview.",
            Groups:
            [
                new ChangeGroup("Added",
                [
                    new ChangeItem("MudKeyboard",
                        "Full on-screen keyboard with two-way @bind-Value, layout switching, one-shot shift, caps lock (double-tap shift) and a numbers/symbols face via the {sym} toggle."),
                    new ChangeItem("MudNumpad",
                        "Calculator-style numeric pad with an optional decimal point (AllowDecimal)."),
                    new ChangeItem("MudPricepad",
                        "Pence-first currency pad with configurable CurrencySymbol and DecimalPlaces."),
                    new ChangeItem("MudKeyboardHost",
                        "Global docked keyboard that slides up when any input is focused and types at the caret, with clear / copy / paste / cursor controls. Backed by a single optional JS focus-capture shim; register with services.AddMudKeyboard()."),
                    new ChangeItem("KeyboardAttachMode & data attributes",
                        "AllInputs / OptIn modes plus data-mudkeyboard, data-mudkeyboard-ignore and data-mudkeyboard-layout to control which fields attach and which layout they show."),
                    new ChangeItem("KeyboardPalette",
                        "Optional per-keyboard colour overrides scoped via MudBlazor CSS variables; unset slots inherit the ambient theme so dark/light mode keeps working."),
                    new ChangeItem("Layout building blocks",
                        "KeyboardLayout, KeyboardKey, KeyboardVariant, KeyTokens and LayoutLibrary (Qwerty, Symbols, Numeric, Numpad, NumpadWithDecimal, Price) for building custom layouts."),
                    new ChangeItem("Automatic MudBlazor theming",
                        "Colours come entirely from MudBlazor CSS variables, so dark/light mode cascades with zero extra code."),
                    new ChangeItem("Multi-targeting & AOT",
                        "Targets net8.0, net9.0 and net10.0 with IsAotCompatible enabled (trim/AOT analyzers run on every build) and XML documentation shipped in the package."),
                ]),
            ]),
    ];
}
