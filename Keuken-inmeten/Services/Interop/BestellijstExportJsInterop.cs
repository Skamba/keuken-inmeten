namespace Keuken_inmeten.Services.Interop;

using Microsoft.JSInterop;

public sealed class BestellijstExportJsInterop(IJSRuntime js) : JsModuleInterop(js, "./Pages/Bestellijst.razor.js")
{
    public ValueTask DownloadTextFileAsync(string filename, string content, string mimeType)
        => InvokeVoidAsync("downloadTextFile", filename, content, mimeType);

    public ValueTask OpenPrintDocumentAsync(string html)
        => InvokeVoidAsync("openPrintDocument", html);
}
