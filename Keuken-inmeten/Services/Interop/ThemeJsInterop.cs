namespace Keuken_inmeten.Services.Interop;

using Microsoft.JSInterop;

public sealed class ThemeJsInterop(IJSRuntime js) : JsModuleInterop(js, "./js/themeInterop.js")
{
    public ValueTask<string> GetThemeAsync() => InvokeAsync<string>("getTheme");

    public ValueTask<string> ToggleThemeAsync() => InvokeAsync<string>("toggleTheme");
}
