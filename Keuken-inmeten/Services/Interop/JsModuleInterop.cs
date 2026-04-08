namespace Keuken_inmeten.Services.Interop;

using Microsoft.JSInterop;

public abstract class JsModuleInterop(IJSRuntime js, string modulePath) : IAsyncDisposable
{
    private Task<IJSObjectReference>? moduleTask;

    protected bool IsModuleLoaded => moduleTask is not null;

    protected async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        moduleTask ??= js.InvokeAsync<IJSObjectReference>("import", modulePath).AsTask();
        return await moduleTask;
    }

    protected async ValueTask InvokeVoidAsync(string identifier, params object?[] args)
        => await (await GetModuleAsync()).InvokeVoidAsync(identifier, args);

    protected async ValueTask<T> InvokeAsync<T>(string identifier, params object?[] args)
        => await (await GetModuleAsync()).InvokeAsync<T>(identifier, args);

    protected async ValueTask InvokeVoidIfLoadedAsync(string identifier, params object?[] args)
    {
        if (moduleTask is null)
            return;

        await (await moduleTask).InvokeVoidAsync(identifier, args);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask is null)
            return;

        await (await moduleTask).DisposeAsync();
    }
}
