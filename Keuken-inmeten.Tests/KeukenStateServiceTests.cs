using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class KeukenStateServiceTests
{
    [Fact]
    public void Deur_toewijzing_bewaart_laatst_gebruikte_pot_hart_afstand()
    {
        var state = new KeukenStateService();

        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            PotHartVanRand = 25
        });

        Assert.Equal(25, state.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void Laden_herstelt_laatst_gebruikte_pot_hart_afstand()
    {
        var state = new KeukenStateService();

        state.Laden(new KeukenData
        {
            LaatstGebruiktePotHartVanRand = 24.5
        });

        Assert.Equal(24.5, state.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void Exporteren_en_laden_bewaren_paneelrandspeling()
    {
        var state = new KeukenStateService();
        state.StelPaneelRandSpelingIn(5);

        var snapshot = state.Exporteren();
        var herladen = new KeukenStateService();
        herladen.Laden(snapshot);

        Assert.Equal(5, snapshot.PaneelRandSpeling);
        Assert.Equal(5, herladen.PaneelRandSpeling);
    }

    [Fact]
    public void Verificatiechecks_roundtrippen_via_export_en_laden()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        var kast = MaakKast("Onderkast");
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            Type = PaneelType.BlindPaneel,
            KastIds = [kast.Id],
            Breedte = 600,
            Hoogte = 720
        };

        state.VoegWandToe(wand);
        state.VoegKastToe(kast, wand.Id);
        state.VoegToewijzingToe(toewijzing);

        var gewijzigd = state.WerkVerificatieStatusBij(toewijzing.Id, matenOk: true, scharnierPositiesOk: false);

        Assert.True(gewijzigd);
        var snapshot = state.Exporteren();
        var herladen = new KeukenStateService();
        herladen.Laden(snapshot);

        var status = Assert.Single(snapshot.VerificatieStatussen);
        Assert.Equal(toewijzing.Id, status.ToewijzingId);
        Assert.True(status.MatenOk);
        Assert.False(status.ScharnierPositiesOk);

        var herladenStatus = Assert.Single(herladen.VerificatieStatussen);
        Assert.Equal(toewijzing.Id, herladenStatus.ToewijzingId);
        Assert.True(herladenStatus.MatenOk);
        Assert.False(herladenStatus.ScharnierPositiesOk);
    }

    [Fact]
    public void Importeer_vervangt_de_huidige_status_en_triggert_exact_een_state_change()
    {
        var state = new KeukenStateService();
        var oudeWand = MaakWand("Oude wand");
        state.VoegWandToe(oudeWand);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var wandId = Guid.NewGuid();
        var kastId = Guid.NewGuid();

        state.Importeer(new KeukenData
        {
            LaatstGebruiktePotHartVanRand = 24.5,
            PaneelRandSpeling = 5,
            Wanden =
            [
                new KeukenWand
                {
                    Id = wandId,
                    Naam = "Nieuwe wand",
                    KastIds = [kastId]
                }
            ],
            Kasten =
            [
                new Kast
                {
                    Id = kastId,
                    Naam = "Nieuwe kast",
                    Breedte = 600,
                    Hoogte = 720,
                    Diepte = 560
                }
            ]
        });

        Assert.Equal(1, notificaties);
        var wand = Assert.Single(state.Wanden);
        var kast = Assert.Single(state.Kasten);
        Assert.Equal("Nieuwe wand", wand.Naam);
        Assert.Equal("Nieuwe kast", kast.Naam);
        Assert.DoesNotContain(state.Wanden, item => item.Id == oudeWand.Id);
        Assert.Equal(24.5, state.LaatstGebruiktePotHartVanRand);
        Assert.Equal(5, state.PaneelRandSpeling);
    }

    [Fact]
    public void VerwijderAlles_wist_projectinhoud_en_herstelt_standaarden()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);
        state.VoegKastToe(MaakKast("Onderkast"), wand.Id);
        state.StelPaneelRandSpelingIn(5);
        state.VoegToewijzingToe(new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            PotHartVanRand = 24.5
        });

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        state.VerwijderAlles();

        Assert.Equal(1, notificaties);
        Assert.False(state.HeeftProjectInhoud());
        Assert.Empty(state.Wanden);
        Assert.Empty(state.Kasten);
        Assert.Empty(state.Apparaten);
        Assert.Empty(state.Toewijzingen);
        Assert.Empty(state.KastTemplates);
        Assert.Equal(ScharnierBerekeningService.CupCenterVanRand, state.LaatstGebruiktePotHartVanRand);
        Assert.Equal(PaneelSpelingService.DefaultRandSpeling, state.PaneelRandSpeling);
    }

    [Fact]
    public void WerkToewijzingBij_vervangt_bestaande_toewijzing_en_bewaart_laatst_gebruikte_pot_hart_afstand()
    {
        var state = new KeukenStateService();
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            Type = PaneelType.Deur,
            PotHartVanRand = 22.5,
            Breedte = 600,
            Hoogte = 2200
        };

        state.VoegToewijzingToe(toewijzing);

        state.WerkToewijzingBij(new PaneelToewijzing
        {
            Id = toewijzing.Id,
            Type = PaneelType.Deur,
            PotHartVanRand = 24.5,
            Breedte = 620,
            Hoogte = 2100
        });

        var bijgewerkt = Assert.Single(state.Toewijzingen);
        Assert.Equal(620, bijgewerkt.Breedte);
        Assert.Equal(2100, bijgewerkt.Hoogte);
        Assert.Equal(24.5, state.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void VerwijderToewijzing_wist_gekoppelde_verificatiestatus()
    {
        var state = new KeukenStateService();
        var toewijzing = new PaneelToewijzing
        {
            Id = Guid.NewGuid(),
            Type = PaneelType.BlindPaneel,
            Breedte = 600,
            Hoogte = 720
        };

        state.VoegToewijzingToe(toewijzing);
        state.WerkVerificatieStatusBij(toewijzing.Id, matenOk: true, scharnierPositiesOk: false);

        state.VerwijderToewijzing(toewijzing.Id);

        Assert.Empty(state.VerificatieStatussen);
    }

    [Fact]
    public void VoegToewijzingenToe_voegt_meerdere_toe_met_exact_een_state_change()
    {
        var state = new KeukenStateService();
        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        state.VoegToewijzingenToe(
        [
            new PaneelToewijzing
            {
                Type = PaneelType.Deur,
                PotHartVanRand = 24.5,
                Breedte = 600,
                Hoogte = 200
            },
            new PaneelToewijzing
            {
                Type = PaneelType.LadeFront,
                Breedte = 600,
                Hoogte = 300
            }
        ]);

        Assert.Equal(2, state.Toewijzingen.Count);
        Assert.Equal(1, notificaties);
        Assert.Equal(24.5, state.LaatstGebruiktePotHartVanRand);
    }

    [Fact]
    public void HernoemWand_triggert_exact_een_state_change()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var gewijzigd = state.HernoemWand(wand.Id, "Rechterwand");

        Assert.True(gewijzigd);
        Assert.Equal("Rechterwand", wand.Naam);
        Assert.Equal(1, notificaties);
    }

    [Fact]
    public void WerkWandAfmetingenBij_triggert_exact_een_state_change()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var gewijzigd = state.WerkWandAfmetingenBij(wand.Id, 2600, 2800, 120);

        Assert.True(gewijzigd);
        Assert.Equal(2600, wand.Breedte);
        Assert.Equal(2800, wand.Hoogte);
        Assert.Equal(120, wand.PlintHoogte);
        Assert.Equal(1, notificaties);
    }

    [Fact]
    public void WerkWandAfmetingenBij_weigert_krimp_als_inhoud_niet_meer_past()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);
        state.VoegKastToe(MaakGeplaatsteKast("Rechts", xPositie: 1800), wand.Id);

        var gewijzigd = state.WerkWandAfmetingenBij(wand.Id, 2000, wand.Hoogte, wand.PlintHoogte);

        Assert.False(gewijzigd);
        Assert.Equal(2400, wand.Breedte);
    }

    [Fact]
    public void VerplaatsKast_weigert_overlap_met_bestaande_kast()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);
        var links = MaakGeplaatsteKast("Links", xPositie: 0);
        var rechts = MaakGeplaatsteKast("Rechts", xPositie: 600);
        state.VoegKastToe(links, wand.Id);
        state.VoegKastToe(rechts, wand.Id);

        var verplaatst = state.VerplaatsKast(rechts.Id, 0, rechts.HoogteVanVloer);

        Assert.False(verplaatst);
        Assert.Equal(600, rechts.XPositie);
    }

    [Fact]
    public void VerplaatsApparaat_weigert_overlap_met_kast()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);
        state.VoegKastToe(MaakGeplaatsteKast("Onderkast", xPositie: 0), wand.Id);
        var apparaat = MaakApparaat("Oven", xPositie: 700, hoogteVanVloer: 100);
        state.VoegApparaatToe(apparaat, wand.Id);

        var verplaatst = state.VerplaatsApparaat(apparaat.Id, 0, 100);

        Assert.False(verplaatst);
        Assert.Equal(700, apparaat.XPositie);
    }

    [Fact]
    public void WerkApparaatBij_weigert_maten_die_met_andere_apparaten_botsen()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);
        var links = MaakApparaat("Oven", xPositie: 600, hoogteVanVloer: 100, breedte: 300);
        var rechts = MaakApparaat("Magnetron", xPositie: 900, hoogteVanVloer: 100, breedte: 300, type: ApparaatType.Magnetron);
        state.VoegApparaatToe(links, wand.Id);
        state.VoegApparaatToe(rechts, wand.Id);

        var gewijzigd = state.WerkApparaatBij(MaakApparaat("Oven", links.Id, xPositie: 600, hoogteVanVloer: 100, breedte: 400));

        Assert.False(gewijzigd);
        Assert.Equal(300, links.Breedte);
    }

    [Fact]
    public void HerstelKastMetToewijzingen_weigert_bezette_plek()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);
        state.VoegKastToe(MaakGeplaatsteKast("Bestaand", xPositie: 0), wand.Id);

        var hersteld = state.HerstelKastMetToewijzingen(
            MaakGeplaatsteKast("Terug", xPositie: 0, id: Guid.NewGuid()),
            wand.Id,
            0,
            []);

        Assert.False(hersteld);
        Assert.Single(state.Kasten);
    }

    [Fact]
    public void HerstelApparaat_weigert_bezette_plek()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);
        state.VoegApparaatToe(MaakApparaat("Bestaand", xPositie: 600, hoogteVanVloer: 100), wand.Id);

        var hersteld = state.HerstelApparaat(
            MaakApparaat("Terug", Guid.NewGuid(), xPositie: 600, hoogteVanVloer: 100),
            wand.Id,
            0);

        Assert.False(hersteld);
        Assert.Single(state.Apparaten);
    }

    [Fact]
    public void Plank_commando_synchroniseren_state_change_pipeline()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);

        var kast = MaakKast("Onderkast");
        state.VoegKastToe(kast, wand.Id);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var plank = state.VoegPlankToe(kast.Id, 320);
        Assert.NotNull(plank);
        Assert.Equal(1, notificaties);
        Assert.Single(kast.Planken);

        var verplaatst = state.VerplaatsPlank(kast.Id, plank!.Id, 352);
        Assert.True(verplaatst);
        Assert.Equal(352, kast.Planken[0].HoogteVanBodem);
        Assert.Equal(2, notificaties);

        var verwijderd = state.VerwijderPlank(kast.Id, plank.Id);
        Assert.True(verwijderd);
        Assert.Empty(kast.Planken);
        Assert.Equal(3, notificaties);

        var hersteld = state.HerstelPlank(kast.Id, plank, 0);
        Assert.NotNull(hersteld);
        Assert.Single(kast.Planken);
        Assert.Equal(plank.Id, kast.Planken[0].Id);
        Assert.Equal(4, notificaties);
    }

    [Fact]
    public void VoegKastToe_normailseert_ongeldige_shape_buiten_ui_flow()
    {
        var state = new KeukenStateService();
        var wand = MaakWand("Achterwand");
        state.VoegWandToe(wand);

        state.VoegKastToe(new Kast
        {
            Naam = "  Testkast  ",
            Breedte = -10,
            Hoogte = 0,
            Diepte = -20,
            Wanddikte = -1,
            GaatjesAfstand = 0,
            EersteGaatVanBoven = -2,
            XPositie = -50,
            HoogteVanVloer = -25,
            Planken = [new Plank { HoogteVanBodem = 9999 }]
        }, wand.Id);

        var kast = Assert.Single(state.Kasten);
        Assert.Equal("Testkast", kast.Naam);
        Assert.Equal(600, kast.Breedte);
        Assert.Equal(720, kast.Hoogte);
        Assert.Equal(560, kast.Diepte);
        Assert.Equal(18, kast.Wanddikte);
        Assert.Equal(32, kast.GaatjesAfstand);
        Assert.Equal(19, kast.EersteGaatVanBoven);
        Assert.Equal(0, kast.XPositie);
        Assert.Equal(0, kast.HoogteVanVloer);
        Assert.Equal(720, kast.Planken[0].HoogteVanBodem);
        Assert.NotEmpty(kast.MontagePlaatPosities);
    }

    [Fact]
    public void SluitAlleGatenOpWand_sluit_kleine_gaten_tussen_aangrenzende_kasten()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Testwand", Breedte = 3000, Hoogte = 2700 };
        state.VoegWandToe(wand);

        var groot = new Kast { Naam = "Groot", Type = KastType.Onderkast, Breedte = 600, Hoogte = 1915, Diepte = 560, Wanddikte = 18, GaatjesAfstand = 32, EersteGaatVanBoven = 19, HoogteVanVloer = 0 };
        var klein = new Kast { Naam = "Klein", Type = KastType.Onderkast, Breedte = 600, Hoogte = 315, Diepte = 560, Wanddikte = 18, GaatjesAfstand = 32, EersteGaatVanBoven = 19, HoogteVanVloer = 1920 };

        state.VoegKastToe(groot, wand.Id);
        state.VoegKastToe(klein, wand.Id);

        var gewijzigd = state.SluitAlleGatenOpWand(wand.Id);

        Assert.True(gewijzigd);
        var kleinNa = state.Kasten.Find(k => k.Id == klein.Id)!;
        Assert.Equal(1915d, kleinNa.HoogteVanVloer);
    }

    [Fact]
    public void SluitAlleGatenOpWand_doet_niets_als_er_geen_gaten_zijn()
    {
        var state = new KeukenStateService();
        var wand = new KeukenWand { Naam = "Testwand", Breedte = 3000, Hoogte = 2700 };
        state.VoegWandToe(wand);

        var groot = new Kast { Naam = "Groot", Type = KastType.Onderkast, Breedte = 600, Hoogte = 1915, Diepte = 560, Wanddikte = 18, GaatjesAfstand = 32, EersteGaatVanBoven = 19, HoogteVanVloer = 0 };
        var klein = new Kast { Naam = "Klein", Type = KastType.Onderkast, Breedte = 600, Hoogte = 315, Diepte = 560, Wanddikte = 18, GaatjesAfstand = 32, EersteGaatVanBoven = 19, HoogteVanVloer = 1915 };

        state.VoegKastToe(groot, wand.Id);
        state.VoegKastToe(klein, wand.Id);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var gewijzigd = state.SluitAlleGatenOpWand(wand.Id);

        Assert.False(gewijzigd);
        Assert.Equal(0, notificaties);
    }

    [Fact]
    public void VoegKastToe_zonder_bestaande_wand_voegt_niets_toe()
    {
        var state = new KeukenStateService();

        var toegevoegd = state.VoegKastToe(MaakKast("Losse kast"), Guid.NewGuid());

        Assert.False(toegevoegd);
        Assert.Empty(state.Kasten);
    }

    [Fact]
    public void WerkKastBij_zonder_bestaande_kast_wordt_afgewezen_zonder_state_change()
    {
        var state = new KeukenStateService();
        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var gewijzigd = state.WerkKastBij(MaakKast("Losse kast"));

        Assert.False(gewijzigd);
        Assert.Equal(0, notificaties);
    }

    [Fact]
    public void WerkKastBijOpWand_verplaatst_kast_naar_een_andere_wand()
    {
        var state = new KeukenStateService();
        var bronWand = MaakWand("Bron");
        var doelWand = MaakWand("Doel");
        state.VoegWandToe(bronWand);
        state.VoegWandToe(doelWand);

        var kast = MaakKast("Onderkast");
        state.VoegKastToe(kast, bronWand.Id);

        var notificaties = 0;
        state.OnStateChanged += () => notificaties++;

        var gewijzigd = state.WerkKastBijOpWand(new Kast
        {
            Id = kast.Id,
            Naam = "Onderkast verplaatst",
            Type = kast.Type,
            Breedte = kast.Breedte,
            Hoogte = kast.Hoogte,
            Diepte = kast.Diepte,
            Wanddikte = kast.Wanddikte,
            GaatjesAfstand = kast.GaatjesAfstand,
            EersteGaatVanBoven = kast.EersteGaatVanBoven,
            HoogteVanVloer = kast.HoogteVanVloer,
            XPositie = kast.XPositie
        }, doelWand.Id);

        Assert.True(gewijzigd);
        Assert.Equal(1, notificaties);
        Assert.DoesNotContain(kast.Id, bronWand.KastIds);
        Assert.Contains(kast.Id, doelWand.KastIds);
        Assert.Equal(doelWand.Id, state.WandVoorKast(kast.Id)?.Id);
        Assert.Equal("Onderkast verplaatst", Assert.Single(state.Kasten).Naam);
    }

    [Fact]
    public void Laden_gebruikt_dezelfde_domeinvalidatie_als_importpaden()
    {
        var state = new KeukenStateService();

        state.Laden(new KeukenData
        {
            Wanden =
            [
                new KeukenWand
                {
                    Naam = "  Achterwand  ",
                    Breedte = -10,
                    Hoogte = 0
                }
            ],
            Kasten =
            [
                new Kast
                {
                    Naam = "  Kast  ",
                    Breedte = -10,
                    Hoogte = 0,
                    Diepte = -20,
                    XPositie = -5
                }
            ],
            Apparaten =
            [
                new Apparaat
                {
                    Naam = "  Oven  ",
                    Type = (ApparaatType)99,
                    Breedte = -1,
                    Hoogte = -1,
                    Diepte = -1,
                    XPositie = -10
                }
            ],
            Toewijzingen =
            [
                new PaneelToewijzing
                {
                    Type = (PaneelType)99,
                    ScharnierZijde = (ScharnierZijde)99,
                    PotHartVanRand = 5,
                    Breedte = -1,
                    Hoogte = 0,
                    XPositie = -10
                }
            ]
        });

        Assert.Equal("Achterwand", Assert.Single(state.Wanden).Naam);
        Assert.Equal(600, Assert.Single(state.Kasten).Breedte);
        Assert.Equal(ApparaatType.Oven, Assert.Single(state.Apparaten).Type);
        Assert.Equal(PaneelType.Deur, Assert.Single(state.Toewijzingen).Type);
        Assert.Equal(1, state.Toewijzingen[0].Breedte);
    }

    private static KeukenWand MaakWand(string naam) => new()
    {
        Naam = naam,
        Breedte = 2400,
        Hoogte = 2700,
        PlintHoogte = 100
    };

    private static Kast MaakKast(string naam) => new()
    {
        Naam = naam,
        Type = KastType.Onderkast,
        Breedte = 600,
        Hoogte = 720,
        Diepte = 560,
        Wanddikte = 18,
        GaatjesAfstand = 32,
        EersteGaatVanBoven = 19
    };

    private static Kast MaakGeplaatsteKast(string naam, double xPositie, double hoogteVanVloer = 100, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Naam = naam,
        Type = KastType.Onderkast,
        Breedte = 600,
        Hoogte = 720,
        Diepte = 560,
        Wanddikte = 18,
        GaatjesAfstand = 32,
        EersteGaatVanBoven = 19,
        XPositie = xPositie,
        HoogteVanVloer = hoogteVanVloer
    };

    private static Apparaat MaakApparaat(
        string naam,
        Guid? id = null,
        double xPositie = 600,
        double hoogteVanVloer = 100,
        double breedte = 600,
        double hoogte = 600,
        ApparaatType type = ApparaatType.Oven) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Naam = naam,
        Type = type,
        Breedte = breedte,
        Hoogte = hoogte,
        Diepte = 560,
        XPositie = xPositie,
        HoogteVanVloer = hoogteVanVloer
    };
}
