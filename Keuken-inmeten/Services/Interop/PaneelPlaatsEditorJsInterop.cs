namespace Keuken_inmeten.Services.Interop;

using Keuken_inmeten.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public sealed class PaneelPlaatsEditorJsInterop(IJSRuntime js) : JsModuleInterop(js, "./Components/PaneelPlaatsEditor.razor.js")
{
    public ValueTask InitAsync(ElementReference svgRef, DotNetObjectReference<PaneelPlaatsEditor> dotNetRef)
        => InvokeVoidAsync("init", svgRef, dotNetRef);

    public ValueTask DisposeSvgAsync(ElementReference svgRef)
        => InvokeVoidIfLoadedAsync("dispose", svgRef);
}
