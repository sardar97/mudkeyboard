# CLAUDE.md — MudKeyboard Project

## Important!! never commit i do commit myself

## Important!! always update the changelog
Every change MUST be recorded in `CHANGELOG.md` in the same edit as the change itself — no exceptions.
This applies to **any** new feature, behaviour change, bug fix, refactor with user-visible effect, or
documentation-site change. Do not consider a task done until the changelog is updated.

Rules:
- Add the entry under the **topmost / current version heading** in `CHANGELOG.md`, in the correct
  [Keep a Changelog](https://keepachangelog.com/) category: `### Added`, `### Changed`, `### Fixed`,
  `### Removed`, `### Deprecated`, `### Security`. Create the category subheading if it doesn't exist yet.
- Changes to the **documentation site only** (`src/MudKeyboard.Docs`) — anything that does not affect the
  published NuGet library/package — go under a `### Documentation` subsection of that version, and must say
  so, so a package consumer is never misled into thinking the library changed.
- Match the existing entry style: a **bold lead-in** naming the feature, then a sentence or two of detail;
  reference the public API names involved.
- `CHANGELOG.md` is the source of truth and may be more detailed than
  `src/MudKeyboard.Docs/Shared/ReleaseNotes.cs` (the curated /releases-page mirror). When a library
  feature/fix is added, keep `ReleaseNotes.cs` in sync; docs-site-only notes need not be mirrored there.

## What is this project?
MudKeyboard is an open-source Blazor virtual on-screen keyboard library.
- Keyboard core is JavaScript-free — pure C# and Blazor. A single optional JS shim powers ONE feature: the global docked keyboard that auto-shows on input focus (see "JavaScript policy" below).
- Built on MudBlazor components and theming
- AOT and trim-friendly (no reflection, no dynamic code)
- Targets .NET 8+ (no static SSR support — interactive Server and WASM only)
- MIT license, published to NuGet as `MudKeyboard`

## Solution layout
src/MudKeyboard/          ← The NuGet library (Razor Class Library)
demo/MudKeyboard.Demo.Server/   ← Interactive Blazor Server demo app
demo/MudKeyboard.Demo.Wasm/     ← Interactive Blazor WASM demo app
tests/MudKeyboard.Tests/        ← xUnit tests

## Architecture rules — READ BEFORE WRITING ANY CODE

### JavaScript policy (minimal, isolated)
The keyboard **core** (rendering, text engine, shift/caps/symbol toggle, all the inline components:
MudKeyboard, MudNumpad, MudPricepad, KeyGrid, KeyButton) must stay 100% JavaScript-free — no
IJSRuntime, all interactivity via Blazor event handlers. It must keep working with zero JS.

The ONE sanctioned exception is the **global docked keyboard** (`MudKeyboardHost`): a browser cannot
report focus on arbitrary inputs to Blazor without JS, so a single small ES module
(`wwwroot/mudKeyboard.js`) does global focus capture + caret-aware insertion. Rules for it:
- All JS interop is confined to `KeyboardInteropService` (constructor-injects IJSRuntime).
- Only primitive strings cross the interop boundary — no reflection-based (de)serialisation, so it
  stays AOT/trim-safe.
- Do not add JavaScript anywhere else, and do not make the core depend on it.

### AOT / Trim rules
- No reflection (no typeof(T) lookups at runtime, no Activator.CreateInstance)
- No dynamic, no object casting from unknown types
- Use [DynamicallyAccessedMembers] annotations if any generic constraints need it
- Prefer sealed classes where inheritance isn't needed
- All public API types must have [JsonSerializable] if serialised

### MudBlazor dependency
- MudBlazor is a peer dependency — do NOT bundle a second copy of MudTheme
- Use MudBlazor's existing MudThemeProvider cascade — the keyboard reads the current theme automatically
- Use MudBlazor components: MudPaper, MudButton, MudText, MudIconButton, MudTooltip
- Use MudBlazor CSS variables (--mud-palette-primary, --mud-palette-surface, etc.) for all colours
- Dark/light mode is automatic — because we use MudBlazor CSS variables, toggling MudThemeProvider dark mode cascades to the keyboard with zero extra code

### Component parameters
Every component must use [Parameter] with sensible defaults.
Never use raw HTML attributes where a Blazor parameter exists.
Use EventCallback<T> not Action<T> for user-facing callbacks.

### Blazor render modes
- Library components must be render-mode agnostic (no @rendermode in the library itself)
- The consuming app sets the render mode
- Demo apps use @rendermode InteractiveServer and @rendermode InteractiveWebAssembly respectively
- No static SSR (@rendermode="null") support — document this clearly

## Key components to build

### MudKeyboard (main component)
Parameters:
- `Value` (string) + `ValueChanged` (EventCallback<string>) — two-way bindable
- `Layout` (KeyboardLayout) — enum or custom layout object
- `Variant` (KeyboardVariant) — Full | Numpad | Pricepad | Custom
- `MaxLength` (int?) — optional cap
- `Disabled` (bool)
- `Class` / `Style` — passthrough CSS
- `OnEnter` (EventCallback) — fires when Enter key pressed
- `OnEscape` (EventCallback)

### MudNumpad
Simplified numpad: 1–9, 0, backspace, enter.
Decimal point optional via `AllowDecimal` (bool) parameter.

### MudPricepad
Like MudNumpad but:
- Formats output as currency string (e.g. "£1.23")
- `CurrencySymbol` (string) parameter, default "£"
- `DecimalPlaces` (int) parameter, default 2
- Enforces format automatically (last 2 digits are always pence)

### KeyButton (internal)
Internal building-block button. Not part of the public API surface.
Wraps MudButton with correct sizing, variant, and key press handling.

## KeyboardLayout model
```csharp
// Layout is just data — arrays of string arrays (rows of keys)
public sealed record KeyboardLayout
{
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
    // Special key tokens: "{bksp}", "{enter}", "{space}", "{shift}", "{caps}"
}
```

## Theme customisation
- Default: inherit from MudThemeProvider (automatic dark/light)
- `KeyboardPalette` optional parameter to override specific colours
- All colours via CSS variables — no inline styles with hardcoded hex

## Built-in layouts to include
- QWERTY English
- Numeric (0–9)
- Numpad (calculator layout)
- Price/Money

## NuGet package metadata (goes in MudKeyboard.csproj)
```xml
<PackageId>MudKeyboard</PackageId>
<Version>0.1.0-alpha</Version>
<Authors>Sardar Qaslany</Authors>
<Description>Blazor on-screen virtual keyboard with MudBlazor theming. The keyboard core is JavaScript-free; an optional, tiny JS shim adds a global docked keyboard that pops up when any input is focused. AOT and trim-friendly.</Description>
<PackageTags>blazor;mudblazor;virtual-keyboard;onscreen-keyboard;touch-keyboard;blazor-component</PackageTags>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageProjectUrl>https://github.com/sardar97/mudkeyboard</PackageProjectUrl>
<RepositoryUrl>https://github.com/sardar97/mudkeyboard</RepositoryUrl>
<GeneratePackageOnBuild>false</GeneratePackageOnBuild>
```

## CI/CD
- GitHub Actions: build + test on PR, publish to NuGet on tag push (v*)
- Use dotnet pack and dotnet nuget push
- NuGet API key stored as GitHub secret NUGET_API_KEY

## Code style
- File-scoped namespaces: `namespace MudKeyboard.Components;`
- Nullable enabled
- Use `required` keyword on model properties where applicable
- Razor files: parameters at top, lifecycle methods next, private methods last
- No code-behind (.razor.cs) unless the file exceeds ~150 lines

## What NOT to do
- Do not add JavaScript files to the library beyond the single focus-capture shim (`wwwroot/mudKeyboard.js`); the keyboard core stays JS-free
- Do not use Bootstrap — MudBlazor only
- Do not target .NET 6 or .NET 7
- Do not add static SSR render mode
- Do not use [Inject] in components — use [Parameter] or constructor injection in services. (Sole exception: `MudKeyboardHost` injects `KeyboardInteropService`, because a render-mode-agnostic component can't use constructor injection; the interop logic itself lives in the service via constructor injection.)
- Do not put business logic in .razor files — extract to C# classes