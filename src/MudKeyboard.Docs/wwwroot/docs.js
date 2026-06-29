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

// Scroll-spy for the API reference's "On this page" table of contents. Watches the section anchor
// elements and reports the one currently nearest the top of the viewport back to .NET, so the matching
// TOC link can be highlighted as the reader scrolls. Returns a controller whose dispose() stops it.
export function observeSections(dotnetRef, ids) {
    const els = ids.map(id => document.getElementById(id)).filter(Boolean);
    if (els.length === 0) {
        return { dispose() { } };
    }

    let active = null;
    const line = 100; // px below the top of the viewport (clears the fixed app bar)

    const update = () => {
        // The active section is the last heading whose top has scrolled above the line.
        let current = els[0].id;
        for (const el of els) {
            if (el.getBoundingClientRect().top <= line) {
                current = el.id;
            } else {
                break;
            }
        }
        if (current !== active) {
            active = current;
            dotnetRef.invokeMethodAsync('SetActiveSection', current);
        }
    };

    // Recompute whenever a heading crosses the top band; the negative top margin puts the trigger
    // line just under the app bar instead of at the very top of the viewport.
    const observer = new IntersectionObserver(update, { rootMargin: `-${line}px 0px 0px 0px`, threshold: [0, 1] });
    els.forEach(el => observer.observe(el));
    update();

    return { dispose() { observer.disconnect(); } };
}

// Tiny localStorage wrappers (used to persist the visitor's theme-mode choice). Both swallow errors
// so a blocked/again unavailable storage (private mode, etc.) never breaks the page.
export function getStored(key) {
    try {
        return localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function setStored(key, value) {
    try {
        localStorage.setItem(key, value);
    } catch {
        // Storage unavailable (private mode / disabled) — preference just won't persist.
    }
}
