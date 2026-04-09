# UX progress

## Bootstrap

- Read the current repo state, recent commits, page structure, layout, and e2e coverage.
- Verified local .NET 10 SDK availability at `/root/.dotnet`.
- Ran the baseline validation path: `dotnet test Code.slnx --nologo`, `dotnet publish Keuken-inmeten/Keuken-inmeten.csproj -c Release -o dist --nologo`, and a Playwright smoke test.
- Started a local static server for the published app on `http://127.0.0.1:4173`.
- Created the durable UX state artifacts and a reusable capture workflow.

## Iteration 1 - baseline review

- Captured the full baseline set for iteration 1: 24 screenshots across empty, normal, dense, validation, completion, warning, and mobile spot-check states, plus 3 contact sheets.
- Re-ran a smoke test on the served app before changing code.
- Reviewed every major journey against the clutter, hierarchy, guidance, and responsive basics criteria.
- Checked current web guidance for:
  - dashboard decluttering and hierarchy,
  - dense list scanability and progressive disclosure,
  - temporary mobile drawers that do not block core workspace interactions.

### Ranked issues

1. `home-resume-hierarchy-overload` - severity 3 - returning users must scan through too many equally loud sections before they can confidently continue.
2. `panelen-review-dense-scanability` - severity 3 - the main dense review list still has too many repeated badges and always-visible actions.
3. `indeling-dense-item-actions-ambiguous` - severity 2 - tiny icon actions are hard to parse and weak for touch.
4. `bestellijst-table-buried-by-overview` - severity 2 - the main review table starts too low on dense screens.
5. `home-empty-onboarding-overexplained` - severity 2 - first-use guidance repeats itself across too many visible blocks.
6. `panelen-mobile-drawer-friction` - severity 2 - narrow-screen editor behavior still needs a dedicated re-check.

### Baseline rubric

| State | Taak | Hier. | Clutter | Cogn. | Scan | Acties | Copy | Guidance | Status | Cons. | A11y | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| home-empty-desktop | 4 | 3 | 2 | 2 | 3 | 4 | 3 | 4 | 3 | 4 | 4 | Goede startknop, maar te veel uitlegblokken onder de hero. |
| home-resume-desktop | 4 | 2 | 2 | 2 | 2 | 3 | 3 | 3 | 4 | 4 | 4 | Hervatten is duidelijk aanwezig, maar concurrerende secties maken de pagina te lang. |
| home-resume-mobile | 3 | 2 | 2 | 2 | 2 | 3 | 3 | 3 | 4 | 4 | 3 | Mobiel is vooral een lange stapel van kaarten zonder sterk brandpunt na de hero. |
| indeling-empty-desktop | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 3 | 4 | 4 | Lege staat is rustig en legt de eerste actie goed uit. |
| indeling-normal-desktop | 4 | 4 | 3 | 3 | 3 | 4 | 4 | 4 | 4 | 4 | 4 | Primaire wandfocus is goed, maar objectlijsten kunnen compacter en duidelijker. |
| indeling-dense-desktop | 3 | 3 | 2 | 2 | 2 | 2 | 4 | 3 | 4 | 4 | 3 | Dense state verliest rust door drukke objectchips en cryptische acties. |
| panelen-editor-normal-desktop | 4 | 4 | 3 | 3 | 3 | 4 | 4 | 4 | 4 | 4 | 3 | Op desktop logisch, maar de editorlaag blijft gevoelig voor responsive frictie. |
| panelen-review-dense-desktop | 3 | 2 | 2 | 2 | 2 | 3 | 4 | 3 | 4 | 4 | 4 | Grootste open issue: te veel badges, samenvattingen en zichtbare acties tegelijk. |
| verificatie-tasklist-dense-desktop | 4 | 4 | 3 | 3 | 3 | 4 | 4 | 4 | 4 | 4 | 4 | Verificatie is relatief sterk; vooral lange lijsten, maar nog goed begrensd. |
| bestellijst-dense-desktop | 4 | 3 | 3 | 3 | 3 | 4 | 4 | 4 | 4 | 4 | 4 | De hoofdtaak is duidelijk, maar de tabel start later dan nodig. |
| zaagplan-dense-desktop | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | Focusmodus en waarschuwingen houden deze stap al rustig en doelgericht. |

### Next fix focus

- Compact the Home page for both empty and resume states.
- Make paneel review rows calmer by reducing visible detail and action noise.
- Replace tiny indeling action icons with clearer labeled controls.
- Pull secondary bestellijst distribution details out of the default view.

## Iteration 1 - execution and after review

- Implemented the first compaction pass in code:
  - Home now uses a single compact resume card, lighter alternative route cards, and collapsed extra onboarding context.
  - Paneel review now has one compact summary bar, calmer group headers, and destructive actions moved out of the default row view.
  - Indeling cabinet and device items now use readable labeled buttons instead of tiny icon-only actions.
  - Bestellijst now hides secondary distribution summaries behind a disclosure block so the table starts earlier.
- Republished the app, ran `dotnet test Code.slnx --nologo`, `dotnet publish Keuken-inmeten/Keuken-inmeten.csproj -c Release -o dist --nologo`, and `npm run test:e2e`.
- Captured the full after set plus comparison sheets under `.agent/screenshots/iteration-1/after/` and `.agent/screenshots/iteration-1/after/comparisons/`.

### Before vs after observations

- **Home empty:** clearly shorter. The first action is still obvious, while extra explanation is no longer forced on first-time users.
- **Home resume desktop:** major win. The next step and direct routes are now the focal point instead of competing with multiple stacked blocks.
- **Home resume mobile:** improved, but not done. The page is calmer, yet project metrics and alternative routes still make the phone view long.
- **Indeling dense:** better scanability and much lower action ambiguity. Labeled actions and clearer confirmations remove a real error-prone pattern.
- **Paneel review dense:** strong improvement. The screen is easier to scan because delete is no longer always visible and group summaries are lighter.
- **Bestellijst dense:** modest improvement. The disclosure block helps, but the dense review still spends too much height before the table.

### Remaining severity 2+ issues after iteration 1

1. `home-resume-hierarchy-overload` - now severity 2, mainly on mobile where too much secondary content is still expanded.
2. `bestellijst-table-buried-by-overview` - severity 2, because the main table still sits lower than ideal in the dense state.
3. `panelen-mobile-drawer-friction` - severity 2, still unresolved because the mobile editor drawer needs a dedicated validation/fix pass.

### Stop gate after iteration 1

- **Not satisfied.** Severity 3 issues are cleared, but there are still severity 2 issues in a primary/mobile flow and in a dense review screen.
- **No regression found** in the validated flows or the refreshed screenshot set.

## Next

- Commit and push iteration 1 because it produced real UX gains without regressions.
- Start iteration 2 from the remaining severity 2 items, beginning with mobile resume compaction and bestellijst density.
