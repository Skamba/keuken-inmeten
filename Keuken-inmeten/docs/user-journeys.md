# Keuken Inmeten — User Journeys

## Journey 1: Standaard keuken inmeten (happy path)

**Persona:** Keukenmonteur die panelen moet bestellen voor een bestaande keuken.

1. **Home** → Leest de 3-stappen uitleg, klikt "Begin met inmeten"
2. **Stap 1 — Indeling**
   - Voegt wand "Achterwand" toe
   - Klikt "+ Kast toevoegen" bij de wand
   - Vult in: "Onderkast spoelbak", Onderkast, 800×720×560 mm
   - Klikt "Standaard berekenen" → scharnierposities worden automatisch ingevuld
   - Klikt "Kast toevoegen" → kast verschijnt in wandoverzicht + visueel
   - Herhaalt voor 3 andere onderkast kasten op dezelfde wand
   - Bekijkt de wandvisualisatie met 4 kasten naast elkaar
   - Verschuift een kast met ◀▶ knoppen als de volgorde niet klopt
   - Voegt wand "Linkerwand" toe met een hoge kast
   - Klikt "Volgende stap"
3. **Stap 2 — Panelen**
   - Selecteert kast uit dropdown (gegroepeerd per wand)
   - Kiest "Deur", scharnier links, afmetingen worden automatisch ingevuld
   - Klikt "Paneel toevoegen" → herhaalt voor elke kast
   - Klikt "Volgende stap"
4. **Stap 3 — Resultaat**
   - Ziet overzichtstabel met alle panelen per wand
   - Scrollt door detail-kaarten met exacte boorgat-posities en SVG-visualisaties
   - Klikt "Afdrukken" om het overzicht mee te nemen naar de werkplaats

---

## Journey 2: Keuken met gemengde kasthoogtes

**Persona:** Monteur met een keuken die hoge kasten naast onderkast kasten heeft.

1. Maakt wand "Achterwand" aan
2. Voegt onderkast (720 mm) en hoge kast (2100 mm) toe op dezelfde wand
3. Wandvisualisatie toont kasten met juiste schaalbreedte/hoogte naast elkaar
4. Klikt "Standaard berekenen" per kast:
   - Onderkast krijgt 2 scharnieren
   - Hoge kast krijgt 4 scharnieren
5. Bij panelen: wijs per kast de juiste paneeltype en scharnierzijde toe
6. Resultaat toont correcte boorgat-posities per paneel
7. Controleert dat hoge kast-panelen boorgaten op 80, 753.3, 1426.7, 2020 mm hebben

---

## Journey 3: Handmatig scharnierposities instellen

**Persona:** Ervaren monteur die weet dat de bestaande kast afwijkende montageplaatposities heeft.

1. Voegt kast toe met afmetingen
2. Klikt **niet** op "Standaard berekenen"
3. Klikt "+ Positie toevoegen" en voert handmatig de exacte mm-waarden in
4. Stelt per positie de juiste zijde in (Links/Rechts)
5. Ziet in de SVG-preview dat de rode bolletjes op de juiste plek staan
6. Past een waarde aan als het niet klopt
7. Verwijdert een overbodige positie met ×

---

## Journey 4: Keuken met meerdere wanden

**Persona:** Monteur voor een U-vormige keuken.

1. Maakt 3 wanden aan: "Linkerwand", "Achterwand", "Rechterwand"
2. Voegt per wand de kasten toe in de juiste volgorde van links naar rechts
3. Gebruikt ◀▶ om de volgorde te corrigeren als een kast verkeerd staat
4. Hernoemt een wand als de naam niet goed was
5. Bij panelen: dropdown groepeert kasten per wand met `<optgroup>`
6. Resultaat-tabel toont de wand-kolom zodat de monteur weet waar elk paneel hoort

---

## Journey 5: Fout corrigeren

**Persona:** Gebruiker die een invoerfout ontdekt op de resultaatpagina.

1. Op Stap 3 ziet dat een kast verkeerde afmetingen heeft
2. Klikt "← Terug naar kasten"
3. Klikt "Bewerken" bij de betreffende kast
4. Past de hoogte aan van 720 naar 700 mm
5. Klikt "Standaard berekenen" opnieuw → posities herberekend
6. Klikt "Opslaan"
7. Navigeert terug naar Stap 3 → resultaat is bijgewerkt

---

## Journey 6: Ladefront en blind paneel

**Persona:** Monteur die ook ladefronten en blinde panelen moet bestellen.

1. Voegt onderkast toe (heeft laden i.p.v. deur)
2. Bij panelen: selecteert "Ladefront" als type
3. Ladefronten hebben mogelijk andere/geen scharnierposities
4. Voegt ook "Blind paneel" toe voor plek waar geen opening is
5. Resultaat toont alle panelen met type-label
