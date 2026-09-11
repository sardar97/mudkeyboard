# Changelog

All notable changes to **MudKeyboard** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] — 2026-09-11

### Changed
- **MudBlazor 9.9.0 is now the minimum.** The package's `MudBlazor` dependency floor moves from 9.5.0 to
  9.9.0 (the framework floors move to `Microsoft.AspNetCore.Components.Web` 8.0.31 / 9.0.20 / 10.0.12
  per target). The demos, docs site and tests build against the same versions (bunit 2.10.3,
  Microsoft.NET.Test.Sdk 18.10.0, xunit.runner.visualstudio 4.0.0).
- **A numeric field's spin buttons never open the docked keyboard.** Pressing a `MudNumericField`
  ▲/▼ spin button makes MudBlazor focus the field, which used to pop the keyboard up for a field the
  user never meant to type into. The focus-capture shim now recognises a press inside
  `.mud-input-numeric-spin` and ignores the focus it causes: the spin button just steps the value. If the
  keyboard was open for a *different* field, that edit is committed and the keyboard closes. Tapping the
  (already focused) field itself opens the keyboard — the shim opens on the tap, since the browser fires
  no `focusin` for a field that already has focus.
- **Changes made from outside the keyboard are always reported to the host.**
  `KeyboardInteropService.OnValueChanged` gained an `external` flag: the shim reports hardware typing,
  spin-button / ArrowUp / ArrowDown steps and app code setting the bound value regardless of
  `ShowValuePreview`, because the keypad state depends on them (below). `ReportValueChanges` now gates
  only the keyboard's *own* edits and caret moves (which only the preview bar consumes), so the
  "no overhead when the preview is off" property still holds for on-screen typing.
  `KeyboardInteropService.CurrentValue` is therefore always current after an outside change.

### Fixed
- **The value preview and the keypads now follow spin buttons, arrow keys and app changes**
  ([#9](https://github.com/sardar97/mudkeyboard/issues/9)). Blazor updates an `<input>` by assigning
  `element.value`, which fires no `input` event — so when a `MudNumericField`'s ▲/▼ spin button (or
  ArrowUp/ArrowDown, or app code) changed the value while the docked keyboard was open, the value-preview
  bar kept showing the old value and the keypad's "first digit replaces the value" rule stayed armed, so
  the next digit wiped the value the spin button had just produced (5 → ▲ → 6 → tap 1 → `1`). The shim now
  shadows `value` on the focused element with an accessor that forwards to the native setter and reports
  every programmatic write (removed again when the field is no longer being edited); the host then
  updates the preview, drops the pending replace and re-seeds the pence-first money accumulator from the
  field, so the next digit appends to what is really there (6 → tap 1 → `61`; a money field spun to
  `7.00` then `5` → `70.05`). `OriginalValue` (what Cancel reverts to) is unaffected.
- **Tapping into a numeric field no longer disarms "first digit replaces the value".** The caret snap on
  pointer-up reported the (unchanged) value back to the host, which treated it as an edit — so with the
  preview bar on (the default), the first digit tapped into a pre-filled field appended instead of
  replacing (`30` → tap 3·0·0 → `30300`). Only a real value change cancels the replace now.
- **A clamped `MudNumericField` shows the clamped text every time it is closed, not just the first**
  ([#8](https://github.com/sardar97/mudkeyboard/issues/8)). Typing `300` into a `Max="30"` field and
  pressing ⏎ clamps the bound value to 30 and shows `30`; doing it again could leave `300` on screen
  while the bound value stayed 30, because the field's text settled on the *same* string Blazor had last
  rendered and the DOM (typed into directly by the shim) was never rewritten. After closing a
  `role="spinbutton"` field the shim now watches its `aria-valuetext` / `aria-valuenow` — the settled
  text MudBlazor does re-render — for a moment and copies it into the field whenever the two disagree.
  Display-only: it fires no events and never touches a field that is focused or being edited again.

### Documentation
_Documentation-site (`src/MudKeyboard.Docs`) and demo-app changes only — no further change to the published library/package._
- **Docked keyboard page — built-in behaviour.** Documents that spin buttons never open the keyboard,
  that outside changes resume editing (and drive the preview), and the settled-text sync on close. The
  same notes are mirrored in `skill.md` / `llms-full.txt`; the "what's new" badges now advertise 1.3.0.
- **Demo apps (Server + WASM).** New *Raw MudNumericField · Min/Max clamp + spin buttons (GitHub #8, #9)*
  section with the reporters' exact fields, for regression checks.
- **README.** `ShowValuePreview` is documented with its real default (`true`).

## [1.2.0] — 2026-06-29

### Added
- **Choose where the docked keyboard appears — top, centre or bottom.** A new
  **`MudKeyboardHost.Position`** parameter (enum `KeyboardPosition`) places the global docked keyboard at the
  `Bottom` (the default — slides up from the bottom edge, unchanged), `Top` (slides down from the top edge)
  or `Center` (floats centred in the viewport, fading into view). It's pure CSS — the keyboard core stays
  100% JavaScript-free — and the panel's rounded corners adapt to the chosen edge. Documented on the
  *Docked keyboard* page with a live example, selectable in the *Playground*, and listed (with the new
  `KeyboardPosition` enum) on the *API reference* page.
- **Live value preview with edit & cancel on the docked keyboard.** Set
  **`MudKeyboardHost.ShowValuePreview="true"`** to show a bar at the top of the docked keyboard with the
  focused field's *live* value — so the user always sees what they're editing, even when the field sits
  behind the panel. Focusing a field that already contains text (say `sardar`) shows it immediately; the
  keys edit it live (all existing binding, `EditForm` validation and SSR-form behaviour is preserved).
  Backed by a new `OnValueChanged` interop callback (the JS shim reports the focused field's value back
  on every change — on-screen *and* hardware typing — gated on the feature, so there's no overhead when
  it's off) and a new `KeyboardInteropService.CurrentValue`.
- **Cancel / revert and an optional backdrop on the docked keyboard.** New
  **`MudKeyboardHost.ShowBackdrop`** renders a dimming backdrop behind the docked keyboard (themed via the
  MudBlazor `--mud-palette-overlay-dark` variable); a backdrop click **cancels** the edit — reverting the
  field to the value it held when it was focused (`KeyboardInteropService.OriginalValue` /
  `CancelAsync()`) — and closes. New **`DisableBackdropClick`** keeps the backdrop from dismissing and
  shows a **Cancel** button in the preview bar instead (so the keyboard stays open until the user
  confirms with ⏎ / the ⌄ Hide button, or cancels); rename it with **`CancelLabel`** (default
  `"Cancel"`). All opt-in.
- **Key click sound on every keyboard — still 100% JavaScript-free.** A new **`Sound`** parameter on
  `MudKeyboard`, `MudNumpad`, `MudPricepad` and `MudKeyboardHost` plays a short click on every key press,
  rendered as a Blazor `<audio>` element (re-mounted per press so the browser autoplays it) — no
  `IJSRuntime`, no JS file, so it works for the inline keyboards *and* the docked keyboard and keeps the
  JS-free-core rule intact. The default click is synthesised in pure C# (a windowed sine burst exposed as
  a `data:` URI — no shipped asset, AOT/trim-safe); point **`SoundSrc`** at any URL or `data:` URI to use
  your own sound. Off by default. Documented on the *MudKeyboard* and *Docked keyboard* pages (with live
  toggles), demonstrated in both demos, and covered by tests.

### Documentation
_Documentation-site (`src/MudKeyboard.Docs`) changes only — no change to the published library/package._
- **"What's new" badges across the docs.** Small **New** / **Updated** pills now flag per-release changes
  on the navigation links, section headings and parameter tables (so *Key click sound* is badged **New**
  and the *Docked keyboard* nav entry carries both **New** and **Updated**). A single `WhatsNew.Version`
  constant drives every badge's tooltip ("New in v1.2.0"), so advertising the next release is a one-line
  bump plus moving the markers.
- **Each docked-keyboard option now has its own runnable example.** The *Live value preview*,
  *Cancel & backdrop*, *Caps lock by default* and *Key click sound* options were split out of one shared
  control panel into self-contained example cards — each with its own toggle(s), a focusable demo field
  and *Show code* — so you can try an option and read the result in place instead of scrolling back up to
  a shared field at the top of the page. The *Toolbar buttons* example gained a focusable field too.
- **The API reference page is now full width with a live "On this page" highlight.** The `/api` page
  widens to the full container so its reference grid has more room (the *On this page* card stays on the
  right), and the current section's entry is highlighted in the primary colour as you scroll or click —
  a scroll-spy backed by an `IntersectionObserver` in the docs site's `docs.js`.
- **System / Light / Dark theme, with the choice remembered.** The docs site now defaults to matching the
  operating-system colour scheme (and follows it live via `MudThemeProvider`), exposes a
  System / Light / Dark switch in the app bar, and persists the visitor's choice in `localStorage` across
  visits — replacing the previous always-light default.

## [1.1.0] — 2026-06-29

### Added
- **Optional negative-number entry on the numeric keypads — a `±` sign-toggle key**
  ([#3](https://github.com/sardar97/mudkeyboard/issues/3)). The number, decimal and money keypads are
  positive-only by default; opt in to a `±` key that flips the sign. Inline:
  `MudNumpad`/`MudPricepad` gain an **`AllowNegative`** parameter (the pricepad formats negatives as
  `-£1.23`, and a zero amount is never shown as negative). Docked: `MudKeyboardNumericField` gains
  **`AllowNegative`** (per field, via a new `data-mudkeyboard-allow-negative` attribute), with a global
  default on **`MudKeyboardHost.AllowNegative`** for every numeric docked field; a field's attribute
  overrides the host default either way. Backed by a new `{sign}` key token (`KeyTokens.Sign`), three
  signed layout variants (`NumpadSigned`, `NumpadWithDecimalSigned`, `PriceSigned`), a
  `KeyboardInputKind.Sign` emit kind, and sign support in the pence-first money formatter — all
  AOT/trim-safe. Documented with runnable examples on the *Numpad*, *Pricepad* and *Docked keyboard*
  pages, demonstrated in both demos, and verified end to end in a real browser.
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

[1.3.0]: https://github.com/sardar97/mudkeyboard/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/sardar97/mudkeyboard/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/sardar97/mudkeyboard/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/sardar97/mudkeyboard/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/sardar97/mudkeyboard/compare/v0.1.0-alpha...v1.0.0
[0.1.0-alpha]: https://github.com/sardar97/mudkeyboard/releases/tag/v0.1.0-alpha
