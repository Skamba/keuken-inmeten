using Keuken_inmeten.Models;
using Keuken_inmeten.Services;
using Xunit;

namespace Keuken_inmeten.Tests;

public class HandBerekendTests
{
    private static Kast MaakKast(double hoogte, double breedte = 600, double wanddikte = 18,
        double gaatjesAfstand = 32, double eersteGaatVanBoven = 19, double vloer = 0,
        KastType? type = null, double x = 0) => new()
    {
        Hoogte = hoogte, Breedte = breedte, Wanddikte = wanddikte,
        GaatjesAfstand = gaatjesAfstand, EersteGaatVanBoven = eersteGaatVanBoven,
        HoogteVanVloer = vloer, XPositie = x,
        Type = type ?? (hoogte <= 500 ? KastType.Bovenkast : hoogte >= 1100 ? KastType.HogeKast : KastType.Onderkast)
    };

    // ═══════════════════════════════════════════════════════════════════
    // 1. GaatjesRijPosities — 720mm kast, default settings
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test01_GaatjesRij_720mm_Default()
    {
        // EersteGaatPositieVanafBoven = 18 + 19 = 37
        // GaatjesAfstand = 32, boundary: pos < 720 - 5 = 715
        // Holes: 37, 37+32=69, 69+32=101, ..., 37+21×32 = 709
        // 709 < 715 ✓, next: 741 ≥ 715 ✗
        // Count = 22
        var posities = ScharnierBerekeningService.GaatjesRijPosities(720, 37, 32);

        var verwacht = new double[]
        {
             37,  69, 101, 133, 165, 197, 229, 261, 293, 325,
            357, 389, 421, 453, 485, 517, 549, 581, 613, 645,
            677, 709
        };

        Assert.Equal(22, posities.Count);
        Assert.Equal(verwacht, posities);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. GaatjesRijPosities — 400mm bovenkast, default settings
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test02_GaatjesRij_400mm_Bovenkast()
    {
        // Boundary: pos < 400 - 5 = 395
        // Holes: 37, 69, 101, 133, 165, 197, 229, 261, 293, 325, 357, 389
        // 389 < 395 ✓, 421 ≥ 395 ✗
        // Count = 12
        var posities = ScharnierBerekeningService.GaatjesRijPosities(400, 37, 32);

        var verwacht = new double[]
        {
            37, 69, 101, 133, 165, 197, 229, 261, 293, 325, 357, 389
        };

        Assert.Equal(12, posities.Count);
        Assert.Equal(verwacht, posities);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. GaatjesRijPosities — custom: wanddikte=25, eersteGaatVanBoven=15
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test03_GaatjesRij_CustomSettings()
    {
        // EersteGaatPositieVanafBoven = 25 + 15 = 40
        // GaatjesAfstand = 32, hoogte = 720, boundary: pos < 715
        // Holes: 40, 72, 104, 136, 168, 200, 232, 264, 296, 328,
        //        360, 392, 424, 456, 488, 520, 552, 584, 616, 648,
        //        680, 712
        // 40 + 21×32 = 712 < 715 ✓, 744 ≥ 715 ✗
        // Count = 22
        var posities = ScharnierBerekeningService.GaatjesRijPosities(720, 40, 32);

        var verwacht = new double[]
        {
             40,  72, 104, 136, 168, 200, 232, 264, 296, 328,
            360, 392, 424, 456, 488, 520, 552, 584, 616, 648,
            680, 712
        };

        Assert.Equal(22, posities.Count);
        Assert.Equal(verwacht, posities);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. MontagePlaatCentra — 720mm kast, default settings
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test04_MontagePlaatCentra_720mm_Default()
    {
        // From test 1: 22 holes at 37, 69, 101, ..., 709
        // Centers = midpoints of consecutive holes:
        // (37+69)/2=53, (69+101)/2=85, ..., (677+709)/2=693
        // Count = 21
        var centra = ScharnierBerekeningService.MontagePlaatCentra(720, 37, 32);

        var verwacht = new double[]
        {
             53,  85, 117, 149, 181, 213, 245, 277, 309, 341,
            373, 405, 437, 469, 501, 533, 565, 597, 629, 661, 693
        };

        Assert.Equal(21, centra.Count);
        Assert.Equal(verwacht, centra);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. MontagePlaatCentra — very small kast (200mm), default
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test05_MontagePlaatCentra_200mm_KleinKast()
    {
        // Boundary: pos < 200 - 5 = 195
        // Holes: 37, 69, 101, 133, 165  (165 < 195 ✓, 197 ≥ 195 ✗)
        // Count = 5 holes
        // Centers: (37+69)/2=53, (69+101)/2=85, (101+133)/2=117, (133+165)/2=149
        // Count = 4
        var centra = ScharnierBerekeningService.MontagePlaatCentra(200, 37, 32);

        Assert.Equal(4, centra.Count);
        Assert.Equal(new double[] { 53, 85, 117, 149 }, centra);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. AantalScharnieren — exact boundary values
    // ═══════════════════════════════════════════════════════════════════
    [Theory]
    [InlineData(999,  2)]  // ≤ 1000 → 2
    [InlineData(1000, 2)]  // ≤ 1000 → 2
    [InlineData(1001, 3)]  // > 1000, ≤ 1500 → 3
    [InlineData(1500, 3)]  // ≤ 1500 → 3
    [InlineData(1501, 4)]  // > 1500, ≤ 2200 → 4
    [InlineData(2200, 4)]  // ≤ 2200 → 4
    [InlineData(2201, 5)]  // > 2200 → 5
    public void Test06_AantalScharnieren_Grenswaarden(double hoogte, int verwacht)
    {
        Assert.Equal(verwacht, ScharnierBerekeningService.AantalScharnieren(hoogte));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 7. BerekenStandaardPosities — 720mm (2 hinges), default
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test07_StandaardPosities_720mm_2Scharnieren()
    {
        // Centers: 53, 85, 117, 149, 181, 213, 245, 277, 309, 341,
        //          373, 405, 437, 469, 501, 533, 565, 597, 629, 661, 693
        // N=2, tussenruimte = (720 - 2×80) / max(1,1) = 560
        // ideaal[0] = 80  → |80-53|=27, |80-85|=5   → snap to 85
        // ideaal[1] = 640 → |640-629|=11, |640-661|=21 → snap to 629
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(720, 37, 32);

        Assert.Equal(2, posities.Count);
        Assert.Equal(85.0, posities[0]);
        Assert.Equal(629.0, posities[1]);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 8. BerekenStandaardPosities — 1200mm (3 hinges), default
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test08_StandaardPosities_1200mm_3Scharnieren()
    {
        // Holes: 37+k×32 < 1195 → last = 37 + 36×32 = 1189, count 37
        // Centers: 53, 85, ..., 1173 → count 36
        // N=3, tussenruimte = (1200 - 160) / 2 = 520
        // ideaal[0] = 80   → |80-85|=5                       → 85
        // ideaal[1] = 600  → |600-597|=3, |600-629|=29       → 597
        // ideaal[2] = 1120 → |1120-1109|=11, |1120-1141|=21  → 1109
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(1200, 37, 32);

        Assert.Equal(3, posities.Count);
        Assert.Equal(85.0, posities[0]);
        Assert.Equal(597.0, posities[1]);
        Assert.Equal(1109.0, posities[2]);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 9. BerekenStandaardPosities — 1800mm (4 hinges), default
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test09_StandaardPosities_1800mm_4Scharnieren()
    {
        // Holes: 37+k×32 < 1795 → last = 37 + 54×32 = 1765, count 55
        // Centers: 53+k×32 for k=0..53 → last = 53 + 53×32 = 1749, count 54
        // N=4, tussenruimte = (1800 - 160) / 3 = 546⅔
        // ideaal[0] = 80        → |80-85|=5                              → 85
        // ideaal[1] = 626.667   → |626.667-629|=2.333, |626.667-597|=29.667  → 629
        // ideaal[2] = 1173.333  → |1173.333-1173|=0.333, |1173.333-1205|=31.667  → 1173
        // ideaal[3] = 1720      → |1720-1717|=3, |1720-1749|=29          → 1717
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(1800, 37, 32);

        Assert.Equal(4, posities.Count);
        Assert.Equal(85.0, posities[0]);
        Assert.Equal(629.0, posities[1]);
        Assert.Equal(1173.0, posities[2]);
        Assert.Equal(1717.0, posities[3]);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 10. BerekenStandaardPosities fallback — 720mm, no hole-row
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test10_StandaardPosities_Fallback_720mm()
    {
        // Overload without hole-row params → returns ideal positions directly
        // N=2, tussenruimte = (720 - 160) / 1 = 560
        // ideaal[0] = 80, ideaal[1] = 80 + 560 = 640
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(720);

        Assert.Equal(2, posities.Count);
        Assert.Equal(80.0, posities[0]);
        Assert.Equal(640.0, posities[1]);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 11. BerekenStandaardPosities fallback — 1500mm (3 hinges)
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test11_StandaardPosities_Fallback_1500mm()
    {
        // No hole-row → ideal positions directly
        // N=3, tussenruimte = (1500 - 160) / 2 = 670
        // ideaal[0] = 80
        // ideaal[1] = 80 + 670 = 750
        // ideaal[2] = 80 + 1340 = 1420
        var posities = ScharnierBerekeningService.BerekenStandaardPosities(1500);

        Assert.Equal(3, posities.Count);
        Assert.Equal(80.0, posities[0]);
        Assert.Equal(750.0, posities[1]);
        Assert.Equal(1420.0, posities[2]);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 12. BerekenPaneelAfmetingen — single kast 600×720
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test12_PaneelAfmetingen_EnkelKast()
    {
        // Single kast: width = kast.Breedte = 600, height = kast.Hoogte = 720
        var kast = MaakKast(720, breedte: 600);
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen([kast]);

        Assert.Equal(600.0, b);
        Assert.Equal(720.0, h);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 13. BerekenPaneelAfmetingen — two side-by-side kasten
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test13_PaneelAfmetingen_NaastElkaar()
    {
        // Kast 1: 400×720 at x=0, floor=0
        // Kast 2: 500×720 at x=400, floor=0
        // Same floor height → side by side
        // Width = 400 + 500 = 900, Height = max(720, 720) = 720
        var kasten = new List<Kast>
        {
            MaakKast(720, breedte: 400, x: 0),
            MaakKast(720, breedte: 500, x: 400)
        };
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);

        Assert.Equal(900.0, b);
        Assert.Equal(720.0, h);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 14. BerekenPaneelAfmetingen — two stacked kasten
    //     (bovenkast 300mm + onderkast 720mm, same width)
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test14_PaneelAfmetingen_Gestapeld()
    {
        // Bovenkast (300) + Onderkast (720), both 600mm wide, both unpositioned
        // Fallback: one is Bovenkast and same width → stacked
        // Width = max(600, 600) = 600
        // Height = 300 + 720 = 1020
        var kasten = new List<Kast>
        {
            MaakKast(720, type: KastType.Onderkast),
            MaakKast(300, type: KastType.Bovenkast)
        };
        var (b, h) = ScharnierBerekeningService.BerekenPaneelAfmetingen(kasten);

        Assert.Equal(600.0, b);
        Assert.Equal(1020.0, h);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 15. IsVertikaalGestapeld — two onderkasts same position → false
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test15_NietGestapeld_TweeOnderkasts()
    {
        // Two onderkasts, same width, same position, same floor height
        // Primary: same floor height → NOT stacked
        // Fallback: no Bovenkast → NOT stacked
        var kasten = new List<Kast>
        {
            MaakKast(720, type: KastType.Onderkast),
            MaakKast(720, type: KastType.Onderkast)
        };

        Assert.False(ScharnierBerekeningService.IsVertikaalGestapeld(kasten));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 16. IsVertikaalGestapeld — bovenkast + onderkast same width → true
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test16_Gestapeld_BovenkastPlusOnderkast()
    {
        // Bovenkast (400mm) + Onderkast (720mm), same width 600mm, unpositioned
        // Fallback: one is Bovenkast and same width → stacked
        var kasten = new List<Kast>
        {
            MaakKast(720, type: KastType.Onderkast),
            MaakKast(400, type: KastType.Bovenkast)
        };

        Assert.True(ScharnierBerekeningService.IsVertikaalGestapeld(kasten));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 17. BerekenPaneelScharnierPosities — single 720mm kast (2 hinges)
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test17_PaneelPosities_720mm_2Scharnieren()
    {
        // Same calculation as test 7:
        // Centers: 53, 85, ..., 693
        // N=2, ideals: 80, 640 → snapped: 85, 629
        // 85 ≥ 80 ✓ (min 80mm from top)
        // 720 − 629 = 91 ≥ 80 ✓ (min 80mm from bottom)
        var kast = MaakKast(720);
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            [kast], paneelHoogte: 720, zijde: ScharnierZijde.Links);

        Assert.Equal(2, posities.Count);
        Assert.Equal(85.0, posities[0]);
        Assert.Equal(629.0, posities[1]);
        Assert.True(posities[0] >= 80, $"Top hinge {posities[0]}mm < 80mm from top");
        Assert.True(720 - posities[^1] >= 80, $"Bottom hinge {posities[^1]}mm too close to bottom");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 18. BerekenPaneelScharnierPosities — single 1200mm kast (3 hinges)
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test18_PaneelPosities_1200mm_3Scharnieren()
    {
        // From test 8: N=3, tussenruimte = 520
        // ideals: 80, 600, 1120 → snapped: 85, 597, 1109
        var kast = MaakKast(1200, type: KastType.HogeKast);
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            [kast], paneelHoogte: 1200, zijde: ScharnierZijde.Links);

        Assert.Equal(3, posities.Count);
        Assert.Equal(85.0, posities[0]);
        Assert.Equal(597.0, posities[1]);
        Assert.Equal(1109.0, posities[2]);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 19. BerekenPaneelScharnierPosities — 2500mm HogeKast (5 hinges)
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test19_PaneelPosities_2500mm_5Scharnieren()
    {
        // Holes: 37+k×32 < 2495 → last = 37+76×32 = 2469, count 77
        // Centers: 53+k×32 for k=0..75 → last = 2453, count 76
        // N=5, tussenruimte = (2500 − 160) / 4 = 585
        // ideaal[0] = 80   → |80-85|=5     → 85
        // ideaal[1] = 665  → |665-661|=4, |665-693|=28   → 661
        // ideaal[2] = 1250 → |1250-1237|=13, |1250-1269|=19  → 1237
        // ideaal[3] = 1835 → |1835-1813|=22, |1835-1845|=10  → 1845
        // ideaal[4] = 2420 → |2420-2421|=1 but 2500-2421=79 < 80 → REJECTED
        //                     |2420-2389|=31 and 2500-2389=111 ≥ 80 → 2389
        var kast = MaakKast(2500, type: KastType.HogeKast);
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            [kast], paneelHoogte: 2500, zijde: ScharnierZijde.Links);

        Assert.Equal(5, posities.Count);
        Assert.Equal(85.0, posities[0]);
        Assert.Equal(661.0, posities[1]);
        Assert.Equal(1237.0, posities[2]);
        Assert.Equal(1845.0, posities[3]);
        Assert.Equal(2389.0, posities[4]);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 20. BerekenPaneelScharnierPosities — stacked, junction avoidance
    //     Bovenkast (300mm) on top + HogeKast (1900mm) on bottom
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test20_PaneelPosities_Gestapeld_NaadVermijding()
    {
        // Setup: Bovenkast 300mm (vloer=1900) + HogeKast 1900mm (vloer=0)
        // Panel height = 300 + 1900 = 2200mm
        //
        // Bovenkast segment (top, 0-300mm from panel top):
        //   Holes: 37, 69, ..., 293 (< 295), 9 holes
        //   Centers: 53, 85, 117, 149, 181, 213, 245, 277
        //
        // HogeKast segment (bottom, 300-2200mm from panel top):
        //   Holes relative to panel: 337, 369, ..., 2193, 59 holes
        //   Centers relative to panel: 353, 385, ..., 2177
        //
        // Junction at 300mm from top. Avoidance zone [250, 350]:
        //   Center 277 → |300-277|=23 < 50 → EXCLUDED
        //   Center 353 → |353-300|=53 ≥ 50 → OK
        //
        // N=4 (2200 ≤ 2200), tussenruimte = (2200-160)/3 = 680
        // ideals: 80, 760, 1440, 2120
        //
        // Expected: [85, 769, 1441, 2113] — all outside [250, 350]
        var kasten = new List<Kast>
        {
            MaakKast(1900, vloer: 0, type: KastType.HogeKast),
            MaakKast(300, vloer: 1900, type: KastType.Bovenkast)
        };
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            kasten, paneelHoogte: 2200, zijde: ScharnierZijde.Links);

        // 2200mm → 4 hinges
        Assert.Equal(4, posities.Count);

        // No hinge within 50mm of junction at 300mm from top
        const double naad = 300.0;
        const double zone = 50.0;
        foreach (var p in posities)
            Assert.False(Math.Abs(p - naad) < zone,
                $"Hinge at {p}mm is within {zone}mm of the junction at {naad}mm");

        // Top and bottom edge constraints
        Assert.True(posities[0] >= 80, $"Top hinge {posities[0]}mm < 80mm from top");
        Assert.True(posities[^1] <= 2200 - 80 + 32,
            $"Bottom hinge {posities[^1]}mm too far from bottom edge");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 21. BerekenPaneel — LadeFront → 0 boorgaten
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test21_BerekenPaneel_LadeFront_GeenBoorgaten()
    {
        // A LadeFront (drawer front) has no hinges → no drill holes
        var kast = MaakKast(200);
        var toewijzing = new PaneelToewijzing
        {
            Type = PaneelType.LadeFront,
            Breedte = 600,
            Hoogte = 200,
            ScharnierZijde = ScharnierZijde.Links,
            KastIds = [kast.Id]
        };

        var resultaat = ScharnierBerekeningService.BerekenPaneel(toewijzing, [kast]);

        Assert.Empty(resultaat.Boorgaten);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 22. BerekenPaneel — BlindPaneel → 0 boorgaten
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test22_BerekenPaneel_BlindPaneel_GeenBoorgaten()
    {
        // A BlindPaneel (blind panel) has no hinges → no drill holes
        var kast = MaakKast(720);
        var toewijzing = new PaneelToewijzing
        {
            Type = PaneelType.BlindPaneel,
            Breedte = 600,
            Hoogte = 720,
            ScharnierZijde = ScharnierZijde.Links,
            KastIds = [kast.Id]
        };

        var resultaat = ScharnierBerekeningService.BerekenPaneel(toewijzing, [kast]);

        Assert.Empty(resultaat.Boorgaten);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 23. BerekenPaneel — Deur 720mm → 2 boorgaten with standaard X
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test23_BerekenPaneel_Deur_720mm()
    {
        // 720mm door → 2 hinges
        // Y positions snapped to centers: 85, 629 (from test 7)
        // X = standaard cup center from panel edge, ScharnierZijde.Links
        // Diameter = 35mm (European 35mm cup standard)
        var kast = MaakKast(720);
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
        Assert.All(resultaat.Boorgaten, g => Assert.Equal(ScharnierBerekeningService.CupCenterVanRand, g.X));
        Assert.All(resultaat.Boorgaten, g => Assert.Equal(35.0, g.Diameter));
        Assert.Equal(85.0, resultaat.Boorgaten[0].Y);
        Assert.Equal(629.0, resultaat.Boorgaten[1].Y);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 26. BerekenPaneel — Deur met aangepaste pot-hart afstand
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test26_BerekenPaneel_Deur_GebruiktAangepastePotHartAfstand()
    {
        var kast = MaakKast(720);
        var toewijzing = new PaneelToewijzing
        {
            Type = PaneelType.Deur,
            Breedte = 600,
            Hoogte = 720,
            ScharnierZijde = ScharnierZijde.Links,
            PotHartVanRand = 24.5,
            KastIds = [kast.Id]
        };

        var resultaat = ScharnierBerekeningService.BerekenPaneel(toewijzing, [kast]);

        Assert.Equal(2, resultaat.Boorgaten.Count);
        Assert.All(resultaat.Boorgaten, g => Assert.Equal(24.5, g.X));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 24. BerekenPaneelScharnierPosities — plank avoidance
    //     Kast with a plank at 350mm from bottom
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test24_PaneelPosities_PlankVermijding()
    {
        // 720mm kast with Plank at HoogteVanBodem = 350mm (from inside bottom)
        // Inside bottom at Hoogte - Wanddikte = 702mm from top
        // Plank from top = 702 - 350 = 352mm  (or ≈ 370 using Hoogte-HoogteVanBodem)
        //
        // 2 hinges, ideals: 80, 640 → snapped to 85, 629
        // Both positions are far from plank area (~350-370), so avoidance is satisfied
        // Avoidance constraint: no hinge within 30mm of plank
        var kast = MaakKast(720);
        kast.Planken = [new Plank { HoogteVanBodem = 350 }];

        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            [kast], paneelHoogte: 720, zijde: ScharnierZijde.Links);

        Assert.Equal(2, posities.Count);

        // Verify no position within 30mm of plank (whether plank is at 352 or 370 from top)
        foreach (var p in posities)
        {
            Assert.True(Math.Abs(p - 352) >= 30,
                $"Hinge at {p}mm is within 30mm of plank at 352mm from top");
            Assert.True(Math.Abs(p - 370) >= 30,
                $"Hinge at {p}mm is within 30mm of plank at 370mm from top");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // 25. BerekenPaneelScharnierPosities — custom hole spacing
    //     Wanddikte=22, EersteGaatVanBoven=17, GaatjesAfstand=37
    // ═══════════════════════════════════════════════════════════════════
    [Fact]
    public void Test25_PaneelPosities_CustomGaatjesRij()
    {
        // EersteGaatPositieVanafBoven = 22 + 17 = 39
        // GaatjesAfstand = 37, hoogte = 720, boundary: pos < 715
        //
        // Holes: 39, 76, 113, 150, 187, 224, 261, 298, 335, 372,
        //        409, 446, 483, 520, 557, 594, 631, 668, 705
        //        (39 + 18×37 = 705 < 715 ✓, 742 ≥ 715 ✗)
        //        Count = 19
        //
        // Centers (midpoints of consecutive holes):
        //   57.5, 94.5, 131.5, 168.5, 205.5, 242.5, 279.5, 316.5, 353.5,
        //   390.5, 427.5, 464.5, 501.5, 538.5, 575.5, 612.5, 649.5, 686.5
        //   Count = 18
        //
        // N=2 (720 ≤ 1000), tussenruimte = (720-160)/1 = 560
        // ideaal[0] = 80  → |80-57.5|=22.5, |80-94.5|=14.5  → 94.5 (94.5 ≥ 80 ✓)
        // ideaal[1] = 640 → |640-649.5|=9.5 but 720-649.5=70.5 < 80 → REJECTED
        //                   |640-612.5|=27.5 and 720-612.5=107.5 ≥ 80 → 612.5
        var kast = MaakKast(720, wanddikte: 22, gaatjesAfstand: 37, eersteGaatVanBoven: 17);
        var posities = ScharnierBerekeningService.BerekenPaneelScharnierPosities(
            [kast], paneelHoogte: 720, zijde: ScharnierZijde.Links);

        Assert.Equal(2, posities.Count);
        Assert.Equal(94.5, posities[0]);
        Assert.Equal(612.5, posities[1]);
    }
}
