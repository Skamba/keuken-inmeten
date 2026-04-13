---
applyTo: "Keuken-inmeten/**/*.razor,Keuken-inmeten/**/*.razor.cs,Keuken-inmeten/**/*.razor.css,Keuken-inmeten/**/*.razor.js"
---

# Blazor Component Instructions

## Component structure

- Razor markup in `.razor`, code-behind in `.razor.cs`, scoped styles in `.razor.css`, JS interop in `.razor.js`.
- Large pages use **partial classes** split by concern (e.g., `KastenInvoer.Kasten.cs`, `KastenInvoer.Wanden.cs`). Name the files `PageName.Concern.cs`.
- Components live in `Components/`, pages live in `Pages/`.

## Parameters & data flow

- Always use `[Parameter, EditorRequired]` for required parameters.
- Use `EventCallback<T>` for child-to-parent communication — never mutate parent state directly.
- Use `@key` when rendering lists where items can be reordered, added, or removed.

## State management

- Inject `KeukenStateService` (singleton) to read/write domain state.
- Call `StateHasChanged()` only when necessary — Blazor auto-rerenders after event handlers.
- Never call `StateHasChanged()` inside a loop or from a non-UI thread without `InvokeAsync`.

## SVG rendering

- Always use `CultureInfo.InvariantCulture` when formatting numbers for SVG attributes (coordinates, dimensions).
- Use `FormattableString.Invariant($"...")` or `.ToString(CultureInfo.InvariantCulture)` — decimal commas break SVG.
- Prefer `<g transform="translate(x,y)">` over absolute positioning for composable SVG groups.

## CSS isolation

- Scoped styles go in `.razor.css` files next to the component.
- Use `::deep` sparingly — prefer component-level styling.
- Bootstrap 5 classes are available globally; avoid duplicating Bootstrap utilities in scoped CSS.

## JS interop

- Keep JS interop files next to the component (e.g., `WandOpstelling.razor.js`).
- Use `IJSRuntime.InvokeVoidAsync` / `InvokeAsync<T>` for interop calls.
- Dispose JS references in `IAsyncDisposable.DisposeAsync()`.

## User-facing text

- All user-facing text must be in **Dutch**.
- Code comments and technical names may be in English.
