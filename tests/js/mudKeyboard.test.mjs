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
globalThis.document = {
  addEventListener: (type, handler) => { if (type === 'focusin') focusInHandler = handler; },
  removeEventListener: () => {},
  body: { getElementsByTagName: () => [] },
  querySelector: () => null,
  activeElement: null,
};
// Don't actually run deferred work (the scroll-into-view timer); just hand back a ticket.
globalThis.setTimeout = () => 0;

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

// Focus an element through the real focusin path so the module's internal activeEl is set.
function focus(el) {
  mod.initialize({ invokeMethodAsync: () => Promise.resolve() }, 'AllInputs');
  focusInHandler({ target: el });
  el.events = []; // ignore anything emitted during focus; tests assert on edits only
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

test('getValue returns the focused field value', () => {
  const el = makeField('INPUT', 'xyz');
  focus(el);

  assert.equal(mod.getValue(), 'xyz');
});

test('insertText works on a <textarea> via the textarea native setter', () => {
  const el = makeField('TEXTAREA', '');
  focus(el);

  mod.insertText('hi');

  assert.equal(el.value, 'hi');
  assert.deepEqual(el.events, ['input', 'change']);
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
