---
name: mudkeyboard
description: >-
  Add and configure MudKeyboard — a free, open-source on-screen virtual keyboard
  for Blazor + MudBlazor apps — in any Blazor project. Covers the inline keyboards
  (MudKeyboard, MudNumpad, MudPricepad), the global docked keyboard (MudKeyboardHost)
  that pops up on input focus, custom layouts and key tokens, emit mode, theming, and
  EVERY render mode: Interactive Server, Interactive WebAssembly, static SSR, with or
  without prerendering, and whether or not the app's own fields are MudBlazor inputs.
  Use this skill whenever the task involves an on-screen / virtual / touch keyboard or
  a numeric / numpad / pricepad / PIN / kiosk / point-of-sale text-entry surface in Blazor.
license: MIT
metadata:
  package: MudKeyboard
  version: "1.1.0"
  homepage: https://mudkeyboard.pages.dev
  nuget: https://www.nuget.org/packages/MudKeyboard
  repository: https://github.com/sardar97/mudkeyboard
  llms_txt: https://mudkeyboard.pages.dev/llms.txt
  llms_full_txt: https://mudkeyboard.pages.dev/llms-full.txt
---

# MudKeyboard — agent skill

MudKeyboard is a free, MIT-licensed **on-screen virtual keyboard** for [Blazor](https://learn.microsoft.com/aspnet/core/blazor)
apps that use [MudBlazor](https://mudblazor.com). It is built for touchscreens, kiosks, point-of-sale
terminals, and any UI where a hardware keyboard is unavailable.

This file is the authoritative how-to for AI coding agents. It is intentionally self-contained: a model
that has only this file can integrate MudKeyboard correctly without browsing the docs site (which is a
WebAssembly SPA and therefore not readable by a plain HTTP fetch). For prose docs, see the companion
[`llms-full.txt`](https://mudkeyboard.pages.dev/llms-full.txt).

> **Latest verified facts** (package **1.1.0**, .NET 8/9/10, MudBlazor 9.x). If the project's installed
> version differs, prefer the API as documented here unless the code says otherwise.

---

## 0. The two hard rules (read first)

1. **MudBlazor is a required peer dependency.** MudKeyboard renders its keys with MudBlazor components
   and reads MudBlazor CSS variables (`--mud-palette-*`) for every colour. The app **must** have the
   `MudBlazor` package, call `AddMudServices()`, and have a `<MudThemeProvider />` mounted in the layout.
   There is **no** "MudBlazor-free" mode. ("Using MudKeyboard without MudBlazor components" only ever
   means *binding to plain inputs/POCOs instead of MudBlazor input fields* — MudBlazor itself is still
   required. See [§9](#9-with-vs-without-mudblazor-input-components).)

2. **Two independent features, two setup paths:**
   - **Inline keyboards** (`MudKeyboard`, `MudNumpad`, `MudPricepad`) — render a keyboard *in the page*
     and two-way bind a string. **No `AddMudKeyboard()` needed.** They require an **interactive** render
     mode (Server or WebAssembly); they do **not** work on static-SSR pages.
   - **Global docked keyboard** (`MudKeyboardHost`) — one host that slides up from the bottom when **any**
     input is focused and types at that field's caret. **Requires `AddMudKeyboard()`** + one
     `<MudKeyboardHost />`. This is the one feature that uses JavaScript (a tiny focus-capture shim). It
     works under Server, WebAssembly, **and static SSR**.

---

## 1. Decision guide — pick the component

| You want… | Use | Needs `AddMudKeyboard()`? | Works on static-SSR page? |
| --- | --- | --- | --- |
| A QWERTY keyboard rendered in the page, bound to a string | `MudKeyboard` | No | No (needs interactivity) |
| A calculator numpad in the page | `MudNumpad` | No | No |
| Pence-first money entry in the page (`5·2·3 → £5.23`) | `MudPricepad` | No | No |
| Your own key layout in the page | `MudKeyboard` + `Layout="…"` `Variant="Custom"` | No | No |
| A keyboard that auto-pops for **every** focused input app-wide | `MudKeyboardHost` | **Yes** | **Yes** |
| Docked keyboard, opt-in per field | `MudKeyboardHost` (OptIn mode) + `MudKeyboardTextField`/`data-mudkeyboard` | **Yes** | **Yes** |
| Docked keypad that matches a numeric field's CLR type (incl. `decimal → money`) | `MudKeyboardNumericField<T>` | **Yes** | **Yes** |
| Route each keypress somewhere custom instead of editing `Value` | `MudKeyboard` + `OnInput` (emit mode) | No | No |

---

## 2. Install (always)

```bash
dotnet add package MudKeyboard
```

```xml
<!-- In the consuming project's .csproj -->
<PackageReference Include="MudBlazor"   Version="9.*" />   <!-- peer dependency -->
<PackageReference Include="MudKeyboard" Version="1.*" />
```

Add the namespaces to `_Imports.razor`:

```razor
@using MudKeyboard.Components
@using MudKeyboard.Models
@* Only if you use the docked keyboard's service options/extensions in a .razor file: *@
@* @using MudKeyboard.Extensions *@
@* @using MudKeyboard.Services *@
```

Ensure MudBlazor itself is set up (theme provider + services). If the app does not already have MudBlazor,
follow <https://mudblazor.com/getting-started/installation> first. Minimum layout:

```razor
<MudThemeProvider @bind-IsDarkMode="_isDarkMode" />   @* required — the keyboard reads this theme *@
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

```csharp
// Program.cs
builder.Services.AddMudServices();
```

> **Static web assets.** The docked keyboard ships two static assets —
> `_content/MudKeyboard/mudKeyboard.js` and `_content/MudKeyboard/MudKeyboard.css`. The host pulls the
> CSS in itself; the JS is imported on demand. The consuming app just needs to serve static web assets
> the normal way (`app.MapStaticAssets()` / `UseStaticFiles()` on a server; automatic for WASM standalone).
> Inline keyboards need no static assets at all.

---

## 3. Inline keyboards

All three are render-mode agnostic but **need an interactive render mode** on the hosting component
(`@rendermode InteractiveServer`, `@rendermode InteractiveWebAssembly`, or a globally-interactive app).

### MudKeyboard (full QWERTY)

```razor
<MudTextField @bind-Value="_text" Label="Name" Variant="Variant.Outlined" />
<MudKeyboard @bind-Value="_text" OnEnter="Submit" MaxLength="40" />

@code {
    private string _text = string.Empty;
    private void Submit() { /* Enter pressed */ }
}
```

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `Value` / `ValueChanged` | `string` | `""` | Two-way (`@bind-Value`). |
| `Layout` | `KeyboardLayout?` | `null` | Explicit layout; overrides `Variant`. |
| `Variant` | `KeyboardVariant` | `Full` | `Full`, `Numpad`, `Pricepad`, `Custom`. |
| `SymbolLayout` | `KeyboardLayout?` | `null` | Numbers/symbols face for the `{sym}` toggle. |
| `MaxLength` | `int?` | `null` | Caps the value length. |
| `Disabled` | `bool` | `false` | Disables every key. |
| `DropShadow` | `bool` | `true` | `false` = flat keys. |
| `Sound` | `bool` | `false` | Click sound on key press (pure-Blazor `<audio>`, no JS). |
| `SoundSrc` | `string?` | `null` | Custom click-sound source; defaults to the built-in click. |
| `Palette` | `KeyboardPalette?` | `null` | Per-keyboard colour overrides. |
| `AriaLabel` | `string?` | `"On-screen keyboard"` | Accessible name of the `role="group"`. |
| `Class` / `Style` | `string?` | `null` | Passthrough CSS. |
| `OnEnter` | `EventCallback` | — | Fires on the Enter key. |
| `OnEscape` | `EventCallback` | — | Fires on an `{esc}` key. |
| `OnInput` | `EventCallback<KeyboardInput>` | — | **Emit mode** (see [§6](#6-emit-mode-oninput)). |

### MudNumpad

```razor
<MudNumpad @bind-Value="_number" AllowDecimal="true" />
@code { private string _number = string.Empty; }
```

Parameters: `Value`/`ValueChanged`, `AllowDecimal` (bool), `AllowNegative` (bool, default `false` — adds a
`±` sign-toggle key for negative numbers), `MaxLength`, `Disabled`, `Sound` (bool) + `SoundSrc`
(`string?`, key-click sound, no JS), `OnEnter`, `Palette`, `AriaLabel` (default `"Numeric keypad"`),
`Class`, `Style`.

### MudPricepad (pence-first currency)

Typing `5`, `2`, `3` yields `£5.23` — the last `DecimalPlaces` digits are always the fraction.

```razor
<MudPricepad @bind-Value="_price" CurrencySymbol="£" DecimalPlaces="2" />
@code { private string _price = string.Empty; }
```

Parameters: `Value`/`ValueChanged`, `CurrencySymbol` (default `"£"`), `DecimalPlaces` (default `2`),
`AllowNegative` (bool, default `false` — adds a `±` key that flips the sign, e.g. `-£1.23`),
`MaxLength`, `Disabled`, `Sound` (bool) + `SoundSrc` (`string?`, key-click sound, no JS), `OnEnter`,
`Palette`, `AriaLabel` (default `"Price entry keypad"`), `Class`, `Style`.

---

## 4. Custom layouts & key tokens

A `KeyboardLayout` is **just data** — rows of key tokens. Literal tokens are typed verbatim; brace tokens
are commands.

```razor
<MudKeyboard @bind-Value="_value" Variant="KeyboardVariant.Custom" Layout="MyLayout" />

@code {
    private string _value = string.Empty;

    private static readonly KeyboardLayout MyLayout = new()
    {
        Rows = new string[][]
        {
            ["m", "u", "d"],
            ["k", "e", "y"],
            [KeyTokens.Space, KeyTokens.Backspace, KeyTokens.Enter],
        },
    };
}
```

`KeyboardLayout` shape:

```csharp
public sealed record KeyboardLayout
{
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}
```

### Key tokens (`KeyTokens`)

| Token | Constant | Action |
| --- | --- | --- |
| `{bksp}` | `KeyTokens.Backspace` | Delete char before the caret |
| `{enter}` | `KeyTokens.Enter` | Commit / submit (raises `OnEnter`) |
| `{space}` | `KeyTokens.Space` | Insert a space |
| `{shift}` | `KeyTokens.Shift` | One-shot shift (double-tap = caps lock) |
| `{caps}` | `KeyTokens.Caps` | Caps lock |
| `{sym}` | `KeyTokens.SymbolToggle` | Flip letters ↔ numbers/symbols (`123`/`ABC`) |
| `{esc}` | `KeyTokens.Escape` | Dismiss (raises `OnEscape`) |

### Built-in layouts (`LayoutLibrary`)

`Qwerty`, `Symbols`, `Numeric`, `Numpad`, `NumpadWithDecimal`, `Price`. Helpers:
`LayoutLibrary.ForVariant(variant)` and `LayoutLibrary.SymbolsForVariant(variant)` (only `Full` has a
symbol face). `KeyboardVariant.Custom` has **no** built-in layout — supplying one is required, and
`ForVariant(Custom)` throws.

---

## 5. Theming & palette

The keyboard inherits the **ambient MudBlazor theme** automatically — toggle `MudThemeProvider`'s dark
mode and every key follows, no extra code. To recolour a single keyboard, pass a `KeyboardPalette`; any
slot left unset still follows the theme (so dark/light keeps working).

```razor
<MudKeyboard @bind-Value="_value" Palette="Brand" />

@code {
    private static readonly KeyboardPalette Brand = new()
    {
        AccentColor = "#00897b",       // Enter / active-shift keys
        AccentTextColor = "#ffffff",
        // Surface, KeyColor, KeyTextColor unset → follow the theme
    };
}
```

| Slot | Recolours |
| --- | --- |
| `Surface` | Keyboard background |
| `KeyColor` | Literal-key background |
| `KeyTextColor` | Literal-key label |
| `AccentColor` | Accent-key (Enter / active shift) background |
| `AccentTextColor` | Accent-key label |

Values are any CSS colour — hex, `rgb()`/`hsl()`, or a `var(--…)` reference.

---

## 6. Emit mode (`OnInput`)

Supply `OnInput` to route each press somewhere other than the keyboard's own `Value` (this is how the
docked host edits the focused field). When `OnInput` is set, the keyboard does not mutate `Value` itself.

```razor
<MudKeyboard OnInput="Handle" />

@code {
    private void Handle(KeyboardInput input)
    {
        switch (input.Kind)
        {
            case KeyboardInputKind.Text:      /* insert input.Text */ break;
            case KeyboardInputKind.Backspace: /* delete one char  */ break;
            case KeyboardInputKind.Enter:     /* commit           */ break;
            case KeyboardInputKind.Escape:    /* dismiss          */ break;
        }
    }
}
```

`KeyboardInput` is a `readonly record struct (KeyboardInputKind Kind, string Text = "")` with factories
`KeyboardInput.Char(text)`, `.Backspace`, `.Enter`, `.Escape`.

---

## 7. Global docked keyboard (`MudKeyboardHost`)

A single host shows a keyboard that slides up when any input is focused and types at that field's caret.

**Step 1 — register services** (`Program.cs`):

```csharp
using MudKeyboard.Extensions;

builder.Services.AddMudServices();   // MudBlazor
builder.Services.AddMudKeyboard();   // MudKeyboard docked-keyboard services
```

**Step 2 — place the host once.** *Where* depends on the render mode — see [§8](#8-render-modes--the-canonical-matrix).
The simplest placement (fully-interactive app) is in the main layout next to the MudBlazor providers:

```razor
<MudThemeProvider @bind-IsDarkMode="_isDarkMode" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudKeyboardHost />   @* once, anywhere in the layout *@
```

That's it: every editable text/number field now raises the keyboard on focus. The layout is **inferred
per field** from the rendered HTML: text → QWERTY, integer fields → plain numpad, floating-point fields →
numpad with a decimal point. The docked panel also has clear / copy / paste / cursor controls and docks
one layer above the top-most element on the page (floats over dialogs and overlays).

`MudKeyboardHost` parameters: `Palette` (`KeyboardPalette?`), `Elevation` (`int`, default `8`),
`MinZIndex` (`int`, default `1400`), `Style` (`string?`), `VisibleActions` (`KeyboardAction`, default
`All`) and `DisabledActions` (`KeyboardAction`, default `None`). `KeyboardAction` is a `[Flags]` enum
(`Clear`, `Copy`, `Paste`, `CursorLeft`, `CursorRight`, `CursorControl`, `Hide`, `None`, `All`) that
lets you hide or disable any toolbar button — globally or one at a time. Hide Copy + Paste with
`VisibleActions="@(KeyboardAction.All & ~(KeyboardAction.Copy | KeyboardAction.Paste))"`, drop the whole
toolbar with `VisibleActions="KeyboardAction.None"`, or grey one out with
`DisabledActions="KeyboardAction.Clear"`. The cursor arrows never appear on the money keypad regardless.

`MudKeyboardHost` also has `AllowNegative` (`bool`, default `false`) — a global default for showing the
`±` sign-toggle key on the numeric keypads. Override it per field with
`data-mudkeyboard-allow-negative="true"`/`"false"`, which `MudKeyboardNumericField.AllowNegative` emits.

### Value preview, backdrop, cancel & sound (all opt-in, new in 1.2.0)

- `ShowValuePreview` (`bool`, default `false`) — show a bar at the top of the docked keyboard with the
  focused field's **live value**, so the user always sees what they're editing (useful when the field is
  hidden behind the panel). Editing is live; a focused field that already contains text shows it at once.
- `ShowBackdrop` (`bool`, default `false`) — dim the page behind the keyboard. A backdrop click
  **cancels** the edit, reverting the field to the value it had at focus-in, and closes.
- `DisableBackdropClick` (`bool`, default `false`) — stop the backdrop dismissing; instead show a
  **Cancel** button in the preview bar (so the keyboard stays open until ⏎ / the ⌄ Hide button confirms,
  or Cancel reverts). `CancelLabel` (`string`, default `"Cancel"`) renames it.
- `Sound` (`bool`, default `false`) + `SoundSrc` (`string?`) — play a click on every key press via a
  Blazor `<audio>` element (no JavaScript). `SoundSrc` overrides the built-in synthesised click with any
  URL or `data:` URI. The same two parameters exist on the inline `MudKeyboard` / `MudNumpad` /
  `MudPricepad`.

```razor
<MudKeyboardHost ShowValuePreview="true" ShowBackdrop="true" DisableBackdropClick="true" Sound="true" />
```

### Controlling which fields attach

Default mode is `KeyboardAttachMode.AllInputs` (every editable field attaches). Switch to opt-in:

```csharp
builder.Services.AddMudKeyboard(o => o.AttachMode = KeyboardAttachMode.OptIn);
```

Data attributes on any input (including a plain HTML `<input>`):

| Attribute | Effect |
| --- | --- |
| `data-mudkeyboard-ignore` | Never raise the keyboard for this field (in `AllInputs` mode). |
| `data-mudkeyboard` | Opt this field in (required in `OptIn` mode). |
| `data-mudkeyboard-layout="qwerty\|numpad\|decimal\|money"` | Force the layout (`price` is an alias of `money`). |

```razor
<MudTextField @bind-Value="_amount" data-mudkeyboard-layout="money" Immediate="true" />
<MudTextField @bind-Value="_note"   data-mudkeyboard-ignore="true"  Immediate="true" />
```

> **Always set `Immediate="true"`** on MudBlazor inputs used with the docked keyboard so the bound value
> updates as keys are tapped (otherwise MudBlazor only commits on blur).

### Convenience wrappers

**`MudKeyboardTextField<T>`** — a thin wrapper over `MudTextField<T>` that adds `data-mudkeyboard`
(and `data-mudkeyboard-layout` when set) for you. Useful in `OptIn` mode and on static-SSR pages.

```razor
<MudKeyboardTextField @bind-Value="Input.Username" Label="Username"
                      DockedKeyboard="true" name="Input.Username" />
```

**`MudKeyboardNumericField<T>`** — a wrapper over `MudNumericField<T>` that picks the docked keypad from
the **bound CLR type**. This solves a problem JavaScript can't: a `decimal` and a `double` both render
`inputmode="decimal"`, so only the type distinguishes currency from a plain decimal.

| Bound `T` | Docked keypad |
| --- | --- |
| `decimal` | **Money** — pence-first (`5·2·3 → 5.23`), like `MudPricepad` |
| `double` / `float` | Numeric keypad **with** a `.` key |
| `int`, `long`, `short`, … | Numeric keypad **without** a `.` key |

```razor
<MudKeyboardNumericField @bind-Value="Model.Price" T="decimal" Format="N2"
                         Adornment="Adornment.Start"
                         AdornmentIcon="@Icons.Material.Filled.CurrencyPound" />

<MudKeyboardNumericField @bind-Value="_measure"  T="double" />  @* keypad with "."   *@
<MudKeyboardNumericField @bind-Value="_quantity" T="int" />     @* keypad, no "."     *@
```

Both wrappers expose `DockedKeyboard` (`bool`, default `false` — adds `data-mudkeyboard="true"`, required
in `OptIn` mode) and `DockedKeyboardLayout` (`string`, default `""` — forces the layout; empty = infer/
resolve from `T`). They forward the common `MudTextField`/`MudNumericField` parameters plus extra
attributes such as `name`.

---

## 8. Render modes — the canonical matrix

This is the part agents most often get wrong. Behaviour by render mode:

| Render mode | Inline keyboards | Docked keyboard | Where to place `<MudKeyboardHost>` |
| --- | --- | --- | --- |
| **Interactive Server** | ✅ works | ✅ works | MainLayout (global render mode) **or** App.razor outside `<Routes>` |
| **Interactive WebAssembly** | ✅ works | ✅ works | MainLayout (WASM app) |
| **Interactive Auto** | ✅ works | ✅ works | App.razor outside `<Routes>` with `@rendermode="InteractiveAuto"` |
| **Static SSR** (no circuit) | ❌ inline needs interactivity | ✅ works | App.razor outside `<Routes>`, with its own interactive render mode |
| **Prerendering** (any interactive mode) | ✅ safe | ✅ safe | unchanged — see below |

### Prerendering / no-prerendering

**Safe either way; no special handling needed.** The docked host only touches JavaScript in
`OnAfterRenderAsync(firstRender: true)`, which never runs during a server prerender pass. It also wraps
the interop call in a try/catch for `JSException`/`InvalidOperationException`, so a stray prerender never
throws. Inline keyboards prerender as static HTML, then wire up their event handlers when they become
interactive. You do **not** need `prerender: false`. (If you do disable prerendering, everything still
works.)

### Interactive Server (Blazor Web App, global interactive render mode)

If `<Routes @rendermode="InteractiveServer" />` is global, put the host in `MainLayout.razor`:

```razor
@* MainLayout.razor *@
<MudThemeProvider /> <MudPopoverProvider /> <MudDialogProvider /> <MudSnackbarProvider />
<MudKeyboardHost />
@Body
```

### Interactive WebAssembly (standalone WASM app)

Register `AddMudKeyboard()` in the WASM `Program.cs` and place `<MudKeyboardHost />` once in
`MainLayout.razor`. No `<script>` tag is needed — the focus-capture module is imported on demand from
`_content/MudKeyboard/mudKeyboard.js`.

```csharp
// Program.cs (WASM standalone)
builder.Services.AddMudServices();
builder.Services.AddMudKeyboard();
```

### Static SSR — and mixing render modes per page

A static-SSR page has no Blazor circuit, so **inline keyboards won't work there**, but the **docked
keyboard will**: it edits the focused `<input>` via JavaScript on `document.activeElement`, dispatching
native `input`/`change` events so the value rides an ordinary form POST. Give the host its **own
interactive island** by placing it in `App.razor`, **outside `<Routes>`**, with an explicit render mode:

```razor
@* App.razor — host has its own circuit; pages without @rendermode stay static SSR *@
<body>
    <Routes />

    <MudKeyboardHost @rendermode="InteractiveServer" />   @* outside <Routes> *@

    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>
```

> Placing the host inside a layout that a static-SSR page uses would make the host static too (its JS
> never runs). Keep it outside `<Routes>`.

A static-SSR page then opts fields in (page needs **no** `@rendermode`):

```razor
@page "/login"
@* No @rendermode → static SSR *@

<EditForm Model="Input" method="post" OnValidSubmit="SignIn" FormName="login">
    <MudKeyboardTextField @bind-Value="Input.Username" Label="Username"
                          DockedKeyboard="true" name="Input.Username" />
    <MudKeyboardTextField @bind-Value="Input.Password" Label="Password"
                          InputType="InputType.Password"
                          DockedKeyboard="true" DockedKeyboardLayout="qwerty"
                          name="Input.Password" />
    <MudButton ButtonType="ButtonType.Submit">Sign in</MudButton>
</EditForm>
```

A plain `MudTextField` with `data-mudkeyboard="true"` works the same way (handy in `AllInputs` mode).

### Mobile viewport (recommended)

Add `viewport-fit=cover` so the docked panel pads past device safe-area insets:

```html
<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
```

---

## 9. With vs without MudBlazor input components

MudBlazor is **always required** (package + `AddMudServices()` + `<MudThemeProvider />`). What's optional
is whether the *fields you bind* are MudBlazor inputs.

**With MudBlazor inputs** (typical):

```razor
<MudTextField @bind-Value="_text" Immediate="true" />     @* docked keyboard attaches automatically *@
<MudKeyboard @bind-Value="_text" />                        @* or an inline keyboard *@
```

**Without MudBlazor inputs** — bind the inline keyboard to a plain string and display it however you like,
or attach the docked keyboard to a plain HTML `<input>`:

```razor
@* Inline keyboard driving a POCO string shown in a native element. *@
<input value="@_text" readonly />
<MudKeyboard @bind-Value="_text" />

@* Docked keyboard on a native input (AllInputs mode attaches automatically; in OptIn mode add the attr). *@
<input type="text" data-mudkeyboard="true" />

@code { private string _text = string.Empty; }
```

Even here you still need `<MudThemeProvider />` somewhere above, because the *keyboard surface itself* is
rendered with MudBlazor and themed by its CSS variables. If colours look broken/unstyled, the most common
cause is a missing `MudThemeProvider`.

---

## 10. Accessibility (built in, nothing to wire up)

Each keyboard surface is a labelled `role="group"`; every key is a real `<button>` with a spoken
`aria-label` (`⌫` → "Backspace", blank space bar → "Space", `123`/`ABC` → "Numbers and symbols"/"Letters").
Shift/caps and the symbol toggle expose `aria-pressed`. While hidden, `MudKeyboardHost` is `inert` and
`aria-hidden` (out of the tab order). Set the `AriaLabel` parameter to name each inline keyboard distinctly.
`KeyboardKey.AccessibleLabel` exposes a key's spoken name. Reduced-motion, safe-area and comfortable touch
targets are handled automatically.

---

## 11. Common mistakes (and the fix)

- **Inline keyboard does nothing on a static-SSR page.** Inline keyboards need an interactive render mode.
  Either add `@rendermode InteractiveServer`/`InteractiveWebAssembly`, or use the docked keyboard (which
  works on static SSR).
- **Docked keyboard never appears.** (a) Forgot `AddMudKeyboard()`; (b) host placed inside a static-SSR
  layout instead of its own interactive island in `App.razor` outside `<Routes>`; (c) in `OptIn` mode but
  the field has no `data-mudkeyboard` / isn't a `MudKeyboard*Field` with `DockedKeyboard="true"`;
  (d) static web assets aren't being served.
- **Typed text doesn't update my bound value.** Add `Immediate="true"` to the MudBlazor input.
- **A `decimal` money field shows the plain decimal keypad.** `decimal` vs `double` are indistinguishable
  to the JS shim. Use `MudKeyboardNumericField<decimal>` or `data-mudkeyboard-layout="money"`.
- **More than one `<MudKeyboardHost>`.** Place it exactly **once**.
- **Colours look wrong/unstyled.** No `<MudThemeProvider />` in the tree (MudBlazor is mandatory).
- **`LayoutLibrary.ForVariant(KeyboardVariant.Custom)` throws.** Custom has no built-in layout — pass your
  own `Layout`.
- **Interactive Auto / WASM-rendered host.** Call `AddMudKeyboard()` in **both** the server and the WASM
  client `Program.cs`.

---

## 12. Copy-paste recipes

**A. Inline QWERTY bound to a MudTextField (Interactive Server or WASM):**

```razor
@rendermode InteractiveServer
<MudTextField @bind-Value="_v" Label="Name" Immediate="true" />
<MudKeyboard @bind-Value="_v" OnEnter="@(() => { /* submit */ })" />
@code { private string _v = ""; }
```

**B. Global docked keyboard, every field, fully-interactive app:**

```csharp
// Program.cs
builder.Services.AddMudServices();
builder.Services.AddMudKeyboard();
```
```razor
@* MainLayout.razor *@
<MudThemeProvider /> <MudPopoverProvider /> <MudDialogProvider /> <MudSnackbarProvider />
<MudKeyboardHost />
@Body
```

**C. Docked keyboard, opt-in only, on a static-SSR login page:** see [§8 Static SSR](#static-ssr--and-mixing-render-modes-per-page).

**D. Pence-first money entry inline:**

```razor
<MudPricepad @bind-Value="_price" CurrencySymbol="£" DecimalPlaces="2" />
@code { private string _price = ""; }
```

---

## 13. Full API surface (quick reference)

- **Components** (`MudKeyboard.Components`): `MudKeyboard`, `MudNumpad`, `MudPricepad`, `MudKeyboardHost`,
  `MudKeyboardTextField<T>`, `MudKeyboardNumericField<T>`.
- **Models** (`MudKeyboard.Models`): `KeyboardLayout`, `KeyboardVariant` (`Full`/`Numpad`/`Pricepad`/`Custom`),
  `KeyboardPalette`, `KeyTokens`, `KeyboardInput` + `KeyboardInputKind` (`Text`/`Backspace`/`Enter`/`Escape`),
  `KeyboardKey`.
- **Layouts** (`MudKeyboard.Layouts`): `LayoutLibrary` (`Qwerty`, `Symbols`, `Numeric`, `Numpad`,
  `NumpadWithDecimal`, `Price`, `ForVariant`, `SymbolsForVariant`).
- **Services / DI** (`MudKeyboard.Services`, `MudKeyboard.Extensions`): `AddMudKeyboard(Action<MudKeyboardOptions>?)`,
  `MudKeyboardOptions { AttachMode }`, `KeyboardAttachMode` (`AllInputs`/`OptIn`), `KeyboardInteropService`
  (internal interop; the only place that uses JS).

---

## 14. Links

- Live docs & playground: <https://mudkeyboard.pages.dev> (note: WebAssembly SPA — not readable by a raw fetch)
- **AI-readable full docs:** <https://mudkeyboard.pages.dev/llms-full.txt>
- AI index: <https://mudkeyboard.pages.dev/llms.txt>
- This skill (always-latest): <https://mudkeyboard.pages.dev/skill.md>
- NuGet: <https://www.nuget.org/packages/MudKeyboard>
- Source (MIT): <https://github.com/sardar97/mudkeyboard>
- MudBlazor (peer dependency): <https://mudblazor.com>
