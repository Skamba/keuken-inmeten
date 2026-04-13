const instances = new WeakMap();
const FOCUSABLE_SELECTOR = [
    "a[href]",
    "button:not([disabled])",
    "input:not([disabled])",
    "select:not([disabled])",
    "textarea:not([disabled])",
    "[tabindex]:not([tabindex='-1'])"
].join(",");

function isVisible(element) {
    return !element.hasAttribute("hidden")
        && element.getAttribute("aria-hidden") !== "true"
        && (element.offsetWidth > 0 || element.offsetHeight > 0 || element.getClientRects().length > 0);
}

function getFocusableElements(shell) {
    return [...shell.querySelectorAll(FOCUSABLE_SELECTOR)]
        .filter((element) => element !== shell && isVisible(element));
}

function trapTabNavigation(shell, event) {
    if (event.key !== "Tab") {
        return;
    }

    const focusable = getFocusableElements(shell);
    if (focusable.length === 0) {
        event.preventDefault();
        shell.focus();
        return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const activeElement = document.activeElement;

    if (event.shiftKey) {
        if (activeElement === shell || activeElement === first || !shell.contains(activeElement)) {
            event.preventDefault();
            last.focus();
        }

        return;
    }

    if (activeElement === shell || activeElement === last || !shell.contains(activeElement)) {
        event.preventDefault();
        first.focus();
    }
}

function createInstance(shell) {
    const previousOverflow = document.body.style.overflow;
    const onKeyDown = (event) => trapTabNavigation(shell, event);

    document.body.style.overflow = "hidden";
    shell.addEventListener("keydown", onKeyDown);

    return {
        previousOverflow,
        onKeyDown
    };
}

export function init(shell) {
    dispose(shell);
    instances.set(shell, createInstance(shell));
}

export function dispose(shell) {
    const instance = instances.get(shell);
    if (!instance) {
        return;
    }

    shell.removeEventListener("keydown", instance.onKeyDown);
    document.body.style.overflow = instance.previousOverflow;
    instances.delete(shell);
}
