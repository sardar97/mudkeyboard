// MudKeyboard docs site — tiny browser helpers (syntax highlighting + clipboard).
// This belongs to the documentation site, NOT the MudKeyboard library (whose core is JS-free).
// Loaded as an ES module by DocsInterop.cs over standard Blazor JS interop.

// Highlight a single <code> element with highlight.js (loaded globally from the CDN in index.html).
export function highlight(element) {
    if (element && window.hljs) {
        // Re-highlighting is safe; clear the flag highlight.js sets so updated code re-colours.
        element.removeAttribute('data-highlighted');
        element.classList.remove('hljs');
        window.hljs.highlightElement(element);
    }
}

// Copy arbitrary text to the clipboard. Returns true on success.
export async function copy(text) {
    try {
        await navigator.clipboard.writeText(text ?? '');
        return true;
    } catch {
        return false;
    }
}
