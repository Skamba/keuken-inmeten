namespace Keuken_inmeten.Services.Interop;

using Keuken_inmeten.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public sealed class WandOpstellingJsInterop(IJSRuntime js) : JsModuleInterop(js, "./Components/WandOpstelling.razor.js")
{
    public ValueTask InitAsync(ElementReference svgRef, DotNetObjectReference<WandOpstelling> dotNetRef, bool leesAlleen)
        => InvokeVoidAsync("init", svgRef, dotNetRef, leesAlleen);

    public ValueTask DisposeSvgAsync(ElementReference svgRef)
        => InvokeVoidIfLoadedAsync("dispose", svgRef);
}
