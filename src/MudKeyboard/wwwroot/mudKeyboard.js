// MudKeyboard — global focus capture shim.
//
// This is the ONLY JavaScript in the library. The keyboard itself (rendering, shift/caps,
// symbol toggle, text engine) is pure Blazor/C#. This module exists solely to do the one
// thing Blazor cannot do without JS: notice when *any* editable field is focused and edit it
// at the caret. It exposes a tiny API consumed by KeyboardInteropService over JS interop.
//
// Contract:
//   initialize(dotnetRef, attachMode) — start listening; calls back .NET OnFocusIn / OnFocusOut
//   insertText(text), backspace(), enter(), blurActive() — edit the active field
//   dispose() — stop listening
//
// No bundler, no dependencies — a plain ES module served from _content/MudKeyboard/mudKeyboard.js.

let dotnet = null;
let attachMode = 'AllInputs'; // 'AllInputs' (opt-out) | 'OptIn'
let activeEl = null;
let closing = 0;

// Input types we treat as free-text editable. Pickers (date/color/checkbox/file/range…) are
// excluded — an on-screen text keyboard cannot meaningfully drive them.
const TEXT_TYPES = new Set(['text', 'search', 'email', 'url', 'tel', 'password', 'number', '']);

const DOCK_SELECTOR = '.mudkeyboard-dock';

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
    dotnet.invokeMethodAsync('OnFocusIn', inferLayout(el), highestZIndex());

    // Lift the field above the docked keyboard so the user can see what they type.
    setTimeout(() => {
        try { el.scrollIntoView({ block: 'center', behavior: 'smooth' }); } catch { /* ignore */ }
    }, 60);
}

function onFocusOut() {
    // Defer: focusout fires before the next focusin, and tapping a key can momentarily move focus.
    // Re-check the real focus target a beat later, then decide whether to close.
    closing += 1;
    const ticket = closing;
    setTimeout(() => {
        if (ticket !== closing) return; // superseded by a newer focus event
        const act = document.activeElement;
        if (insideDock(act)) return;     // focus is on the keyboard — keep open
        if (shouldAttach(act)) return;   // moved to another field — its focusin handles the switch
        activeEl = null;
        if (dotnet) dotnet.invokeMethodAsync('OnFocusOut');
    }, 120);
}

export function initialize(dotnetRef, mode) {
    dotnet = dotnetRef;
    if (mode) attachMode = mode;
    document.addEventListener('focusin', onFocusIn, true);
    document.addEventListener('focusout', onFocusOut, true);
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

    el.value = next;
    const caret = Math.min(start + text.length, el.value.length);
    setCaret(el, caret);
    dispatchInput(el);
}

export function backspace() {
    const el = activeEl;
    if (!el) return;

    const value = el.value ?? '';
    const start = el.selectionStart ?? value.length;
    const end = el.selectionEnd ?? value.length;

    if (start !== end) {
        el.value = value.slice(0, start) + value.slice(end);
        setCaret(el, start);
    } else if (start > 0) {
        el.value = value.slice(0, start - 1) + value.slice(start);
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
    el.value = '';
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

// Returns the focused field's current value (used by pence-first money formatting).
export function getValue() {
    return activeEl ? (activeEl.value ?? '') : '';
}

// Replaces the focused field's whole value (used by pence-first money formatting) and dispatches input.
export function setValue(text) {
    const el = activeEl;
    if (!el) return;
    el.value = text ?? '';
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
        el.dispatchEvent(new Event('change', { bubbles: true }));
        try { el.blur(); } catch { /* ignore */ }
    }
}

export function dispose() {
    document.removeEventListener('focusin', onFocusIn, true);
    document.removeEventListener('focusout', onFocusOut, true);
    dotnet = null;
    activeEl = null;
}

function setCaret(el, pos) {
    try { el.setSelectionRange(pos, pos); } catch { /* number/email inputs disallow selection — ignore */ }
}

// Tell Blazor the value changed. Immediate fields bind on 'input'; this also keeps MudBlazor's
// own input in sync. 'change' is dispatched on enter/blur for non-immediate bindings.
function dispatchInput(el) {
    el.dispatchEvent(new Event('input', { bubbles: true }));
}
