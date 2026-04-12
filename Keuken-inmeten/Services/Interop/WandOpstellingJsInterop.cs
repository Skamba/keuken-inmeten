namespace Keuken_inmeten.Services.Interop;

using Keuken_inmeten.Components;
using Microsoft.JSInterop;

public sealed class WandOpstellingJsInterop(IJSRuntime js) : JsModuleInterop(js, "./Components/WandOpstelling.razor.js")
{
    public ValueTask InitAsync(string svgElementId, DotNetObjectReference<WandOpstelling> dotNetRef, bool leesAlleen)
        => InvokeVoidAsync("init", svgElementId, dotNetRef, leesAlleen);

    public ValueTask DisposeSvgAsync(string svgElementId)
        => InvokeVoidIfLoadedAsync("dispose", svgElementId);
}
