# Changelog

All notable changes to **MudKeyboard** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
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
- **Focus-capture shim now writes through the native input setter and dispatches `change`.** Every
  keystroke from the docked keyboard sets the field's value via the native
  `HTMLInputElement`/`HTMLTextAreaElement` `value` setter and dispatches both `input` and `change`
  events. This makes the typed value flow correctly into static-SSR Blazor forms, plain HTML form
  POSTs and non-Blazor inputs, in addition to MudBlazor immediate and non-immediate bindings.
- **The docked keyboard's toolbar tooltips now use the native `title` attribute** instead of
  `MudTooltip`. `MudTooltip` requires a `MudPopoverProvider` in scope; dropping it makes
  `MudKeyboardHost` fully self-contained, so it can be placed in its own interactive island (in
  `App.razor`, outside `<Routes>`) — as the static-SSR setup requires — without a provider as an
  ancestor and without rendering duplicate popovers when it sits in a normal layout. Every toolbar
  button keeps its `aria-label`.

### Fixed
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

[Unreleased]: https://github.com/sardar97/mudkeyboard/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/sardar97/mudkeyboard/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/sardar97/mudkeyboard/compare/v0.1.0-alpha...v1.0.0
[0.1.0-alpha]: https://github.com/sardar97/mudkeyboard/releases/tag/v0.1.0-alpha
