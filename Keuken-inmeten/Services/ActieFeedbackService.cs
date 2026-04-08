namespace Keuken_inmeten.Services;

public enum ActieFeedbackType
{
    Info,
    Succes,
    Fout
}

public sealed record ActieFeedbackMelding(
    string Bericht,
    ActieFeedbackType Type,
    string? ActieLabel = null,
    Func<Task>? Actie = null);

public sealed class ActieFeedbackService
{
    public event Action? OnChanged;

    public ActieFeedbackMelding? HuidigeMelding { get; private set; }

    public void ToonInfo(string bericht, string? actieLabel = null, Func<Task>? actie = null)
        => Toon(new ActieFeedbackMelding(bericht, ActieFeedbackType.Info, actieLabel, actie));

    public void ToonSucces(string bericht, string? actieLabel = null, Func<Task>? actie = null)
        => Toon(new ActieFeedbackMelding(bericht, ActieFeedbackType.Succes, actieLabel, actie));

    public void ToonFout(string bericht, string? actieLabel = null, Func<Task>? actie = null)
        => Toon(new ActieFeedbackMelding(bericht, ActieFeedbackType.Fout, actieLabel, actie));

    public void Sluit()
    {
        if (HuidigeMelding is null)
            return;

        HuidigeMelding = null;
        OnChanged?.Invoke();
    }

    public async Task VoerActieUitAsync()
    {
        if (HuidigeMelding?.Actie is not { } actie)
            return;

        HuidigeMelding = null;
        OnChanged?.Invoke();
        await actie();
    }

    private void Toon(ActieFeedbackMelding melding)
    {
        HuidigeMelding = melding;
        OnChanged?.Invoke();
    }
}
