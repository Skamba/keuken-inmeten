export function printCurrentPage() {
    window.print();
}

export function downloadTextFile(filename, content, mimeType) {
    const blob = new Blob(["\uFEFF", content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function openPrintDocument(html) {
    const printWindow = window.open("", "_blank");
    if (!printWindow) {
        throw new Error("Print window could not be opened.");
    }

    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
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
