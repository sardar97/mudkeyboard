# Changelog

All notable changes to **MudKeyboard** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/sardar97/mudkeyboard/compare/v0.1.0-alpha...HEAD
[0.1.0-alpha]: https://github.com/sardar97/mudkeyboard/releases/tag/v0.1.0-alpha
