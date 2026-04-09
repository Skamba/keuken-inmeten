namespace Keuken_inmeten.Services.Interop;

using Microsoft.JSInterop;

public sealed class BrowserWindowJsInterop(IJSRuntime js) : JsModuleInterop(js, "./js/browserWindowInterop.js")
{
    public ValueTask PrintCurrentPageAsync() => InvokeVoidAsync("printCurrentPage");

    public ValueTask DownloadTextFileAsync(string filename, string content, string mimeType)
        => InvokeVoidAsync("downloadTextFile", filename, content, mimeType);

    public ValueTask OpenPrintDocumentAsync(string html)
        => InvokeVoidAsync("openPrintDocument", html);

    public ValueTask<string> ShareUrlAsync(string url, string title, string text)
        => InvokeAsync<string>("shareUrl", url, title, text);
}
