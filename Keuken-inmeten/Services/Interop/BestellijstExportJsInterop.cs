namespace Keuken_inmeten.Services.Interop;

using Microsoft.JSInterop;

public sealed class BestellijstExportJsInterop(IJSRuntime js) : JsModuleInterop(js, "./js/browserWindowInterop.js")
{
    public ValueTask DownloadTextFileAsync(string filename, string content, string mimeType)
        => InvokeVoidAsync("downloadTextFile", filename, content, mimeType);

    public ValueTask DownloadPdfDocumentAsync(string filename, string payloadJson)
        => InvokeVoidAsync("downloadPdfDocument", filename, payloadJson);
}
