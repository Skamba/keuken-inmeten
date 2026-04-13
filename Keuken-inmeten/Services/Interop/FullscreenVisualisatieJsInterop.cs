namespace Keuken_inmeten.Services.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public sealed class FullscreenVisualisatieJsInterop(IJSRuntime js) : JsModuleInterop(js, "./Components/FullscreenVisualisatie.razor.js")
{
    public ValueTask InitAsync(ElementReference shell)
        => InvokeVoidAsync("init", shell);

    public ValueTask DisposeViewerAsync(ElementReference shell)
        => InvokeVoidIfLoadedAsync("dispose", shell);
}
