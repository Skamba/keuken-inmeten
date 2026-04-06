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
