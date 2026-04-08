using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenDomeinDefaultsTests
{
    [Fact]
    public void Modellen_en_formhelpers_gebruiken_dezelfde_centrale_defaults()
    {
        var modelKast = new Kast();
        var formulierKast = IndelingFormulierHelper.NieuweKast();
        var modelWand = new KeukenWand();
        var formulierWand = IndelingFormulierHelper.NieuweWand();

        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Type, modelKast.Type);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Breedte, modelKast.Breedte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Hoogte, modelKast.Hoogte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Diepte, modelKast.Diepte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Wanddikte, modelKast.Wanddikte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.GaatjesAfstand, modelKast.GaatjesAfstand);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.EersteGaatVanBoven, modelKast.EersteGaatVanBoven);

        Assert.Equal(modelKast.Breedte, formulierKast.Breedte);
        Assert.Equal(modelKast.Hoogte, formulierKast.Hoogte);
        Assert.Equal(modelKast.Diepte, formulierKast.Diepte);
        Assert.Equal(modelKast.Wanddikte, formulierKast.Wanddikte);
        Assert.Equal(modelKast.GaatjesAfstand, formulierKast.GaatjesAfstand);
        Assert.Equal(modelKast.EersteGaatVanBoven, formulierKast.EersteGaatVanBoven);

        Assert.Equal(KeukenDomeinDefaults.WandDefaults.Breedte, modelWand.Breedte);
        Assert.Equal(KeukenDomeinDefaults.WandDefaults.Hoogte, modelWand.Hoogte);
        Assert.Equal(KeukenDomeinDefaults.WandDefaults.PlintHoogte, modelWand.PlintHoogte);
        Assert.Equal(modelWand.Breedte, formulierWand.Breedte);
        Assert.Equal(modelWand.Hoogte, formulierWand.Hoogte);
        Assert.Equal(modelWand.PlintHoogte, formulierWand.PlintHoogte);
    }

    [Fact]
    public void Sharecodec_decodeert_ontbrekende_defaultwaarden_naar_dezelfde_bron()
    {
        var wandId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var kastId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var data = new KeukenData
        {
            Wanden =
            [
                new KeukenWand
                {
                    Id = wandId,
                    Naam = "Muur",
                    KastIds = [kastId]
                }
            ],
            Kasten =
            [
                new Kast
                {
                    Id = kastId,
                    Naam = "Kast"
                }
            ]
        };

        var token = KeukenShareCodec.Encode(data);
        var decodedOk = KeukenShareCodec.TryDecode(token, out var decoded);

        Assert.True(decodedOk);

        var wand = Assert.Single(decoded.Wanden);
        var kast = Assert.Single(decoded.Kasten);

        Assert.Equal(KeukenDomeinDefaults.WandDefaults.Breedte, wand.Breedte);
        Assert.Equal(KeukenDomeinDefaults.WandDefaults.Hoogte, wand.Hoogte);
        Assert.Equal(KeukenDomeinDefaults.WandDefaults.PlintHoogte, wand.PlintHoogte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Breedte, kast.Breedte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Hoogte, kast.Hoogte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Diepte, kast.Diepte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.Wanddikte, kast.Wanddikte);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.GaatjesAfstand, kast.GaatjesAfstand);
        Assert.Equal(KeukenDomeinDefaults.KastDefaults.EersteGaatVanBoven, kast.EersteGaatVanBoven);
    }
}
