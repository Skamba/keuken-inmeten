namespace Keuken_inmeten.Services;

using Keuken_inmeten.Models;
using Keuken_inmeten.Services.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class PersistentieService : IDisposable, IAsyncDisposable
{
    private const string StorageKey = "keuken-inmeten-data";

    private readonly IJSRuntime _js;
    private readonly KeukenStateService _state;
    private readonly NavigationManager _navigation;
    private ShareCompressionJsInterop? _shareCompressionInterop;
    private bool _bewaarGepland;
    private bool _disposed;
    public bool HeeftGedeeldeLinkGeladen { get; private set; }
    public bool HeeftOngeldigeDeelLink { get; private set; }
    public event Action? OnOpslagStatusChanged;
    public bool HeeftOpslagStatus => !string.IsNullOrWhiteSpace(OpslagStatusTekst);
    public string? OpslagStatusTekst { get; private set; }
    public OpslagStatusType OpslagStatus { get; private set; } = OpslagStatusType.Opgeslagen;
    private ShareCompressionJsInterop ShareCompressionInterop => _shareCompressionInterop ??= new(_js);

    public PersistentieService(IJSRuntime js, KeukenStateService state, NavigationManager navigation)
    {
        _js = js;
        _state = state;
        _navigation = navigation;
        _state.OnStateChanged += OpStateGewijzigd;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _state.OnStateChanged -= OpStateGewijzigd;
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();

        if (_shareCompressionInterop is null)
            return;

        await _shareCompressionInterop.DisposeAsync();
        _shareCompressionInterop = null;
    }

    private void ZetOpslagStatus(OpslagStatusType status, string tekst)
    {
        OpslagStatus = status;
        OpslagStatusTekst = tekst;
        OnOpslagStatusChanged?.Invoke();
    }
}

public enum OpslagStatusType
{
    Bezig,
    Opgeslagen,
    Fout
}
