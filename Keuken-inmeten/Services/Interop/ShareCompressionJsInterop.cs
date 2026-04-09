namespace Keuken_inmeten.Services.Interop;

using Microsoft.JSInterop;

public sealed class ShareCompressionJsInterop(IJSRuntime js) : JsModuleInterop(js, "./js/shareCompressionInterop.js")
{
    public ValueTask<string> CompressSharePayloadAsync(string json)
        => InvokeAsync<string>("compressSharePayload", json);

    public ValueTask<string> DecompressSharePayloadAsync(string compressedPayload)
        => InvokeAsync<string>("decompressSharePayload", compressedPayload);
}
