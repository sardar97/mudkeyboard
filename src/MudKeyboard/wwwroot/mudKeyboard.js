// MudKeyboard — global focus capture shim.
//
// This is the ONLY JavaScript in the library. The keyboard itself (rendering, shift/caps,
// symbol toggle, text engine) is pure Blazor/C#. This module exists solely to do the one
// thing Blazor cannot do without JS: notice when *any* editable field is focused and edit it
// at the caret. It exposes a tiny API consumed by KeyboardInteropService over JS interop.
//
// Contract:
//   initialize(dotnetRef, attachMode, reportValue) — start listening; calls back .NET OnFocusIn /
//       OnFocusOut, and (when reportValue is set) OnValueChanged on every edit so the host can show a
//       live value-preview bar
//   insertText(text), backspace(), enter(), setValue(text), blurActive() — edit the active field
//   dispose() — stop listening
//
// No bundler, no dependencies — a plain ES module served from _content/MudKeyboard/mudKeyboard.js.

let dotnet = null;
let attachMode = 'AllInputs'; // 'AllInputs' (opt-out) | 'OptIn'
let activeEl = null;
let closing = 0;
// When true, the focused field's value is pushed back to .NET (OnValueChanged) on every change so the
// docked keyboard can show a live value-preview bar. Off unless MudKeyboardHost.ShowValuePreview is set.
let reportValue = false;

// Input types we treat as free-text editable. Pickers (date/color/checkbox/file/range…) are
// excluded — an on-screen text keyboard cannot meaningfully drive them.
const TEXT_TYPES = new Set(['text', 'search', 'email', 'url', 'tel', 'password', 'number', '']);

const DOCK_SELECTOR = '.mudkeyboard-dock';
const BACKDROP_SELECTOR = '.mudkeyboard-backdrop';

function isEditable(el) {
    if (!el || el.nodeType !== 1) return false;
    if (el.tagName === 'TEXTAREA') return true;
    if (el.tagName !== 'INPUT') return false;
    const type = (el.getAttribute('type') || 'text').toLowerCase();
    return TEXT_TYPES.has(type);
}

function shouldAttach(el) {
    if (!isEditable(el)) return false;
    if (el.disabled || el.readOnly) return false;
    if (el.hasAttribute('data-mudkeyboard-ignore')) return false;
    // OptIn: only fields explicitly marked. AllInputs: everything not opted out (handled above).
    if (attachMode === 'OptIn') return el.hasAttribute('data-mudkeyboard');
    return true;
}

// Resolve which keyboard face the field wants. An explicit data-mudkeyboard-layout wins; otherwise
// infer from the field's type / inputmode. Falls back to the full QWERTY keyboard.
function inferLayout(el) {
    const explicit = (el.getAttribute('data-mudkeyboard-layout') || '').toLowerCase();
    if (explicit) return explicit;

    const type = (el.getAttribute('type') || 'text').toLowerCase();
    const mode = (el.getAttribute('inputmode') || '').toLowerCase();

    if (mode === 'decimal') return 'decimal';
    if (type === 'number' || mode === 'numeric' || mode === 'tel') return 'numpad';
    return 'qwerty';
}

function insideDock(el) {
    return !!(el && el.closest && el.closest(DOCK_SELECTOR));
}

function insideBackdrop(el) {
    return !!(el && el.closest && el.closest(BACKDROP_SELECTOR));
}

// Push the focused field's current value to .NET so the value-preview bar can mirror it. One-way and
// display-only — it never writes back to the field, so it cannot race the field's own re-render.
function reportValueChanged(el) {
    if (!reportValue || !dotnet || !el) return;
    dotnet.invokeMethodAsync('OnValueChanged', el.value ?? '');
}

// Highest z-index currently used anywhere on the page, ignoring our own dock (which carries the
// value we set last time). Lets the keyboard sit one above whatever is on top right now — a dialog,
// a nested dialog, a custom overlay at any value — instead of guessing with a static number.
function highestZIndex() {
    let max = 0;
    const dock = document.querySelector(DOCK_SELECTOR);
    const all = document.body ? document.body.getElementsByTagName('*') : [];
    for (let i = 0; i < all.length; i++) {
        const el = all[i];
        if (el === dock) continue;
        const z = parseInt(window.getComputedStyle(el).zIndex, 10);
        if (!Number.isNaN(z) && z > max) max = z;
    }
    // Guard against overflow when added to on the .NET side.
    return Math.min(max, 2000000000);
}

function onFocusIn(e) {
    const el = e.target;
    if (insideDock(el)) return; // focus moving onto the keyboard itself — ignore
    if (!shouldAttach(el)) return;

    activeEl = el;
    // Pass the field's current value so the docked keyboard can seed pence-first money entry from it
    // (and so it never has to read the value back across a second interop round trip mid-keystroke), plus
    // the per-field data-mudkeyboard-allow-negative opt-in (empty when absent → the host default applies).
    dotnet.invokeMethodAsync('OnFocusIn', inferLayout(el), highestZIndex(), el.value ?? '',
        el.getAttribute('data-mudkeyboard-allow-negative') ?? '');

    // Lift the field above the docked keyboard so the user can see what they type.
    setTimeout(() => {
        try { el.scrollIntoView({ block: 'center', behavior: 'smooth' }); } catch { /* ignore */ }
    }, 60);
}

function onFocusOut(e) {
    // The field losing focus, and the field we were actively editing at the moment focus started to leave.
    const losing = e && e.target;
    const wasActive = activeEl;
    // Defer: focusout fires before the next focusin, and tapping a key can momentarily move focus.
    // Re-check the real focus target a beat later, then decide whether to close.
    closing += 1;
    const ticket = closing;
    setTimeout(() => {
        if (ticket !== closing) return; // superseded by a newer focus event
        const act = document.activeElement;
        if (insideDock(act)) return;     // focus is on the keyboard — keep open, still editing the field
        // Focus has genuinely left the field we were editing. Commit it (a single settled 'change' that
        // flushes non-immediate bindings) as a fallback for non-pointer blurs — e.g. Tab — since pointer
        // interactions are already committed pre-blur by onPointerDownCapture. Skip it when focus merely
        // bounced back to the same field (a transient blur during a key tap) — we're still editing it.
        if (losing && losing === wasActive && act !== wasActive) {
            commitField(losing);
        }
        if (shouldAttach(act)) return;   // moved to another field — its focusin handles the switch
        activeEl = null;
        if (dotnet) dotnet.invokeMethodAsync('OnFocusOut');
    }, 120);
}

// Commit the field being edited the instant the user presses somewhere outside it — crucially BEFORE the
// browser blurs the field. A hardware keyboard fires 'change' before 'blur', and MudBlazor's
// MudNumericField does its Min/Max clamping AND re-formats the displayed text inside its own blur handler,
// reading the field's Blazor-side text state. Our programmatic edits (native value setter) never update
// that state, so the field's native blur would otherwise see stale text and leave an out-of-range value on
// screen — e.g. 500 left visible in a Max=100 field even though the bound value clamped correctly. Firing
// 'change' here syncs the Blazor text state first, so the field's own blur then validates and fixes the
// display exactly as it does for a real keyboard. Key taps never reach this: the dock's mousedown
// preventDefault keeps the field focused, and we ignore presses inside the dock (and on the field itself).
function onPointerDownCapture(e) {
    const el = activeEl;
    if (!el) return;
    const target = e && e.target;
    // The backdrop is part of the keyboard UI: pressing it cancels (handled in Blazor), so skip the
    // commit here too — otherwise we would commit the edited value an instant before reverting it.
    if (target === el || insideDock(target) || insideBackdrop(target)) return;
    commitField(el);
}

// Mirror hardware-keyboard typing into the value-preview bar: when the user types into the focused
// field directly (not via the on-screen keys), report the new value too.
function onInputCapture(e) {
    if (reportValue && activeEl && e && e.target === activeEl) {
        reportValueChanged(activeEl);
    }
}

export function initialize(dotnetRef, mode, report) {
    dotnet = dotnetRef;
    if (mode) attachMode = mode;
    reportValue = !!report;
    document.addEventListener('focusin', onFocusIn, true);
    document.addEventListener('focusout', onFocusOut, true);
    document.addEventListener('pointerdown', onPointerDownCapture, true);
    document.addEventListener('input', onInputCapture, true);
}

// Turn live value reporting on/off after initialize (the host toggling ShowValuePreview at runtime).
export function setReportValue(value) {
    reportValue = !!value;
}

export function insertText(text) {
    const el = activeEl;
    if (!el || typeof text !== 'string' || text.length === 0) return;

    const value = el.value ?? '';
    const start = el.selectionStart ?? value.length;
    const end = el.selectionEnd ?? value.length;
    let next = value.slice(0, start) + text + value.slice(end);

    const max = el.maxLength;
    if (typeof max === 'number' && max >= 0 && next.length > max) {
        next = next.slice(0, max);
    }

    setNativeValue(el, next);
    const caret = Math.min(start + text.length, el.value.length);
    setCaret(el, caret);
    dispatchInput(el);
}

// Toggles a leading minus sign on the focused field's value (the ± key on the signed numeric keypads):
// "5" ↔ "-5", "" → "-". Used for the plain and decimal numeric keypads; the money keypad re-formats its
// own value with the sign on the .NET side instead.
export function toggleSign() {
    const el = activeEl;
    if (!el) return;
    const value = el.value ?? '';
    const next = value.startsWith('-') ? value.slice(1) : '-' + value;
    setNativeValue(el, next);
    setCaret(el, el.value.length);
    dispatchInput(el);
}

export function backspace() {
    const el = activeEl;
    if (!el) return;

    const value = el.value ?? '';
    const start = el.selectionStart ?? value.length;
    const end = el.selectionEnd ?? value.length;

    if (start !== end) {
        setNativeValue(el, value.slice(0, start) + value.slice(end));
        setCaret(el, start);
    } else if (start > 0) {
        setNativeValue(el, value.slice(0, start - 1) + value.slice(start));
        setCaret(el, start - 1);
    } else {
        return;
    }

    dispatchInput(el);
}

export function enter() {
    const el = activeEl;
    if (!el) return;

    // Emulate a real Enter so listeners/forms can react. The keyboard then closes (the host calls
    // blurActive after this), which commits the value for non-immediate bindings.
    el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', bubbles: true }));
    el.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', bubbles: true }));
}

// Empties the focused field.
export function clear() {
    const el = activeEl;
    if (!el) return;
    setNativeValue(el, '');
    setCaret(el, 0);
    dispatchInput(el);
}

// Copies the current selection (or the whole value when nothing is selected) to the clipboard.
export async function copy() {
    const el = activeEl;
    if (!el) return;
    const value = el.value ?? '';
    const start = el.selectionStart ?? 0;
    const end = el.selectionEnd ?? 0;
    const text = start !== end ? value.slice(start, end) : value;
    try { await navigator.clipboard.writeText(text); } catch { /* clipboard unavailable/blocked */ }
}

// Reads the clipboard and inserts it at the caret.
export async function paste() {
    const el = activeEl;
    if (!el) return;
    let text = '';
    try { text = await navigator.clipboard.readText(); } catch { return; }
    if (text) insertText(text);
}

// Replaces the focused field's whole value (used by pence-first money formatting) and dispatches input.
export function setValue(text) {
    const el = activeEl;
    if (!el) return;
    setNativeValue(el, text ?? '');
    setCaret(el, el.value.length);
    dispatchInput(el);
}

// Moves the caret left (delta < 0) or right (delta > 0); collapses a selection toward that side.
export function moveCaret(delta) {
    const el = activeEl;
    if (!el) return;
    const value = el.value ?? '';
    const start = el.selectionStart ?? value.length;
    const end = el.selectionEnd ?? value.length;
    const pos = start !== end
        ? (delta < 0 ? start : end)
        : Math.max(0, Math.min(value.length, start + delta));
    setCaret(el, pos);
}

export function blurActive() {
    const el = activeEl;
    activeEl = null;
    if (el) {
        // Commit + validate the value, like a hardware-keyboard blur, then drop focus. commitField fires a
        // single settled 'change' (for spinbuttons too — unlike the per-keystroke path, one change once
        // editing has stopped commits/clamps the value without snapping back).
        commitField(el);
        try { el.blur(); } catch { /* ignore */ }
    }
}

export function dispose() {
    document.removeEventListener('focusin', onFocusIn, true);
    document.removeEventListener('focusout', onFocusOut, true);
    document.removeEventListener('pointerdown', onPointerDownCapture, true);
    document.removeEventListener('input', onInputCapture, true);
    dotnet = null;
    activeEl = null;
}

function setCaret(el, pos) {
    try { el.setSelectionRange(pos, pos); } catch { /* number/email inputs disallow selection — ignore */ }
}

// Write the field's value through the *native* prototype value setter rather than `el.value = …`.
// Frameworks that track inputs by patching the value setter (React et al.) — and, crucially, Blazor's
// static-SSR/EditForm machinery and any plain HTML form — only observe the new value when it is set via
// the prototype descriptor. Without this, text typed by the on-screen keyboard would not be picked up
// by an SSR form POST. Textarea and input expose the setter on different prototypes.
function setNativeValue(el, value) {
    const proto = el.tagName === 'TEXTAREA'
        ? window.HTMLTextAreaElement.prototype
        : window.HTMLInputElement.prototype;
    const setter = Object.getOwnPropertyDescriptor(proto, 'value')?.set;
    if (setter) {
        setter.call(el, value);
    } else {
        el.value = value; // very old browsers without a descriptor setter — fall back to direct assignment
    }
}

// MudBlazor's numeric field renders role="spinbutton" on its <input>. It owns its formatted text and
// re-derives it from its parsed value, so when it receives a 'change' on top of the 'input' it discards
// a value that was set programmatically (the on-screen keyboard) and snaps back to the bound value.
// Detecting that one case lets us spare it the 'change' while leaving every other field untouched.
function isSpinButton(el) {
    return el.getAttribute('role') === 'spinbutton';
}

// Tell the page the value changed. For most fields we fire BOTH 'input' and 'change' after every
// keystroke so that MudBlazor immediate bindings (which listen on 'input'), non-immediate bindings and
// plain/SSR HTML forms (which commit on 'change'), and any non-Blazor listeners all stay in sync — this
// is what lets the docked keyboard drive static-SSR Blazor forms, ordinary <form> POSTs and bare inputs
// alike. The lone exception is the numeric spinbutton above: it accepts the value on 'input' alone and
// would otherwise revert. Its value still reaches an SSR POST (it is written to the DOM via the native
// setter) and immediate bindings update live on 'input'.
function dispatchInput(el) {
    el.dispatchEvent(new Event('input', { bubbles: true }));
    if (!isSpinButton(el)) {
        el.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

// Commit a field's value when the docked keyboard closes (focus leaves the field, the Hide/Enter buttons,
// or it switches to another field). Programmatic value writes (via the native setter) queue no native
// 'change', so without this a field with non-immediate binding never flushes its typed value and field
// validators never run when the keyboard closes — leaving the typed text visible but unbound (e.g. "100"
// stuck in a MudNumericField with Max=10). A single 'change' here mirrors a hardware-keyboard blur: it
// commits the value and runs validation/clamping. We DO fire it for spinbuttons (which dispatchInput
// spares per keystroke to avoid racing MudNumericField's re-render) because, once editing has settled, one
// 'change' commits and clamps cleanly without snapping back to the old value.
function commitField(el) {
    if (el && el.nodeType === 1 && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA')) {
        el.dispatchEvent(new Event('change', { bubbles: true }));
    }
}
