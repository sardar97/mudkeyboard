# Changelog

All notable changes to **MudKeyboard** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
