---
applyTo: "Keuken-inmeten.Tests/**/*.cs,e2e/**/*.ts,e2e/**/*.spec.ts"
---

# Testing Instructions

## Unit tests (xUnit)

- Test project: `Keuken-inmeten.Tests/`
- Framework: xUnit 2.9 on .NET 10
- Run: `dotnet test Code.slnx --nologo`
- One test file per service/helper — naming pattern: `{ClassName}Tests.cs`
- Test method naming: descriptive Dutch or English, using the pattern `MethodName_Scenario_ExpectedResult`
- Use `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterized tests
- Prefer testing public API surface; avoid testing private internals
- Services are mostly static or plain classes — no mocking framework needed

## E2E tests (Playwright)

- Test dir: `e2e/tests/` with Page Object Models in `e2e/pages/`
- Framework: Playwright for .NET-published Blazor WASM
- Run: `npm run test:e2e` (requires prior `dotnet publish ... -o dist`)
- Config: `playwright.config.ts` — serves from `dist/wwwroot` on port 4173
- One Page Object per page (e.g., `IndelingPage.ts`, `PanelenPage.ts`)
- Use Page Object methods instead of raw selectors in test files
- Tests run in Chromium headless; retries enabled in CI (2 retries)
- Screenshots on failure, video retained on failure, trace on first retry

## Adding new tests

- When adding a new service, create a matching `ServiceNameTests.cs` in the test project.
- When adding a new page or changing a user flow, add or update E2E specs.
- Always run the full validation sequence before pushing:
  1. `dotnet test Code.slnx --nologo`
  2. `dotnet publish Keuken-inmeten/Keuken-inmeten.csproj -c Release -o dist --nologo`
  3. `npm run test:e2e`
