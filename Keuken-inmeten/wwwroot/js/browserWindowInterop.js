export function printCurrentPage() {
    window.print();
}

export async function shareUrl(url, title, text) {
    if (navigator.share && (!navigator.canShare || navigator.canShare({ url }))) {
        try {
            await navigator.share({ url, title, text });
            return "shared";
        } catch (error) {
            if (error && error.name === "AbortError") {
                return "cancelled";
            }
        }
    }

    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(url);
            return "copied";
        }
    } catch {
        // Fall back to execCommand below.
    }

    const input = document.createElement("textarea");
    input.value = url;
    input.setAttribute("readonly", "");
    input.style.position = "fixed";
    input.style.opacity = "0";
    document.body.appendChild(input);
    input.select();
    input.setSelectionRange(0, input.value.length);
    const copied = document.execCommand("copy");
    document.body.removeChild(input);
    return copied ? "copied" : "unsupported";
}
