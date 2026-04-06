# Copilot Instructions — Keuken Inmeten

## Project Overview

Blazor WebAssembly (.NET 10) application for measuring kitchen cabinets to calculate panel dimensions and drill hole positions for European 35mm cup hinges. No backend — all logic runs client-side.

## Architecture

- **Models/** — Domain types (Kast, PaneelToewijzing, PaneelResultaat, etc.)
- **Services/** — Business logic (ScharnierBerekeningService, KeukenStateService)
- **Components/** — Reusable Blazor components (SVG visualizations)
- **Pages/** — Routed pages (step-based wizard flow)
- **Layout/** — Shell layout and navigation

## C# Conventions

- File-scoped namespaces
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Use `var` when the type is obvious from the right-hand side
- Use collection expressions (`[]`) for empty collections
- Use pattern matching (`switch` expressions, `is`, `and`/`or`)
- Use primary constructors where appropriate
- PascalCase for public members; camelCase for private fields and locals
- Dutch names for domain terms (Kast, Paneel, Boorgat, Scharnier)
- English names for technical/framework terms

## Blazor Conventions

- `[Parameter, EditorRequired]` for required component parameters
- `@inject` for page-level service injection
- Favor small, composable components over large pages
- Use `EventCallback<T>` for child-to-parent communication
- Use `@key` for list rendering when items can change identity
- Format numbers with `CultureInfo.InvariantCulture` in SVG attributes

## Error Handling

- Validate only at system boundaries (user input)
- Guard clauses for early returns
- No exceptions for control flow

## Domain: European 35mm Cup Hinge Standard

| Parameter | Value |
|---|---|
| Cup hole diameter | 35 mm |
| Cup center from panel edge | 22.5 mm |
| Min distance from top/bottom | 80 mm |
| Hinges for height ≤ 1000 mm | 2 |
| Hinges for height ≤ 1500 mm | 3 |
| Hinges for height ≤ 2200 mm | 4 |
| Hinges for height > 2200 mm | 5 |

## UI/UX

- Bootstrap 5 for layout and styling
- SVG for cabinet and panel visualizations
- Dutch language for all user-facing text
- Step-based wizard flow: Kasten → Panelen → Resultaat
