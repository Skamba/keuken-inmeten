# Keuken Inmeten

Een Blazor WebAssembly-app voor het inmeten van keukenkastjes. Bereken paneelafmetingen en boorgatposities voor Europese 35 mm kopscharnieren.

🌐 **Live app:** [https://skamba.github.io/keuken-inmeten/](https://skamba.github.io/keuken-inmeten/)

## Wat doet de app?

Doorloop een stap-voor-stap wizard om je keuken in te meten:

1. **Kasten** — voer de afmetingen van je kastjes in
2. **Panelen** — wijs panelen toe aan kasten
3. **Verificatie** — controleer de invoer
4. **Zaagplan** — overzicht van te zagen panelen met exacte maten
5. **Bestellijst** — exporteer de materiaallijst

Alle berekeningen draaien client-side; er is geen backend.

## Technologie

- [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/) op .NET 10
- Bootstrap 5 voor opmaak
- SVG voor visualisaties
- Europese 35 mm kopschanierstandaard (boorgat ø 35 mm, hart op 22,5 mm uit de rand)

## Lokaal draaien

```bash
cd Keuken-inmeten
dotnet run
```

De app is beschikbaar op `https://localhost:5001` (of de poort die .NET kiest).

## Tests uitvoeren

```bash
# Unit tests
dotnet test Code.slnx --nologo

# E2E tests (vereist Node.js en Playwright)
npm ci
npx playwright install --with-deps chromium
npm run test:e2e
```

## Deploymen

Bij elke push naar `main` wordt de app automatisch gepubliceerd naar GitHub Pages via de CI/CD-workflow in `.github/workflows/`.
