namespace Keuken_inmeten.Services.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public sealed class ScharnierOverzichtViewerJsInterop(IJSRuntime js) : JsModuleInterop(js, "./Components/ScharnierOverzichtVisueel.razor.js")
{
    public ValueTask InitAsync(ElementReference stage, ElementReference canvas, ElementReference focusTarget)
        => InvokeVoidAsync("init", stage, canvas, focusTarget);

    public ValueTask ZoomInAsync(ElementReference stage)
        => InvokeVoidAsync("zoomIn", stage);

    public ValueTask ZoomOutAsync(ElementReference stage)
        => InvokeVoidAsync("zoomOut", stage);

    public ValueTask ResetAsync(ElementReference stage)
        => InvokeVoidAsync("reset", stage);

    public ValueTask DisposeViewerAsync(ElementReference stage)
        => InvokeVoidIfLoadedAsync("dispose", stage);
}
