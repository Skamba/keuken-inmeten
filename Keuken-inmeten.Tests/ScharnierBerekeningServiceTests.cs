using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class BerekenPaneelAfmetingenTests
{
    // ── single cabinet ─────────────────────────────────────────────────

    [Fact]
    public void Enkel_kast_geeft_eigen_afmetingen()
    {
        var kasten = new List<Kast> { new() { Breedte = 600, Hoogte = 720 } };
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);
        Assert.Equal(600, b);
        Assert.Equal(720, h);
    }

    // ── side-by-side ───────────────────────────────────────────────────

    [Fact]
    public void Naast_elkaar_geplaatste_kasten_sommeren_breedte()
    {
        // Two onderkasts side by side: x=0 and x=600, same floor height=0
        var kasten = new List<Kast>
        {
            new() { Breedte = 600, Hoogte = 720, XPositie = 0,   HoogteVanVloer = 0 },
            new() { Breedte = 450, Hoogte = 720, XPositie = 600, HoogteVanVloer = 0 },
        };
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);
        Assert.Equal(1050, b); // 600 + 450
        Assert.Equal(720, h);  // max
    }

    [Fact]
    public void Naast_elkaar_is_niet_verticaal_gestapeld()
    {
        var kasten = new List<Kast>
        {
            new() { Breedte = 600, Hoogte = 720, XPositie = 0,   HoogteVanVloer = 0 },
            new() { Breedte = 600, Hoogte = 720, XPositie = 600, HoogteVanVloer = 0 },
        };
        Assert.False(ScharnierBerekeningService.IsVertikaalGestapeld(kasten));
    }

    // ── vertically stacked (positioned) ────────────────────────────────

    [Fact]
    public void Gestapeld_via_positie_sommeren_hoogte()
    {
        // Rechtsonder (600×1900) at x=2000, floor=0; Boven (600×300) at x=2000, floor=1900
        var kasten = new List<Kast>
        {
            new() { Breedte = 600, Hoogte = 1900, XPositie = 2000, HoogteVanVloer = 0,    Type = KastType.HogeKast },
            new() { Breedte = 600, Hoogte = 300,  XPositie = 2000, HoogteVanVloer = 1900, Type = KastType.Bovenkast },
        };
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);
        Assert.Equal(600, b);  // max breedte
        Assert.Equal(2200, h); // 1900 + 300
    }

    [Fact]
    public void Gestapeld_met_gedeeltelijk_overlappende_x_sommeren_hoogte()
    {
        // Cabinets with slightly different X but still overlapping (e.g. x=2000 vs x=2400 with breedte=600)
        var kasten = new List<Kast>
        {
            new() { Breedte = 600, Hoogte = 1900, XPositie = 2000, HoogteVanVloer = 0,    Type = KastType.HogeKast  },
            new() { Breedte = 600, Hoogte = 300,  XPositie = 2400, HoogteVanVloer = 1900, Type = KastType.Bovenkast },
        };
        // X ranges [2000..2600] and [2400..3000] overlap at [2400..2600] → stacked
        Assert.True(ScharnierBerekeningService.IsVertikaalGestapeld(kasten));
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);
        Assert.Equal(2200, h);
    }

    // ── fallback: unpositioned Bovenkast + same-width ──────────────────

    [Fact]
    public void Bovenkast_plus_onderkast_zelfde_breedte_niet_gepositioneerd_is_gestapeld()
    {
        // Both at default XPositie=0, HoogteVanVloer=0 (not yet dragged to position)
        var kasten = new List<Kast>
        {
            new() { Breedte = 600, Hoogte = 1900, XPositie = 0, HoogteVanVloer = 0, Type = KastType.HogeKast  },
            new() { Breedte = 600, Hoogte = 300,  XPositie = 0, HoogteVanVloer = 0, Type = KastType.Bovenkast },
        };
        Assert.True(ScharnierBerekeningService.IsVertikaalGestapeld(kasten));
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);
        Assert.Equal(600,  b);
        Assert.Equal(2200, h);
    }

    [Fact]
    public void Twee_onderkasts_zelfde_breedte_niet_gepositioneerd_is_naast_elkaar()
    {
        var kasten = new List<Kast>
        {
            new() { Breedte = 600, Hoogte = 720, XPositie = 0, HoogteVanVloer = 0, Type = KastType.Onderkast },
            new() { Breedte = 600, Hoogte = 720, XPositie = 0, HoogteVanVloer = 0, Type = KastType.Onderkast },
        };
        Assert.False(ScharnierBerekeningService.IsVertikaalGestapeld(kasten));
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);
        Assert.Equal(1200, b); // 600 + 600
        Assert.Equal(720,  h);
    }
}

public class AantalScharnierenTests
{
    [Theory]
    [InlineData(700,  2)]
    [InlineData(1000, 2)]
    [InlineData(1001, 3)]
    [InlineData(1500, 3)]
    [InlineData(1501, 4)]
    [InlineData(2200, 4)]
    [InlineData(2201, 5)]
    public void Klopt_voor_standaard_hoogtes(double hoogte, int verwacht)
    {
        Assert.Equal(verwacht, ScharnierBerekeningService.AantalScharnieren(hoogte));
    }
}

public class KastMeetwijzeTests
{
    [Fact]
    public void Eerste_gat_vanaf_onderkant_bovenplank_wordt_omgezet_naar_van_boven()
    {
        var kast = new Kast
        {
            Wanddikte = 18,
            EersteGaatVanBoven = 30
        };

        Assert.Equal(48, kast.EersteGaatPositieVanafBoven);
    }
}

public class BerekenStandaardPositiesTests
{
    [Fact]
    public void Posities_gesnapt_aan_gaatjesrij_midpoints_720mm()
    {
        // Holes: 37, 69, 101, ...; midpoints: 53, 85, 117, ...
        // For 720mm kast with 2 hinges at ±80mm margin:
        // ideal positions: 80 and 640mm → nearest midpoints: 85 and 629
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(720, eersteGaatVanBoven: 37, gaatjesAfstand: 32);
        Assert.Equal(2, posities.Count);
        Assert.Equal(85.0, posities[0]);
        Assert.Equal(629.0, posities[1]);
    }

    [Fact]
    public void Posities_zijn_gesorteerd_van_boven_naar_beneden()
    {
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(1200, 37, 32);
        Assert.Equal(posities.OrderBy(p => p).ToList(), posities);
    }

    [Fact]
    public void Fallback_zonder_gaatjesrij_geeft_vrije_posities()
    {
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(720);
        Assert.Equal(2, posities.Count);
        Assert.Equal(80.0, posities[0]);
        Assert.Equal(640.0, posities[1]);
    }
}

public class BerekenPaneelScharnierPositiesTests
{
    private static Kast MaakKast(double hoogte, double vloer = 0, KastType type = KastType.HogeKast) => new()
    {
        Hoogte = hoogte, HoogteVanVloer = vloer, Breedte = 600, Type = type,
        Wanddikte = 18, GaatjesAfstand = 32, EersteGaatVanBoven = 19
    };

    private static List<Kast> GestapeldeKasten() =>
    [
        MaakKast(1900, vloer: 0,    type: KastType.HogeKast),
        MaakKast(300,  vloer: 1900, type: KastType.Bovenkast),
    ];

    [Fact]
    public void Onderste_scharnier_maximaal_MinAfstand_van_onderkant_paneel()
    {
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            GestapeldeKasten(), paneelHoogte: 2200, zijde: ScharnierZijde.Links);

        Assert.True(posities.Count >= 2);
        // Bottom hinge must be within MinAfstandVanRand (80) + 1 gaatjes-step (32) of bottom
        Assert.True(posities[^1] >= 2200 - 80 - 32,
            $"Bottom hinge at {posities[^1]}mm is too far from the bottom of the 2200mm panel");
    }

    [Fact]
    public void Scharnieren_liggen_niet_bij_naad_tussen_kasten()
    {
        // Naad from top of panel = 300mm (upper kast height when stacked)
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            GestapeldeKasten(), paneelHoogte: 2200, zijde: ScharnierZijde.Links);

        const double naad = 300.0;
        const double zone = 50.0;
        foreach (var p in posities)
            Assert.False(Math.Abs(p - naad) < zone,
                $"Hinge at {p}mm is within {zone}mm of the naad at {naad}mm");
    }

    [Fact]
    public void Enkel_kast_posities_zijn_goed_verdeeld()
    {
        var kasten = new List<Kast> { MaakKast(720) };
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            kasten, paneelHoogte: 720, zijde: ScharnierZijde.Links);
        Assert.Equal(2, posities.Count);
        Assert.True(posities[0] >= 80, $"Top hinge {posities[0]} < 80");
        Assert.True(posities[^1] <= 640, $"Bottom hinge {posities[^1]} > 640");
    }
}

public class BerekenPaneelOnderbouwingTests
{
    [Fact]
    public void Enkel_kast_geeft_gatenrij_en_kastnaam_mee()
    {
        var kast = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Onderkast links",
            Breedte = 600,
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };

        var toewijzing = new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 720,
            ScharnierZijde = ScharnierZijde.Links,
            KastIds = [kast.Id]
        };

        var resultaat = ScharnierBerekeningService.BerekenPaneel(toewijzing, [kast]);

        Assert.Equal(2, resultaat.Boorgaten.Count);
        var eerste = Assert.Single(resultaat.Boorgaten, g => g.Y == 85.0);
        Assert.NotNull(eerste.Onderbouwing);
        Assert.Equal("Onderkast links", eerste.Onderbouwing!.KastNaam);
        Assert.Equal(2, eerste.Onderbouwing.GaatBovenIndex);
        Assert.Equal(3, eerste.Onderbouwing.GaatOnderIndex);
        Assert.Equal(69.0, eerste.Onderbouwing.GaatBovenY);
        Assert.Equal(101.0, eerste.Onderbouwing.GaatOnderY);
        Assert.True(eerste.Onderbouwing.AfstandTotBoven >= ScharnierBerekeningService.MinAfstandVanRand);
    }

    [Fact]
    public void Gestapeld_paneel_verdeelt_onderbouwing_over_de_juiste_kasten()
    {
        var onder = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Hoge kast",
            Breedte = 600,
            Hoogte = 1900,
            HoogteVanVloer = 0,
            Type = KastType.HogeKast,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };
        var boven = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Bovenkast",
            Breedte = 600,
            Hoogte = 300,
            HoogteVanVloer = 1900,
            Type = KastType.Bovenkast,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };

        var toewijzing = new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 2200,
            ScharnierZijde = ScharnierZijde.Rechts,
            KastIds = [onder.Id, boven.Id]
        };

        var resultaat = ScharnierBerekeningService.BerekenPaneel(toewijzing, [onder, boven]);

        Assert.Equal(4, resultaat.Boorgaten.Count);
        Assert.Equal("Bovenkast", resultaat.Boorgaten[0].Onderbouwing!.KastNaam);
        Assert.Contains(resultaat.Boorgaten.Skip(1), g => g.Onderbouwing!.KastNaam == "Hoge kast");
        Assert.All(resultaat.Boorgaten, g => Assert.NotNull(g.Onderbouwing!.AfstandTotDichtstbijzijndeNaad));
    }

    [Fact]
    public void Expliciet_geplaatst_deelpaneel_geeft_boorgaten_binnen_het_geselecteerde_segment()
    {
        var kast = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Hoge kast",
            Breedte = 600,
            Hoogte = 2600,
            HoogteVanVloer = 0,
            Type = KastType.HogeKast,
            Wanddikte = 18,
            GaatjesAfstand = 32,
            EersteGaatVanBoven = 19
        };

        var toewijzing = new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 900,
            XPositie = 0,
            HoogteVanVloer = 700,
            ScharnierZijde = ScharnierZijde.Links,
            KastIds = [kast.Id]
        };

        var resultaat = ScharnierBerekeningService.BerekenPaneel(toewijzing, [kast]);

        Assert.Equal(2, resultaat.Boorgaten.Count);
        Assert.Equal(109.0, resultaat.Boorgaten[0].Y);
        Assert.Equal(813.0, resultaat.Boorgaten[1].Y);
        Assert.All(resultaat.Boorgaten, gat => Assert.Equal("Hoge kast", gat.Onderbouwing!.KastNaam));
        Assert.All(resultaat.Boorgaten, gat =>
        {
            Assert.InRange(gat.Y, 0, toewijzing.Hoogte);
            Assert.InRange(gat.Onderbouwing!.MontagePlaatMiddenInKast, 1000, 1900);
        });
    }

    [Fact]
    public void Plank_op_systeemgat_blokkeert_scharnier_dat_dat_gat_gebruikt()
    {
        var kast = new Kast
        {
            Id = Guid.NewGuid(),
            Naam = "Onderkast",
            Breedte = 600,
            Hoogte = 720,
            Wanddikte = 18,
            GaatjesAfstand = 64,
            EersteGaatVanBoven = 19,
            Planken = [new Plank { HoogteVanBodem = 601 }]
        };

        var toewijzing = new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 720,
            ScharnierZijde = ScharnierZijde.Links,
            KastIds = [kast.Id]
        };

        var resultaat = ScharnierBerekeningService.BerekenPaneel(toewijzing, [kast]);

        Assert.Equal(2, resultaat.Boorgaten.Count);
        Assert.DoesNotContain(resultaat.Boorgaten, gat => gat.Y == 133.0);
        Assert.Equal(197.0, resultaat.Boorgaten[0].Y);
    }
}
