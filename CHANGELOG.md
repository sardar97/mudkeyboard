# Changelog

All notable changes to **MudKeyboard** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] — 2026-06-29

### Added
- **Show, hide or disable any docked-keyboard toolbar button — globally or one at a time**
  ([#5](https://github.com/sardar97/mudkeyboard/issues/5)). `MudKeyboardHost` gains two parameters,
  `VisibleActions` (default `All`) and `DisabledActions` (default `None`), both typed as a new
  `[Flags]` enum **`KeyboardAction`** (`Clear`, `Copy`, `Paste`, `CursorLeft`, `CursorRight`,
  `CursorControl`, `Hide`, plus `None`/`All`). Drop a single button —
  `VisibleActions="@(KeyboardAction.All & ~KeyboardAction.Paste)"` — remove the whole toolbar with
  `KeyboardAction.None`, or keep a button visible but greyed-out via
  `DisabledActions="KeyboardAction.Clear"`. Hiding wins over disabling, and the cursor arrows still
  never appear on the money keypad. When every action is hidden the `role="toolbar"` element is
  dropped entirely so no empty toolbar lingers in the accessibility tree. Documented on the
  *Docked keyboard* and *API reference* pages.
- **`MudKeyboardNumericField` — type-aware numeric keypads for the docked keyboard.** A new generic
  (`@typeparam T`) wrapper over `MudNumericField<T>` that chooses the docked keypad from the bound CLR
  type, with no data attribute to remember: `decimal` → the **money** keypad (pence-first, like
  `MudPricepad`), `double`/`float` → the numeric keypad **with** a `.` key, and integer types
  (`int`, `long`, `short`, …) → the numeric keypad **without** a `.` key. This closes a gap that
  JavaScript alone cannot: a `decimal` and a `double` both render `inputmode="decimal"`, so only the
  bound type can distinguish currency from a plain decimal. It forwards the common `MudNumericField`
  parameters (`For`, `Format`, `Min`/`Max`/`Step`, `Adornment`, …) and any extra attributes, exposes an
  optional `DockedKeyboardLayout` override and a `DockedKeyboard` opt-in marker, and is AOT/trim friendly
  (a single trim-safe `typeof` comparison — no member reflection). Lives in the `MudKeyboard.Components`
  namespace. Documented on the *Docked keyboard* page and demonstrated in both demos.
- **Accessibility, end to end.** Every keyboard surface is now a labelled `role="group"`, and each key is a
  real `<button>` carrying a spoken `aria-label` (so `⌫` reads *"Backspace"*, the blank space bar reads
  *"Space"*, `123`/`ABC` reads *"Numbers and symbols"*/*"Letters"*, etc.). The shift/caps and symbol-toggle
  keys expose `aria-pressed` so assistive tech announces their on/off state. A new
  `KeyboardKey.AccessibleLabel` property exposes the spoken name, and a new **`AriaLabel`** parameter on
  `MudKeyboard`, `MudNumpad` and `MudPricepad` sets the group's accessible name (defaults
  *"On-screen keyboard"* / *"Numeric keypad"* / *"Price entry keypad"*). A new **Accessibility** page in the
  docs and an accessibility note in both demos document it all.
- **Static SSR support for the global docked keyboard.** The docked keyboard now works on Blazor
  **static Server-Side Rendering** pages (.NET 8+). Because it edits the focused field through
  JavaScript on `document.activeElement` rather than via Blazor binding, it needs no per-page
  interactivity — place `<MudKeyboardHost>` in `App.razor` outside `<Routes>` (with its own
  interactive render mode) and it works on every page, statically rendered ones included. See the new
  *Static SSR support* section in the README and the runnable `/components/ssr-login-demo` page in the
  Server demo.
- **`MudKeyboardTextField`** — a generic (`@typeparam T`) wrapper over `MudTextField<T>` that opts a
  field into the docked keyboard via `DockedKeyboard="true"` (emitting `data-mudkeyboard`) and an
  optional `DockedKeyboardLayout` (emitting `data-mudkeyboard-layout`). It forwards the common
  text-field parameters and any extra attributes (such as `name`), and is AOT/trim friendly. Lives in
  the `MudKeyboard.Components` namespace.

### Changed
- **The docked keyboard is now hidden from assistive tech and the tab order while closed.** When the panel
  is off-screen, `MudKeyboardHost` sets `inert` and `aria-hidden="true"` on the dock, so its keys and tools
  are no longer reachable by Tab or announced by screen readers until a field is focused; both clear
  automatically on open. Its action bar is now a labelled `role="toolbar"`, and the inner keyboard carries
  its own `aria-label` ("Keyboard keys").
- **The docked keyboard now behaves better on every device.** The panel clamps to `100vw` (no more
  horizontal scrolling on phones), pads past device safe-area insets (the iOS home indicator / notches —
  honours `viewport-fit=cover`), scrolls its keys instead of overflowing on short/landscape screens, shows
  a clear theme-coloured focus ring for keyboard users, keeps comfortable touch targets on small phones,
  and strengthens key edges under high/forced-contrast modes. The demo and docs host pages now set
  `viewport-fit=cover`.
- **Focus-capture shim now writes through the native input setter and dispatches `change`.** Every
  keystroke from the docked keyboard sets the field's value via the native
  `HTMLInputElement`/`HTMLTextAreaElement` `value` setter and dispatches both `input` and `change`
  events. This makes the typed value flow correctly into static-SSR Blazor forms, plain HTML form
  POSTs and non-Blazor inputs, in addition to MudBlazor immediate and non-immediate bindings. The lone
  exception is MudBlazor's numeric field (`role="spinbutton"`): it owns its formatted text and re-derives
  it from its parsed value, so it accepts the value on `input` alone — firing a trailing `change` on it
  would make it discard a programmatically-set value and snap back. The shim detects the spinbutton role
  and dispatches `input` only for it (its value still reaches an SSR POST via the native setter), which
  is what lets the docked **money** keypad drive a `MudNumericField`/`MudKeyboardNumericField` correctly.
- **The docked keyboard's toolbar tooltips now use the native `title` attribute** instead of
  `MudTooltip`. `MudTooltip` requires a `MudPopoverProvider` in scope; dropping it makes
  `MudKeyboardHost` fully self-contained, so it can be placed in its own interactive island (in
  `App.razor`, outside `<Routes>`) — as the static-SSR setup requires — without a provider as an
  ancestor and without rendering duplicate popovers when it sits in a normal layout. Every toolbar
  button keeps its `aria-label`.

### Fixed
- **The docked keyboard now commits and validates the focused field when it closes**
  ([#4](https://github.com/sardar97/mudkeyboard/issues/4)). The keyboard edits the field through the
  native value setter, which queues no native `change` — so a field with the default *non-immediate*
  binding (for example `<MudNumericField @bind-Value="x" Min="-10" Max="10"/>`) used to keep the typed
  text on screen without committing or validating it: typing `100` then tapping away left `100` showing
  while the bound value never updated. The focus-capture shim now mirrors a hardware keyboard's
  `change`→`blur` order — it commits the field the instant you press outside it, **before** the browser
  blurs it, as well as when the **Hide**/**Enter** buttons close the panel. That lets `MudNumericField`'s
  own blur handler run, so the value flows into the binding, the field's validation runs, and the
  displayed text is re-formatted to the validated value: `100` becomes the clamped `10` both in the
  binding and on screen — even when the clamped result equals the value already bound, the case that
  previously left the out-of-range text stuck. The per-keystroke behaviour is unchanged, so the money
  keypad still types cleanly and tapping keys never commits early. Verified end to end in a real browser
  and demonstrated by a new non-immediate `Min`/`Max` field in both demos.
- **`MudKeyboardHost` no longer risks throwing during prerendering.** Its JavaScript initialization is
  guarded so that, if the JS runtime is not yet available (for example a stray prerender pass) or the
  circuit has already disconnected, the host silently does nothing instead of throwing.

## [1.0.1] — 2026-06-04

### Fixed
- **No longer ships Blazor scoped CSS.** The docked keyboard's styles previously lived in
  `MudKeyboardHost.razor.css`, which made the package a Razor Class Library that contributes a
  scoped-CSS bundle. Referencing such a library switches on the *consuming* app's
  `{AppName}.styles.css` generation — and if the app already had a stray physical/checked-in file of
  that name, the build crashed with `InvalidOperationException: Sequence contains more than one
  element` in `GenerateStaticWebAssetsDevelopmentManifest`. The styles now ship as a plain static
  asset (`_content/MudKeyboard/MudKeyboard.css`) that `MudKeyboardHost` auto-loads via a `<link>`, so
  the library contributes no scoped CSS and can never trigger that collision. No code changes are
  required when upgrading — the dock styling still loads automatically.

## [1.0.0] — 2026-06-04

First stable release. Promotes the `0.1.0-alpha` preview to a stable `1.0.0` under
[Semantic Versioning](https://semver.org/spec/v2.0.0.html) — the public API surface is now considered
stable and subsequent changes will follow SemVer. No functional changes from `0.1.0-alpha`.

## [0.1.0-alpha] — 2026-06-03

First public preview.

### Added
- **`MudKeyboard`** — full on-screen keyboard with two-way `@bind-Value`, layout switching, one-shot
  shift, caps lock (double-tap shift) and a numbers/symbols face via the `{sym}` toggle.
- **`MudNumpad`** — calculator-style numeric pad with an optional decimal point (`AllowDecimal`).
- **`MudPricepad`** — pence-first currency pad with configurable `CurrencySymbol` and `DecimalPlaces`.
- **`MudKeyboardHost`** — global docked keyboard that slides up when any input is focused and types at
  the caret, with clear / copy / paste / cursor controls. Backed by a single optional JS focus-capture
  shim; register with `services.AddMudKeyboard()`.
- **`KeyboardAttachMode`** (`AllInputs` / `OptIn`) plus `data-mudkeyboard`, `data-mudkeyboard-ignore`
  and `data-mudkeyboard-layout` attributes to control which fields attach and which layout they show.
- **`KeyboardPalette`** — optional per-keyboard colour overrides scoped via MudBlazor CSS variables;
  unset slots inherit the ambient theme so dark/light mode keeps working.
- **`KeyboardLayout`**, **`KeyboardKey`**, **`KeyboardVariant`**, **`KeyTokens`** and **`LayoutLibrary`**
  (`Qwerty`, `Symbols`, `Numeric`, `Numpad`, `NumpadWithDecimal`, `Price`) for building custom layouts.
- Automatic MudBlazor theming — colours come entirely from MudBlazor CSS variables, so dark/light mode
  cascades with zero extra code.
- Multi-targeting for `net8.0`, `net9.0` and `net10.0`, with `IsAotCompatible` enabled (trim/AOT
  analyzers run on every build) and XML documentation shipped in the package.

[1.1.0]: https://github.com/sardar97/mudkeyboard/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/sardar97/mudkeyboard/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/sardar97/mudkeyboard/compare/v0.1.0-alpha...v1.0.0
[0.1.0-alpha]: https://github.com/sardar97/mudkeyboard/releases/tag/v0.1.0-alpha
