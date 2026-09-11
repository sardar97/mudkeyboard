// Unit tests for the focus-capture shim (src/MudKeyboard/wwwroot/mudKeyboard.js).
//
// The module is JavaScript, so it sits outside the .NET test project. Run it with Node's built-in
// test runner (Node 18+):
//
//     cd tests/js && npm test          # or: node --test
//     node --test tests/js/mudKeyboard.test.mjs   # from the repo root
//
// These tests pin the behaviour that makes the docked keyboard work on static-SSR pages: every edit
// must write the value through the *native* input/textarea value setter and dispatch BOTH an 'input'
// and a 'change' event, so plain/SSR form POSTs and Blazor bindings alike pick the value up.

import test from 'node:test';
import assert from 'node:assert/strict';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';

// ---- Minimal DOM stubs (installed before importing the module's functions are used) -------------

const dirname = path.dirname(fileURLToPath(import.meta.url));
const modulePath = path.resolve(dirname, '../../src/MudKeyboard/wwwroot/mudKeyboard.js');

function defineNativeValue(prototype) {
  Object.defineProperty(prototype, 'value', {
    get() { return this._value ?? ''; },
    set(v) { this._value = v; },
    configurable: true,
  });
}

globalThis.window = {
  HTMLInputElement: { prototype: {} },
  HTMLTextAreaElement: { prototype: {} },
  getComputedStyle: () => ({ zIndex: 'auto' }),
};
defineNativeValue(globalThis.window.HTMLInputElement.prototype);
defineNativeValue(globalThis.window.HTMLTextAreaElement.prototype);

globalThis.Event = class { constructor(type) { this.type = type; } };

let focusInHandler = null;
let focusOutHandler = null;
let pointerDownHandler = null;
let pointerUpHandler = null;
let inputHandler = null;
globalThis.document = {
  addEventListener: (type, handler) => {
    if (type === 'focusin') focusInHandler = handler;
    if (type === 'focusout') focusOutHandler = handler;
    if (type === 'pointerdown') pointerDownHandler = handler;
    if (type === 'pointerup') pointerUpHandler = handler;
    if (type === 'input') inputHandler = handler;
  },
  removeEventListener: () => {},
  body: { getElementsByTagName: () => [] },
  querySelector: () => null,
  activeElement: null,
};
// Deferred work (the scroll-into-view timer and the focusout close check) is queued rather than run
// inline, so a test can interleave a focusin between a focusout and its deferred check — exactly the
// ordering a real browser produces — then flush with runTimers().
let pendingTimers = [];
globalThis.setTimeout = (fn) => { if (typeof fn === 'function') pendingTimers.push(fn); return pendingTimers.length; };
globalThis.clearTimeout = () => {};
function runTimers() { const due = pendingTimers; pendingTimers = []; for (const fn of due) fn(); }

// A MutationObserver stand-in: records the observers the shim creates so a test can fire an attribute
// change at it (Blazor re-rendering aria-valuenow / aria-valuetext) with observer.trigger().
const observers = [];
globalThis.MutationObserver = class {
  constructor(callback) { this.callback = callback; this.target = null; this.disconnected = false; observers.push(this); }
  observe(target) { this.target = target; }
  disconnect() { this.disconnected = true; }
  trigger() { this.callback([], this); }
};

function makeField(tagName, initial = '') {
  const prototype = tagName === 'TEXTAREA'
    ? globalThis.window.HTMLTextAreaElement.prototype
    : globalThis.window.HTMLInputElement.prototype;
  const el = Object.create(prototype);
  el.tagName = tagName;
  el._value = initial;
  el.selectionStart = initial.length;
  el.selectionEnd = initial.length;
  el.maxLength = -1;
  el.nodeType = 1;
  el.disabled = false;
  el.readOnly = false;
  el.events = [];
  el.getAttribute = (name) => (name === 'type' ? 'text' : null);
  el.hasAttribute = () => false;
  el.dispatchEvent = (e) => { el.events.push(e.type); return true; };
  el.setSelectionRange = (s, e) => { el.selectionStart = s; el.selectionEnd = e; };
  el.scrollIntoView = () => {};
  el.closest = () => null;
  el.blur = () => {};
  return el;
}

// The shim is a browser ES module served with a .js extension; copy it to a .mjs temp file so Node
// parses its `export`s as ESM regardless of any package.json "type" in the tree, then import that.
const tmpModule = path.join(mkdtempSync(path.join(os.tmpdir(), 'mudkbd-')), 'mudKeyboard.mjs');
writeFileSync(tmpModule, readFileSync(modulePath, 'utf8'));
const mod = await import(pathToFileURL(tmpModule).href);

// .NET methods the shim invoked since the last focus() call (e.g. 'OnFocusIn', 'OnFocusOut').
let invoked = [];

// Focus an element through the real focusin path so the module's internal activeEl is set.
function focus(el) {
  mod.initialize({ invokeMethodAsync: (m) => { invoked.push(m); return Promise.resolve(); } }, 'AllInputs');
  focusInHandler({ target: el });
  runTimers();    // run the scroll-into-view timer (a no-op stub) so it can't leak into a later test
  el.events = []; // ignore anything emitted during focus; tests assert on edits only
  invoked = [];
}

// A field that reports role="spinbutton" — i.e. a MudBlazor MudNumericField. The per-keystroke 'change'
// is suppressed for these, so the typed value commits only when the keyboard closes.
function makeSpinButton(initial = '') {
  const el = makeField('INPUT', initial);
  el.getAttribute = (name) => (name === 'role' ? 'spinbutton' : name === 'type' ? 'text' : null);
  return el;
}

// An element that is not an attachable field; focus landing here closes the keyboard.
function elsewhere() {
  return { nodeType: 1, tagName: 'DIV', closest: () => null, getAttribute: () => null, hasAttribute: () => false, disabled: false, readOnly: false };
}

// A press target inside the docked keyboard (e.g. a key) — closest('.mudkeyboard-dock') resolves.
function dockKey() {
  return { closest: () => ({}) };
}

// ---- Tests --------------------------------------------------------------------------------------

test('insertText writes via the native setter and dispatches input then change', () => {
  const el = makeField('INPUT', '');
  focus(el);

  mod.insertText('a');

  assert.equal(el.value, 'a');
  assert.deepEqual(el.events, ['input', 'change']);
});

test('insertText inserts at the caret, not just at the end', () => {
  const el = makeField('INPUT', 'ac');
  el.selectionStart = 1;
  el.selectionEnd = 1;
  focus(el);

  mod.insertText('b');

  assert.equal(el.value, 'abc');
});

test('insertText honours maxLength', () => {
  const el = makeField('INPUT', 'ab');
  el.maxLength = 3;
  focus(el);

  mod.insertText('cd');

  assert.equal(el.value, 'abc');
});

test('backspace removes the char before the caret and dispatches input then change', () => {
  const el = makeField('INPUT', 'ab');
  focus(el);

  mod.backspace();

  assert.equal(el.value, 'a');
  assert.deepEqual(el.events, ['input', 'change']);
});

test('clear empties the field and notifies', () => {
  const el = makeField('INPUT', 'hello');
  focus(el);

  mod.clear();

  assert.equal(el.value, '');
  assert.deepEqual(el.events, ['input', 'change']);
});

test('setValue replaces the whole value (used by pence-first money entry)', () => {
  const el = makeField('INPUT', '1');
  focus(el);

  mod.setValue('5.23');

  assert.equal(el.value, '5.23');
  assert.deepEqual(el.events, ['input', 'change']);
});

test('insertText works on a <textarea> via the textarea native setter', () => {
  const el = makeField('TEXTAREA', '');
  focus(el);

  mod.insertText('hi');

  assert.equal(el.value, 'hi');
  assert.deepEqual(el.events, ['input', 'change']);
});

// ---- Commit on close (GitHub #4) ----------------------------------------------------------------
// Programmatic value writes queue no native 'change', so the keyboard must synthesise one when it
// closes — otherwise a non-immediate field (and any Min/Max validation) never sees the typed value.

test('blurActive commits the field with a single change — including spinbuttons', () => {
  const el = makeSpinButton('100');
  focus(el);

  mod.blurActive();

  // The per-keystroke change is suppressed for a spinbutton; closing commits + validates it now.
  assert.deepEqual(el.events, ['change']);
});

test('blurActive commits an ordinary field on close too', () => {
  const el = makeField('INPUT', 'abc');
  focus(el);

  mod.blurActive();

  assert.deepEqual(el.events, ['change']);
});

test('focusout commits the field, then closes, when focus leaves it entirely', () => {
  const el = makeSpinButton('100');
  focus(el);

  focusOutHandler({ target: el });
  document.activeElement = elsewhere();
  runTimers();

  assert.deepEqual(el.events, ['change']);    // value committed + validated
  assert.ok(invoked.includes('OnFocusOut'));  // keyboard told to close
});

test('focusout commits the field being left when focus moves to another field', () => {
  const a = makeSpinButton('100');
  const b = makeField('INPUT', '');
  focus(a);

  focusOutHandler({ target: a }); // a starts losing focus (deferred check queued)
  focusInHandler({ target: b });  // focus lands on b first, as in a real browser
  document.activeElement = b;
  runTimers();                    // now the deferred check runs

  assert.deepEqual(a.events, ['change']);      // the field we left is committed
  assert.ok(!invoked.includes('OnFocusOut'));  // keyboard stays open for b
});

test('focusout does NOT commit when focus merely bounces back to the same field', () => {
  const el = makeSpinButton('5');
  focus(el);

  focusOutHandler({ target: el });
  document.activeElement = el; // a transient blur during a key tap — focus returns to the field
  runTimers();

  assert.deepEqual(el.events, []); // still editing; nothing committed prematurely
});

// Pressing outside the field must commit it BEFORE the browser blurs it — mirroring a hardware
// keyboard's change→blur order — so MudNumericField's own blur handler validates/clamps and re-formats
// the displayed text (otherwise an out-of-range value clamped back to the existing value stays on screen).

test('pointerdown outside the field commits it (pre-blur, like a hardware keyboard)', () => {
  const el = makeSpinButton('100');
  focus(el);

  pointerDownHandler({ target: elsewhere() });

  assert.deepEqual(el.events, ['change']);
});

test('pointerdown on a keyboard key does NOT commit (key taps keep editing)', () => {
  const el = makeSpinButton('1');
  focus(el);

  pointerDownHandler({ target: dockKey() });

  assert.deepEqual(el.events, []);
});

test('pointerdown on the field itself does NOT commit (still editing / selecting)', () => {
  const el = makeSpinButton('1');
  focus(el);

  pointerDownHandler({ target: el });

  assert.deepEqual(el.events, []);
});

// ---- Negative numbers (GitHub #3) ---------------------------------------------------------------

test('toggleSign flips a leading minus on the focused field', () => {
  const el = makeField('INPUT', '5');
  focus(el);

  mod.toggleSign();
  assert.equal(el.value, '-5');

  mod.toggleSign();
  assert.equal(el.value, '5');
});

test('toggleSign on an empty field yields a lone minus (sign-first entry)', () => {
  const el = makeField('INPUT', '');
  focus(el);

  mod.toggleSign();

  assert.equal(el.value, '-');
});

test('onFocusIn forwards the data-mudkeyboard-allow-negative attribute to .NET', () => {
  const el = makeField('INPUT', '');
  el.getAttribute = (name) =>
    name === 'data-mudkeyboard-allow-negative' ? 'true' : name === 'type' ? 'text' : null;

  const calls = [];
  mod.initialize({ invokeMethodAsync: (m, ...rest) => { if (m === 'OnFocusIn') calls.push(rest); return Promise.resolve(); } }, 'AllInputs');
  focusInHandler({ target: el });
  runTimers();

  // OnFocusIn(layoutKind, pageMaxZIndex, currentValue, allowNegative)
  assert.equal(calls.length, 1);
  assert.equal(calls[0][3], 'true');
});

// ---- Caret reporting for the value-preview bar --------------------------------------------------
// When value reporting is on, a caret move (no 'input' event fires) reports the value AND caret to .NET
// so the preview bar can draw the cursor where the caret is.

test('moveCaret reports the new value and caret to .NET when value reporting is on', () => {
  const el = makeField('INPUT', 'abcd'); // selectionStart starts at 4 (end)
  const calls = [];
  mod.initialize({ invokeMethodAsync: (m, ...rest) => { calls.push([m, ...rest]); return Promise.resolve(); } }, 'AllInputs', true);
  focusInHandler({ target: el });
  runTimers();
  calls.length = 0;

  mod.moveCaret(-1); // 4 -> 3

  const report = calls.find((c) => c[0] === 'OnValueChanged');
  assert.ok(report, 'OnValueChanged should have been invoked');
  assert.equal(report[1], 'abcd'); // value unchanged
  assert.equal(report[2], 3);      // caret moved left one
});

test('moveCaret does NOT report when value reporting is off', () => {
  const el = makeField('INPUT', 'abcd');
  const calls = [];
  mod.initialize({ invokeMethodAsync: (m, ...rest) => { calls.push([m, ...rest]); return Promise.resolve(); } }, 'AllInputs', false);
  focusInHandler({ target: el });
  runTimers();
  calls.length = 0;

  mod.moveCaret(-1);

  assert.ok(!calls.some((c) => c[0] === 'OnValueChanged'));
});

// ---- Programmatic value changes: spin buttons, ArrowUp/Down, app code (GitHub #9) -----------------
// Blazor updates an <input> by assigning element.value, which fires no 'input' event. The shim shadows
// `value` on the focused element so such writes are reported to .NET as *external* changes — always,
// even when the preview bar (reportValue) is off — so the preview follows and "first digit replaces"
// lapses. The shim's own edits go through the prototype setter and must NOT be reported as external.

// initialize() with a recording dotnet ref and focus `el`; returns the recorded calls.
function focusRecording(el, report) {
  const calls = [];
  mod.initialize({ invokeMethodAsync: (m, ...rest) => { calls.push([m, ...rest]); return Promise.resolve(); } }, 'AllInputs', report);
  focusInHandler({ target: el });
  document.activeElement = el;
  runTimers();
  calls.length = 0;
  return calls;
}

function valueReports(calls) {
  return calls.filter((c) => c[0] === 'OnValueChanged').map((c) => ({ value: c[1], caret: c[2], external: c[3] }));
}

test('a programmatic write to the focused field (a spin button) is reported to .NET as external', () => {
  const el = makeSpinButton('5');
  const calls = focusRecording(el, true);

  el.value = '6'; // what Blazor does when MudNumericField increments

  assert.equal(el.value, '6');
  assert.deepEqual(valueReports(calls), [{ value: '6', caret: 1, external: true }]);
});

test('programmatic writes are reported even when the preview bar (value reporting) is off', () => {
  const el = makeSpinButton('5');
  const calls = focusRecording(el, false);

  el.value = '6';

  assert.deepEqual(valueReports(calls), [{ value: '6', caret: 1, external: true }]);
});

// A field whose dispatchEvent delivers 'input' to the shim's capture listener synchronously, as the DOM does.
function makeLiveField(initial) {
  const el = makeField('INPUT', initial);
  el.dispatchEvent = (e) => { el.events.push(e.type); if (e.type === 'input' && inputHandler) inputHandler({ target: el }); return true; };
  return el;
}

test("the shim's own edits are reported as the keyboard's own (not external) while reporting is on", () => {
  const el = makeLiveField('5');
  const calls = focusRecording(el, true);

  mod.insertText('2');

  assert.deepEqual(valueReports(calls), [{ value: '52', caret: 2, external: false }]);
});

test("the shim's own edits are not reported at all while reporting is off", () => {
  const el = makeLiveField('5');
  const calls = focusRecording(el, false);

  mod.insertText('2');

  assert.equal(el.value, '52');
  assert.deepEqual(valueReports(calls), []);
});

test('hardware typing into the focused field is reported as external (with reporting off too)', () => {
  const el = makeField('INPUT', 'a');
  const calls = focusRecording(el, false);

  el._value = 'ab'; // the browser applied a keystroke…
  inputHandler({ target: el }); // …and fired 'input' at the field (not from the shim)

  assert.deepEqual(valueReports(calls), [{ value: 'ab', caret: 1, external: true }]);
});

test('the value hook is removed when the keyboard closes, so nothing lingers on the element', () => {
  const el = makeSpinButton('5');
  const calls = focusRecording(el, true);
  assert.ok(Object.prototype.hasOwnProperty.call(el, 'value'), 'value should be shadowed while editing');

  mod.blurActive();
  el.value = '6';

  assert.ok(!Object.prototype.hasOwnProperty.call(el, 'value'), 'shadow removed on close');
  assert.deepEqual(valueReports(calls), []);
  assert.equal(el.value, '6'); // the native setter still works as before
});

// ---- Spin buttons never open the keyboard ---------------------------------------------------------
// MudBlazor focuses the numeric field when its ▲/▼ spin button is pressed. That focus is not a request
// to type: the keyboard must not open for it. A later tap on the (already focused) field opens it.

// A numeric field plus a press target on its ▲ spin button (closest() resolves the spin wrapper, whose
// closest('.mud-input') is the field wrapper that holds the input).
function makeSpinPair() {
  const el = makeSpinButton('5');
  const wrapper = { querySelector: () => el };
  const spinDiv = { closest: (sel) => (sel === '.mud-input' ? wrapper : null), parentElement: wrapper };
  const button = { nodeType: 1, tagName: 'BUTTON', closest: (sel) => (sel === '.mud-input-numeric-spin' ? spinDiv : null), getAttribute: () => null, hasAttribute: () => false };
  return { el, button };
}

test('focus given to a field by its spin button does not open the keyboard', () => {
  const { el, button } = makeSpinPair();
  mod.dispose();
  const calls = [];
  mod.initialize({ invokeMethodAsync: (m, ...rest) => { calls.push([m, ...rest]); return Promise.resolve(); } }, 'AllInputs', true);

  pointerDownHandler({ target: button }); // press ▲ …
  document.activeElement = el;
  focusInHandler({ target: el });         // … MudBlazor focuses the input
  runTimers();

  assert.ok(!calls.some((c) => c[0] === 'OnFocusIn'), 'keyboard must not open for a spin-button focus');
});

test('tapping a field that is already focused (after a spin button focused it) opens the keyboard', () => {
  const { el, button } = makeSpinPair();
  mod.dispose();
  const calls = [];
  mod.initialize({ invokeMethodAsync: (m, ...rest) => { calls.push([m, ...rest]); return Promise.resolve(); } }, 'AllInputs', true);
  pointerDownHandler({ target: button });
  document.activeElement = el;
  focusInHandler({ target: el });
  runTimers();
  assert.ok(!calls.some((c) => c[0] === 'OnFocusIn'));

  pointerDownHandler({ target: el }); // the user now taps the field itself — no focusin fires (already focused)

  const open = calls.find((c) => c[0] === 'OnFocusIn');
  assert.ok(open, 'keyboard opens from the tap');
  assert.equal(open[3], '5'); // seeded with the field's current value
});

test('a spin button pressed while the keyboard is open for that field keeps it open and commits first', () => {
  const { el, button } = makeSpinPair();
  const calls = focusRecording(el, true);

  pointerDownHandler({ target: button });
  el.value = '6'; // the increment lands

  assert.deepEqual(el.events, ['change']); // the pending typed value was committed before the spin
  assert.ok(!calls.some((c) => c[0] === 'OnFocusOut'), 'still open');
  assert.deepEqual(valueReports(calls), [{ value: '6', caret: 1, external: true }]);
});

test('a spin button of ANOTHER field closes the keyboard instead of opening it for that field', () => {
  const a = makeField('INPUT', 'abc');
  const { el: b, button } = makeSpinPair();
  const calls = focusRecording(a, true);

  pointerDownHandler({ target: button }); // press b's ▲ while editing a
  focusOutHandler({ target: a });
  document.activeElement = b;
  focusInHandler({ target: b });          // MudBlazor focuses b
  runTimers();

  assert.ok(!calls.some((c) => c[0] === 'OnFocusIn'), 'b does not open');
  assert.ok(calls.some((c) => c[0] === 'OnFocusOut'), 'keyboard closed');
});

test('focus bouncing back to the field being edited is not a fresh focus-in', () => {
  const el = makeSpinButton('5');
  const calls = focusRecording(el, true);

  focusOutHandler({ target: el });
  focusInHandler({ target: el }); // older MudBlazor blurs/refocuses the input around a spin click
  document.activeElement = el;
  runTimers();

  assert.ok(!calls.some((c) => c[0] === 'OnFocusIn'), 'no re-seed of the value / replace state');
  assert.ok(!calls.some((c) => c[0] === 'OnFocusOut'));
});

// ---- Settled text after closing a numeric field (GitHub #8) ---------------------------------------
// After the keyboard closes a MudNumericField, the text on screen must end up as the text the field
// settled on (Min/Max clamped, re-formatted). Blazor skips the DOM write when that text equals what it
// last rendered — but the shim typed behind its back — so the shim watches the field's aria-valuetext /
// aria-valuenow (which Blazor does re-render) and copies the settled text in when they disagree.

function makeAriaSpinButton(initial, aria) {
  const el = makeSpinButton(initial);
  el.getAttribute = (name) => {
    if (name === 'role') return 'spinbutton';
    if (name === 'type') return 'text';
    return Object.prototype.hasOwnProperty.call(aria, name) ? aria[name] : null;
  };
  return el;
}

test('after closing, a numeric field showing raw typed text is synced to its settled aria text', () => {
  const aria = { 'aria-valuenow': '30', 'aria-valuetext': '300' };
  const el = makeAriaSpinButton('300', aria); // typed 300 into a Max=30 field that already held 30
  focusRecording(el, true);
  observers.length = 0;

  mod.blurActive();
  document.activeElement = elsewhere();
  const observer = observers.at(-1);
  assert.ok(observer && observer.target === el, 'the field is watched');

  aria['aria-valuetext'] = '300';   // Blazor re-rendered after the change: text still "300"
  observer.trigger();
  assert.equal(el.value, '300');    // matches the field's text → untouched

  delete aria['aria-valuetext'];    // after blur the field settled on "30" (== aria-valuenow)
  observer.trigger();

  assert.equal(el.value, '30');
  assert.deepEqual(el.events, ['change']); // display-only: no extra events fired
});

test('the settle watcher stops after its window and never touches a field that is being edited again', () => {
  const aria = { 'aria-valuenow': '30' };
  const el = makeAriaSpinButton('300', aria);
  focusRecording(el, true);
  observers.length = 0;

  mod.blurActive();
  document.activeElement = el; // the user immediately focused it again and is typing
  const observer = observers.at(-1);
  observer.trigger();
  assert.equal(el.value, '300');  // not synced while focused

  document.activeElement = elsewhere();
  runTimers();                    // window elapsed: disconnect + one last sync

  assert.ok(observer.disconnected);
  assert.equal(el.value, '30');
});

test('the settle watcher is only attached to numeric spinbutton fields', () => {
  const el = makeField('INPUT', 'abc');
  focusRecording(el, true);
  observers.length = 0;

  mod.blurActive();

  assert.equal(observers.length, 0);
});

test('edits without a focused field are a safe no-op', () => {
  // dispose() clears activeEl; subsequent edits must not throw.
  mod.dispose();

  assert.doesNotThrow(() => {
    mod.insertText('x');
    mod.backspace();
    mod.clear();
  });
});
