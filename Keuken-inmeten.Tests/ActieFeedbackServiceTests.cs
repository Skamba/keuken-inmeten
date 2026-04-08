using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class ActieFeedbackServiceTests
{
    [Fact]
    public void ToonSucces_stelt_huidige_melding_in()
    {
        var service = new ActieFeedbackService();

        service.ToonSucces("Export gelukt.");

        Assert.NotNull(service.HuidigeMelding);
        Assert.Equal("Export gelukt.", service.HuidigeMelding!.Bericht);
        Assert.Equal(ActieFeedbackType.Succes, service.HuidigeMelding.Type);
    }

    [Fact]
    public async Task VoerActieUitAsync_voert_undoactie_uit_en_wist_melding()
    {
        var service = new ActieFeedbackService();
        var uitgevoerd = false;

        service.ToonInfo(
            "Paneel verwijderd.",
            "Ongedaan maken",
            () =>
            {
                uitgevoerd = true;
                return Task.CompletedTask;
            });

        await service.VoerActieUitAsync();

        Assert.True(uitgevoerd);
        Assert.Null(service.HuidigeMelding);
    }

    [Fact]
    public void Sluit_wist_de_actieve_melding()
    {
        var service = new ActieFeedbackService();
        service.ToonFout("Delen mislukt.");

        service.Sluit();

        Assert.Null(service.HuidigeMelding);
    }
}
