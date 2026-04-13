# Copilot Instructions — Keuken Inmeten

## Project Summary

Blazor WebAssembly (.NET 10) single-page application for measuring kitchen cabinets and calculating panel dimensions + drill hole positions for European 35 mm cup hinges. Everything runs client-side — there is no backend, no database, no API. State is persisted to `localStorage` and can be shared via URL-encoded links.

Live app: <https://skamba.github.io/keuken-inmeten/>

---

## Build, Test & Validate

Always run these commands from the **repository root** (not from a subdirectory).

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0.x |
| Node.js | 22.x |
| npm | (bundled with Node) |

### Step-by-step

```bash
# 1. Restore + run unit tests (≈ 10 s)
dotnet test Code.slnx --nologo

# 2. Publish the Blazor app to dist/ (≈ 15 s)
dotnet publish Keuken-inmeten/Keuken-inmeten.csproj -c Release -o dist --nologo

# 3. Install Playwright + deps (first time only)
npm ci
npx playwright install --with-deps chromium

# 4. Run E2E tests against the published output (≈ 30 s)
npm run test:e2e
```

> **Important:** Always publish to `dist/` *before* running E2E tests — Playwright serves from `dist/wwwroot`.

### Common pitfalls

- Do **not** run `dotnet run` or `dotnet build` from the repo root — use the commands above.
- The solution file is `Code.slnx` (XML-based slnx format), not a classic `.sln`.
- E2E tests auto-start a local server on port 4173 via `serve -s dist/wwwroot -l 4173`.

---

## Repository Map

```
.
├── Code.slnx                          # Solution file (2 projects)
├── package.json                       # Node config — E2E test scripts only
├── playwright.config.ts               # Playwright E2E configuration
├── README.md                          # User-facing readme (Dutch)
│
├── .github/
│   ├── copilot-instructions.md        # ← This file (repo-wide AI instructions)
│   ├── instructions/                  # Path-specific AI instructions
│   │   ├── blazor.instructions.md     # Blazor component conventions
│   │   └── testing.instructions.md    # Testing conventions
│   └── workflows/
│       ├── deploy-pages.yml           # CI/CD: test → publish → deploy to GitHub Pages
│       └── copilot-setup-steps.yml    # Pre-installs deps for Copilot cloud agent
│
├── Keuken-inmeten/                    # Main Blazor WASM project
│   ├── Keuken-inmeten.csproj          # .NET 10, nullable enabled, implicit usings
│   ├── Program.cs                     # Entry point — registers DI services, loads state
│   ├── App.razor                      # Root router component
│   ├── _Imports.razor                 # Global @using directives
│   │
│   ├── Models/                        # Domain types (all in Keuken_inmeten.Models)
│   │   ├── Enums.cs                   #   KastType, ScharnierZijde, PaneelType
│   │   ├── Kast.cs                    #   Cabinet with dimensions + hinge positions
│   │   ├── KeukenWand.cs              #   Kitchen wall containing cabinets
│   │   ├── KeukenData.cs              #   Serializable state snapshot
│   │   ├── KeukenDomeinDefaults.cs    #   Default values & factory methods
│   │   ├── PaneelToewijzing.cs        #   Panel assignment to cabinet(s)
│   │   ├── PaneelResultaat.cs         #   Calculated panel with drill holes
│   │   ├── MontagePlaatPositie.cs     #   Mounting plate position per hinge
│   │   ├── Plank.cs                   #   Shelf inside a cabinet
│   │   ├── Apparaat.cs                #   Appliance (oven, dishwasher, etc.)
│   │   └── ...                        #   Templates, PDF models, geometry
│   │
│   ├── Services/                      # Business logic (all in Keuken_inmeten.Services)
│   │   ├── KeukenStateService.cs      #   Central state container (singleton)
│   │   ├── ScharnierBerekeningService.cs  # Hinge calculation engine (static)
│   │   ├── PaneelGeometrieService.cs  #   Panel geometry calculations
│   │   ├── PaneelSpelingService.cs    #   Gap/spacing calculations
│   │   ├── ZaagplanService.cs         #   Saw plan generation
│   │   ├── BestellijstService.cs      #   Order list generation
│   │   ├── PersistentieService.cs     #   localStorage persistence
│   │   ├── DeelLinkService.cs         #   URL sharing codec
│   │   └── ...                        #   Helpers, formatters, validators
│   │
│   ├── Components/                    # Reusable Blazor components
│   │   ├── WandOpstelling.razor       #   Interactive wall layout editor
│   │   ├── PaneelVisueel.razor        #   Panel SVG visualization
│   │   ├── ScharnierDetailVisueel.razor # Hinge detail SVG
│   │   ├── KastFormulierOverlay.razor #   Cabinet form modal
│   │   ├── StapNavigatie.razor        #   Wizard step navigation
│   │   └── ...                        #   ~40 components total
│   │
│   ├── Pages/                         # Routed pages (step-based wizard)
│   │   ├── Home.razor                 #   / — Landing page
│   │   ├── KastenInvoer.razor         #   /kasten — Step 1: cabinet input
│   │   ├── PaneelConfiguratie.razor   #   /panelen — Step 2: panel config
│   │   ├── Verificatie.razor          #   /verificatie — Step 3: verification
│   │   ├── Zaagplan.razor             #   /zaagplan — Step 4: saw plan
│   │   ├── Bestellijst.razor          #   /bestellijst — Step 5: order list
│   │   └── Project.razor              #   /project — Project management
│   │
│   ├── Layout/                        # Shell layout
│   │   ├── MainLayout.razor           #   App shell with header
│   │   └── NavMenu.razor              #   Navigation menu
│   │
│   ├── docs/
│   │   └── user-journeys.md           # Detailed user journey scenarios
│   │
│   └── wwwroot/                       # Static assets (CSS, JS, icons)
│
├── Keuken-inmeten.Tests/              # xUnit test project
│   ├── Keuken-inmeten.Tests.csproj    # References main project
│   ├── ScharnierBerekeningServiceTests.cs  # Core algorithm tests
│   ├── KeukenStateServiceTests.cs     # State management tests
│   └── ...                            # ~40 test files, one per service/helper
│
└── e2e/                               # Playwright E2E tests
    ├── pages/                         # Page Object Models
    │   ├── IndelingPage.ts            #   Cabinet layout page
    │   ├── PanelenPage.ts             #   Panel configuration page
    │   └── ...                        #   One POM per page
    └── tests/                         # Test specs
        ├── indeling-shells.spec.ts    #   Wall/cabinet layout flows
        ├── panelen-editor.spec.ts     #   Panel editor flows
        └── ...                        #   ~7 E2E test files
```

---

## Architecture & Data Flow

```
User Input → KastenInvoer (Page)
                ↓
           KeukenStateService (singleton, central state)
                ↓
           ScharnierBerekeningService (static, pure functions)
                ↓
           PaneelResultaat (calculated panels + drill holes)
                ↓
           Verificatie / Zaagplan / Bestellijst (read-only pages)
                ↓
           PersistentieService → localStorage (auto-save on state change)
```

**Key design decisions:**
- `KeukenStateService` is a **singleton** registered in DI. It holds all mutable state and fires `OnStateChanged` after mutations.
- `ScharnierBerekeningService` is **static** with pure functions — no state, easy to unit test.
- Pages use **partial classes** split across multiple files (e.g., `KastenInvoer.razor` + `KastenInvoer.razor.cs` + `KastenInvoer.Kasten.cs` + `KastenInvoer.Wanden.cs`).
- Services are often split using **partial classes** for large files (e.g., `KeukenStateService.Kasten.cs`, `.Panelen.cs`, `.Wanden.cs`).

---

## C# Conventions

- File-scoped namespaces (`namespace Keuken_inmeten.Models;`)
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Use `var` when the type is obvious from the right-hand side
- Use collection expressions (`[]`) for empty collections
- Use pattern matching (`switch` expressions, `is`, `and`/`or`)
- Use primary constructors where appropriate
- PascalCase for public members; camelCase for private fields and locals
- **Dutch names** for domain terms (Kast, Paneel, Boorgat, Scharnier, Wand, Plank)
- **English names** for technical/framework terms (Service, Helper, Controller)

---

## Blazor Conventions

- `[Parameter, EditorRequired]` for required component parameters
- `@inject` for page-level service injection
- Favor small, composable components over large pages
- Use `EventCallback<T>` for child-to-parent communication
- Use `@key` for list rendering when items can change identity
- Format numbers with `CultureInfo.InvariantCulture` in SVG attributes
- Component JS interop files live next to the `.razor` file (e.g., `WandOpstelling.razor.js`)

---

## Error Handling

- Validate only at system boundaries (user input)
- Guard clauses for early returns
- No exceptions for control flow

---

## Domain: European 35 mm Cup Hinge Standard

| Parameter | Value |
|---|---|
| Cup hole diameter | 35 mm |
| Cup center from panel edge | 22.5 mm |
| Min distance from top/bottom edge | 80 mm |
| Hinges for height ≤ 1000 mm | 2 |
| Hinges for height ≤ 1500 mm | 3 |
| Hinges for height ≤ 2200 mm | 4 |
| Hinges for height > 2200 mm | 5 |

### Domain Glossary (Dutch → English)

| Dutch | English | Description |
|-------|---------|-------------|
| Kast | Cabinet | A kitchen cabinet unit |
| Wand | Wall | A kitchen wall that holds cabinets |
| Paneel | Panel | A door, drawer front, or blind panel |
| Scharnier | Hinge | European 35 mm cup hinge |
| Boorgat | Drill hole | 35 mm cup hole in a panel |
| Plank | Shelf | Adjustable shelf inside a cabinet |
| Apparaat | Appliance | Oven, dishwasher, etc. |
| Onderkast | Base cabinet | Floor-standing cabinet |
| Bovenkast | Wall cabinet | Wall-mounted upper cabinet |
| HogeKast | Tall cabinet | Full-height cabinet (e.g., pantry) |
| Deur | Door | Cabinet door panel type |
| LadeFront | Drawer front | Drawer face panel type |
| BlindPaneel | Blind panel | Non-opening filler panel |
| MontagePlaatPositie | Mounting plate position | Where the hinge plate attaches |
| Zaagplan | Saw plan | Cutting plan for panels |
| Bestellijst | Order list | Material ordering list |
| Speling | Gap/clearance | Space between panel and cabinet |
| Gaatjesrij | Hole row | System-drilled adjustment holes (32 mm spacing) |
| Toewijzing | Assignment | Panel-to-cabinet assignment |

---

## UI/UX

- Bootstrap 5 for layout and styling
- SVG for cabinet and panel visualizations
- Dutch language for **all** user-facing text
- Step-based wizard flow: Home → Kasten → Panelen → Verificatie → Zaagplan → Bestellijst

---

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/deploy-pages.yml`) runs on every push to `main`:

1. Check out repository
2. Set up .NET 10.0.x + Node.js 22
3. `npm ci` + install Playwright chromium
4. `dotnet test Code.slnx -c Release --nologo`
5. `dotnet publish` to `dist/`
6. `npm run test:e2e` (Playwright against published output)
7. Rewrite `<base href>` for GitHub Pages subdirectory
8. Deploy to GitHub Pages

Trust these instructions. Only search the codebase when the information here is incomplete or appears incorrect.
