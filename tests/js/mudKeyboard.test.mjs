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
globalThis.document = {
  addEventListener: (type, handler) => {
    if (type === 'focusin') focusInHandler = handler;
    if (type === 'focusout') focusOutHandler = handler;
    if (type === 'pointerdown') pointerDownHandler = handler;
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
function runTimers() { const due = pendingTimers; pendingTimers = []; for (const fn of due) fn(); }

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

test('edits without a focused field are a safe no-op', () => {
  // dispose() clears activeEl; subsequent edits must not throw.
  mod.dispose();

  assert.doesNotThrow(() => {
    mod.insertText('x');
    mod.backspace();
    mod.clear();
  });
});
