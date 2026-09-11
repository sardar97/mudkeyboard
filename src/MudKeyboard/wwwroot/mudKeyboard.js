// MudKeyboard — global focus capture shim.
//
// This is the ONLY JavaScript in the library. The keyboard itself (rendering, shift/caps,
// symbol toggle, text engine) is pure Blazor/C#. This module exists solely to do the one
// thing Blazor cannot do without JS: notice when *any* editable field is focused and edit it
// at the caret. It exposes a tiny API consumed by KeyboardInteropService over JS interop.
//
// Contract:
//   initialize(dotnetRef, attachMode, reportValue) — start listening; calls back .NET OnFocusIn /
//       OnFocusOut, and OnValueChanged whenever the focused field's value changes (always for changes
//       made outside the on-screen keys — hardware typing, a spin button, app code — and, when
//       reportValue is set, for the keyboard's own edits and caret moves too, so the host can show a
//       live value-preview bar with a cursor)
//   insertText(text), backspace(), enter(), setValue(text), blurActive() — edit the active field
//   dispose() — stop listening
//
// No bundler, no dependencies — a plain ES module served from _content/MudKeyboard/mudKeyboard.js.

let dotnet = null;
let attachMode = 'AllInputs'; // 'AllInputs' (opt-out) | 'OptIn'
let activeEl = null;
let closing = 0;
// When true, the keyboard's own edits and pure caret moves are pushed back to .NET (OnValueChanged) as
// well, so the docked keyboard can show a live value-preview bar with a cursor. Off unless
// MudKeyboardHost.ShowValuePreview is set. Changes made from outside the keyboard are always reported.
let reportValue = false;
// True while the shim itself is writing the focused field (insertText/backspace/setValue…), so the
// 'input' event it dispatches can be told apart from the user typing on a hardware keyboard.
let selfEditing = false;
// The field a numeric spin button (▲/▼) is about to focus — see onPointerDownCapture / onFocusIn.
let spinTarget = null;
let spinTimer = 0;
// The element whose `value` setter is currently intercepted — see hookValue.
let hookedEl = null;

// Input types we treat as free-text editable. Pickers (date/color/checkbox/file/range…) are
// excluded — an on-screen text keyboard cannot meaningfully drive them.
const TEXT_TYPES = new Set(['text', 'search', 'email', 'url', 'tel', 'password', 'number', '']);

const DOCK_SELECTOR = '.mudkeyboard-dock';
const BACKDROP_SELECTOR = '.mudkeyboard-backdrop';
// MudBlazor's numeric field wraps its ▲/▼ spin buttons in this element, inside the field's .mud-input.
const SPIN_SELECTOR = '.mud-input-numeric-spin';
const INPUT_WRAPPER_SELECTOR = '.mud-input';
// How long a spin-button press keeps the field it focuses from opening the keyboard (Blazor Server
// focuses the input after a round trip, so this needs some slack).
const SPIN_FOCUS_WINDOW_MS = 2000;
// How long, after the keyboard closes a numeric field, its displayed text is kept in step with the
// text the field settles on — see watchSpinButtonSettle.
const SETTLE_WINDOW_MS = 2000;

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

// The <input> a numeric field's spin button belongs to, or null when the press target is not a spin button.
function spinButtonInput(target) {
    if (!target || !target.closest) return null;
    const spin = target.closest(SPIN_SELECTOR);
    if (!spin || !spin.closest) return null;
    const wrapper = spin.closest(INPUT_WRAPPER_SELECTOR) || spin.parentElement;
    const input = wrapper && wrapper.querySelector ? wrapper.querySelector('input') : null;
    return input || null;
}

function setSpinTarget(input) {
    clearSpinTarget();
    if (!input) return;
    spinTarget = input;
    spinTimer = setTimeout(clearSpinTarget, SPIN_FOCUS_WINDOW_MS);
}

function clearSpinTarget() {
    spinTarget = null;
    if (spinTimer) {
        clearTimeout(spinTimer);
        spinTimer = 0;
    }
}

// The caret offset of a field, or the end of its value when selection is unavailable (number/email
// inputs disallow selectionStart). Used to position the preview bar's cursor.
function caretOf(el) {
    const len = (el.value ?? '').length;
    const pos = el.selectionStart;
    return typeof pos === 'number' ? pos : len;
}

// Push the focused field's current value AND caret position to .NET. One-way and display-only — it never
// writes back to the field, so it cannot race the field's own re-render.
//
// `external` marks a change the keyboard did not make itself: the user typing on a hardware keyboard, a
// MudNumericField spin button / ArrowUp / ArrowDown, or app code setting the bound value. Those are always
// reported, because the host needs them to keep its state honest (the "first digit replaces the value"
// rule and the pence-first money accumulator must follow what is really in the field), not just to draw
// the preview. The keyboard's own edits and pure caret moves are only reported while the value-preview
// bar is on (reportValue) — nothing else consumes them.
function reportValueChanged(el, external = false) {
    if (!dotnet || !el) return;
    if (!external && !reportValue) return;
    dotnet.invokeMethodAsync('OnValueChanged', el.value ?? '', caretOf(el), external);
}

// The native value accessor for an input/textarea, from its prototype.
function nativeValueDescriptor(el) {
    const proto = el.tagName === 'TEXTAREA'
        ? window.HTMLTextAreaElement.prototype
        : window.HTMLInputElement.prototype;
    return proto ? Object.getOwnPropertyDescriptor(proto, 'value') : undefined;
}

// Intercept programmatic writes to the focused field's value. Blazor updates an <input> by assigning
// element.value — a MudNumericField spin button or ArrowUp/ArrowDown, app code changing the bound value,
// a re-render after Min/Max clamping — which fires no 'input' event, so the docked keyboard never heard
// about it: the value-preview bar kept showing the old value and the "first digit replaces the value"
// rule stayed armed, so the next digit wiped what the spin button had just produced (GitHub #9).
// Shadowing `value` on the element *instance* with an accessor that forwards to the native prototype
// setter and then reports the new value catches every such write. The shim's own edits go straight to
// the prototype setter (setNativeValue) and never land here. Removed again by unhookValue when the
// field is no longer being edited, so nothing lingers on the element.
function hookValue(el) {
    unhookValue();
    if (!el || Object.prototype.hasOwnProperty.call(el, 'value')) return; // already shadowed by someone else — leave it
    const desc = nativeValueDescriptor(el);
    if (!desc || typeof desc.get !== 'function' || typeof desc.set !== 'function') return;
    try {
        Object.defineProperty(el, 'value', {
            configurable: true,
            enumerable: desc.enumerable,
            get() { return desc.get.call(this); },
            set(v) {
                desc.set.call(this, v);
                if (this === activeEl) reportValueChanged(this, true);
            },
        });
        hookedEl = el;
    } catch { /* non-extensible element — live without programmatic-change tracking */ }
}

function unhookValue() {
    if (!hookedEl) return;
    try { delete hookedEl.value; } catch { /* ignore */ }
    hookedEl = null;
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

// Start editing `el`: open the keyboard for it and watch it for programmatic value changes.
function attach(el) {
    activeEl = el;
    hookValue(el);
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

// Stop editing: forget the field and close the keyboard.
function detach() {
    activeEl = null;
    unhookValue();
    if (dotnet) dotnet.invokeMethodAsync('OnFocusOut');
}

function onFocusIn(e) {
    const el = e.target;
    if (insideDock(el)) return; // focus moving onto the keyboard itself — ignore
    if (!shouldAttach(el)) return;

    if (el === spinTarget) {
        // Focus handed to a numeric field by its own ▲/▼ spin button (MudBlazor focuses the input when a
        // spin button is pressed). Pressing a spin button is not a request to type, so it never opens the
        // keyboard; if the keyboard was open for a different field, that edit is over — close it. A later
        // tap on the (already focused) field opens the keyboard from onPointerDownCapture.
        clearSpinTarget();
        if (activeEl && activeEl !== el) detach();
        return;
    }

    // Focus returning to the field being edited (a blur/refocus bounce, e.g. around a spin button on
    // older MudBlazor builds) is not a fresh start: keep the value at focus-in, the "first digit
    // replaces" state and the preview exactly as they are.
    if (el === activeEl) return;

    attach(el);
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
        if (activeEl) detach();
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
//
// Two more jobs live here because pointerdown is the earliest moment a press can be seen:
//   - a press on a numeric field's ▲/▼ spin button is remembered (spinTarget) so the focus MudBlazor
//     then gives the input does not open the keyboard (see onFocusIn);
//   - a press on a field that is already focused but not being edited (typically after a spin button
//     focused it) opens the keyboard for it — the browser fires no focusin in that case.
function onPointerDownCapture(e) {
    const target = e && e.target;
    // The keyboard UI (keys, toolbar, backdrop) never commits: taps there are part of the edit, and a
    // backdrop press cancels (handled in Blazor) — committing first would commit the edit an instant
    // before reverting it.
    if (insideDock(target) || insideBackdrop(target)) return;

    const spinInput = spinButtonInput(target);
    setSpinTarget(spinInput);

    const el = activeEl;
    if (!el) {
        if (!spinInput && target === document.activeElement && shouldAttach(target)) attach(target);
        return;
    }
    if (target === el) return;
    commitField(el);
}

// Mirror typing that did not come from the on-screen keys (a hardware keyboard, paste, autofill) into
// the host — and, when the preview bar is on, the keyboard's own edits too.
function onInputCapture(e) {
    if (activeEl && e && e.target === activeEl) {
        reportValueChanged(activeEl, !selfEditing);
    }
}

// Snap the field's caret to the very end of its value (and keep the preview cursor in step).
function caretToEnd(el) {
    if (!el) return;
    setCaret(el, (el.value ?? '').length);
    reportValueChanged(el);
}

// The docked keyboard always types at the END of the field. A tap inside the field makes the browser
// drop the caret wherever the user pressed, so the next key would insert mid-text (e.g. typing into
// "sardar" tapped before "rdar" gives "sa…rdar"). Override that here: on pointerup — which fires AFTER
// the browser has positioned the caret from the tap — move it back to the end. This covers both the
// first focusing tap and any later re-tap inside the same already-focused field (where focusin never
// fires again), so the cursor is always at the end after the user clicks anywhere in the field.
function onPointerUpCapture(e) {
    const el = activeEl;
    if (!el) return;
    if (e && e.target === el) caretToEnd(el);
}

export function initialize(dotnetRef, mode, report) {
    dotnet = dotnetRef;
    if (mode) attachMode = mode;
    reportValue = !!report;
    document.addEventListener('focusin', onFocusIn, true);
    document.addEventListener('focusout', onFocusOut, true);
    document.addEventListener('pointerdown', onPointerDownCapture, true);
    document.addEventListener('pointerup', onPointerUpCapture, true);
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
    // No 'input' event fires for a pure caret move, so report it explicitly to keep the preview cursor synced.
    reportValueChanged(el);
}

export function blurActive() {
    const el = activeEl;
    activeEl = null;
    unhookValue();
    if (el) {
        // Commit + validate the value, like a hardware-keyboard blur, then drop focus. commitField fires a
        // single settled 'change' (for spinbuttons too — unlike the per-keystroke path, one change once
        // editing has stopped commits/clamps the value without snapping back). Then keep the text on
        // screen in step with whatever the numeric field settles on.
        watchSpinButtonSettle(el);
        commitField(el);
        try { el.blur(); } catch { /* ignore */ }
    }
}

export function dispose() {
    document.removeEventListener('focusin', onFocusIn, true);
    document.removeEventListener('focusout', onFocusOut, true);
    document.removeEventListener('pointerdown', onPointerDownCapture, true);
    document.removeEventListener('pointerup', onPointerUpCapture, true);
    document.removeEventListener('input', onInputCapture, true);
    clearSpinTarget();
    unhookValue();
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
// by an SSR form POST. Textarea and input expose the setter on different prototypes. It also bypasses
// the shim's own instance-level accessor (hookValue), so the keyboard's edits are never mistaken for
// programmatic changes.
function setNativeValue(el, value) {
    const setter = nativeValueDescriptor(el)?.set;
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
    selfEditing = true;
    try {
        el.dispatchEvent(new Event('input', { bubbles: true }));
        if (!isSpinButton(el)) {
            el.dispatchEvent(new Event('change', { bubbles: true }));
        }
    } finally {
        selfEditing = false;
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

// After the keyboard closes a MudBlazor numeric field (role="spinbutton"), make sure the text left on
// screen is the text the field settles on. The field re-derives its text from the committed value (Min/Max
// clamping, formatting) and Blazor only rewrites the DOM when that text differs from what it last
// rendered — but the keyboard typed into the DOM directly, behind Blazor's back, so a field whose text
// settles on the *same* string as before (typing 300 into a Max=30 field that already held 30) can be
// left showing the raw typed text (GitHub #8). MudNumericField mirrors its settled text into
// aria-valuetext (or, when the text is just the number, aria-valuenow), which Blazor does re-render, so
// watch those for a moment after closing and copy the settled text into the field whenever they disagree.
// Display-only: no events are fired, and nothing happens once the field is focused or being edited again.
function watchSpinButtonSettle(el) {
    if (!isSpinButton(el) || typeof MutationObserver === 'undefined') return;
    const sync = () => {
        if (el === activeEl || document.activeElement === el) return;
        const settled = el.getAttribute('aria-valuetext') ?? el.getAttribute('aria-valuenow');
        if (settled !== null && settled !== undefined && el.value !== settled) setNativeValue(el, settled);
    };
    const observer = new MutationObserver(sync);
    try {
        observer.observe(el, { attributes: true, attributeFilter: ['aria-valuenow', 'aria-valuetext'] });
    } catch { return; }
    setTimeout(() => { observer.disconnect(); sync(); }, SETTLE_WINDOW_MS);
}
