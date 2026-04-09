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

## Iteration 2 - focused follow-up

- Started from `cf84b56` on `main` and refreshed the iteration-2 baseline screenshot set before changing code.
- Reused the earlier round-2 research direction on:
  - mobile resume-first hierarchy,
  - dense order-review screens that prioritize the table,
  - mobile drawers that preserve workspace context and do not block the next primary interaction.
- Reproduced the mobile paneel problem directly with Playwright: the full-height drawer blocked the first kast-click on a narrow viewport, confirmed by timeout evidence and `.agent/screenshots/iteration-2/mobile-paneel-editor-check.png`.

### Ranked issues entering iteration 2

1. `home-resume-hierarchy-overload` - severity 2 - mobile still showed too much secondary information under the main resume path.
2. `bestellijst-table-buried-by-overview` - severity 2 - the table still started too low in the dense review state.
3. `panelen-mobile-drawer-friction` - severity 2 - the mobile editor layer still blocked the first meaningful action.

### What changed

- **Home**
  - Projectoverzicht and Andere projectroutes are now collapsed by default for returning users.
  - The resume card stays fully visible, while secondary overview content is still one tap away.
- **Bestellijst**
  - Removed the separate metric row.
  - Condensed the toolbar into one compact review block with summary chips and a short export note.
  - Kept the per-wand and per-type breakdown behind the existing disclosure, now with summary counts in the header.
- **Panelen**
  - Opening a wand no longer auto-opens the editor drawer.
  - The empty mobile drawer became a compact bottom sheet instead of a full blocker.
  - Clicks outside the drawer now pass back to the workspace, and the empty drawer state explains only the next step by default.
  - Added a Playwright regression test that verifies the first mobiele kastselectie works before the editor opens.
- **Capture tooling**
  - Mobile spot checks now also capture `panelen-workspace-mobile` and `panelen-editor-mobile`.

### Before vs after observations

- **Home resume mobile:** clear improvement. The resume path remains dominant and the long secondary stack is gone unless the user explicitly opens it.
- **Bestellijst dense:** clear improvement. The ordertabel now starts much earlier and the top area reads as one compact control block instead of stacked summary zones.
- **Panelen mobile:** major improvement. The catastrophic blocked-selection state is gone; the user can open a wand, reach the canvas first, and treat the drawer as a temporary tool layer.

### What still feels wrong after iteration 2

- **Panelen mobile workspace:** even without the old blocker, the mobile editor view still spends too much vertical space on status copy and metric chrome before the canvas becomes the clear focal point.

### Remaining severity 2+ issues after iteration 2

1. `panelen-mobile-canvas-priority` - severity 2 - the mobile paneel workspace still makes users scroll through too much status before the main selection surface takes over.

### Stop gate after iteration 2

- **Not satisfied.** The old severity-2 issues from Home, Bestellijst, and the mobile drawer blocker are fixed, but there is still one severity-2 issue left in a primary mobile flow.
- **No regression found** in the validated flows, the refreshed screenshot set, or the new mobile regression test.

## Next

- Commit and push iteration 2 because the changes are validated and produced real UX gains.
- Start iteration 3 from the remaining severity-2 issue: make the mobile paneel canvas more dominant by compressing the status area above it.

## Iteration 3 - mobile panel hierarchy pass

- Started from `3646d73` on `main` and refreshed the iteration-3 before capture set before changing code.
- Rechecked current mobile workspace guidance around canvas-first layouts, lighter default status, and progressive disclosure for secondary explanation.
- Focused only on `panelen-mobile-canvas-priority`, because it was the last open severity-2 issue in a primary flow.

### What changed

- **Panelen**
  - Once a wall is open, the mode tabs switch to a compact variant without the extra subtitle copy.
  - Replaced the three separate mobile status cards with one compact summary block.
  - Shortened the active-workspace explanation and removed the duplicated `Open editorlaag` button when nothing is selected.
  - Kept the next-step guidance and editor state visible, but moved more visual weight onto the actual canvas and bottom editor sheet.
- **Validation and evidence**
  - Re-ran `dotnet test Code.slnx --nologo`, `dotnet publish Keuken-inmeten/Keuken-inmeten.csproj -c Release -o dist --nologo`, and `npm run test:e2e`.
  - Captured the full iteration-3 after set and created direct before/after comparison boards for `panelen-workspace-mobile` and `panelen-editor-mobile`.

### Before vs after observations

- **Panelen workspace mobile:** clear improvement. The canvas now enters materially sooner and the next step reads as one focused prompt instead of a stack of helper cards.
- **Panelen editor mobile:** clear improvement. The editor sheet still guides the user, but no longer competes with a heavy block of status chrome above it.
- **Overall:** the remaining top-of-step intro copy is now only a cosmetic inefficiency on very small screens, not a primary-flow blocker or confusion source.

### What still feels wrong after iteration 3

- The generic mobile step intro in Stap 2 could still be shortened slightly if later user evidence says the extra context is unnecessary after a wall is active.
- That issue is now severity 1 and not worth another code pass in this session because the main work surface, action hierarchy, and next-step clarity are already strong.

### Remaining severity 2+ issues after iteration 3

- None.

### Stop gate after iteration 3

- **Satisfied.** No severity 4 issues remain. No severity 3 issues remain. No severity 2 issues remain in primary flows or dense states, and the only deferred item is a low-value severity-1 cosmetic refinement.
- **No regression found** in the validated flows, refreshed screenshot set, or the earlier fixed mobile drawer path.

## Final stop-gate review

- Primary journeys now have a clear dominant task on each step, with secondary context moved behind lighter summaries or disclosures.
- Visual clutter is under control: dense screens no longer stack multiple equally loud metric blocks, repeated helper cards, or always-visible secondary actions ahead of the main work surface.
- State files and screenshot evidence are current through iteration 3, and the repo is ready for the final commit/push for this UX run.

## Resume verification - stop gate reopened

- Re-read the durable state, re-ran a fresh Playwright smoke pass on `main`, and captured a new full baseline set under `.agent/screenshots/iteration-4/before/`.
- The earlier stop call turned out slightly too optimistic: the mobile Panelen editor still stacked too much orientation copy and secondary help above the active canvas.

### Ranked issue entering iteration 4

1. `panelen-mobile-intro-still-verbose` - severity 2 - while a wand was already active on mobile, the screen still showed too many orientation blocks before the actual work surface.

### What changed

- **Panelen mobile hierarchy**
  - Replaced the long top intro with a compact active-wand cue plus a shorter `Staphulp` action.
  - Moved the paneel glossary below the active workspace once a wand is open, so the canvas no longer competes with secondary help.
  - Removed the redundant `Wandenoverzicht` intro while a wand is already active.
  - Shortened the waiting editor state and reduced the waiting bottom-sheet height.
- **Copy clarity**
  - Renamed the generic terminology disclosure heading to clearer glossary language (`Begrippen in gewone taal` / `Paneeltermen in gewone taal`).
  - Changed the per-term tag to `Wanneer relevant?`, which better matches the content behind the disclosure.
- **Regression coverage**
  - Added a new mobile Playwright regression test that verifies secondary explanation sits below the active panel workspace.

### Before vs after observations

- **Panelen workspace mobile:** major improvement. The canvas now appears materially sooner and the path to the next tap is clear without reading through a stack of repeated helper blocks.
- **Panelen editor mobile:** major improvement. The waiting editor now behaves like a concise tool layer instead of a mini onboarding panel.
- **Dense desktop screens:** small but worthwhile copy improvement. The glossary cards are easier to recognize at a glance because their labels now describe what they contain.

### Remaining severity 2+ issues after iteration 4

- None.

### Stop gate after iteration 4

- **Satisfied.** No severity 4 issues remain. No severity 3 issues remain. No severity 2 issues remain in primary flows, dense states, or critical decision points.
- **No regression found** in the 163 unit tests, 13 Playwright tests, fresh iteration-4 screenshots, or the new mobile hierarchy regression.

## Final stop-gate review after iteration 4

- Visual clutter is now under control, including the previously weakest mobile Panelen state: the main work surface becomes the focal point faster, and secondary help no longer crowds the top of the screen.
- Text clarity is now under control: primary headings, button labels, and glossary disclosures are specific enough to scan quickly without reading long explanatory sentences first.
- State files and screenshot evidence are current through iteration 4, and the repo is ready for the final commit/push for this UX run.

## Iteration 5 - counted wizard compaction

- Reopened the UX run under the stricter evidence rubric and treated iteration 5 as the first fully counted round with explicit journey, page, section, and element coverage.
- Upgraded `.agent/ux_capture.mjs` so each counted iteration now writes categorized screenshots plus `manifest.json` files for both before and after phases.
- Captured the full iteration-5 baseline set before app changes: **102 screenshots** (`8 journey`, `27 page`, `36 section`, `31 element`) across empty, validation, normal, dense, warning, completion, mobile, and repeated-form states.
- Rechecked the strongest remaining friction points against current guidance from USWDS and GOV.UK form-structure advice about progressive disclosure, one clear progress indicator, and reduced cognitive load in multi-step forms.

### Ranked issues entering iteration 5

1. `indeling-form-progress-redundancy` - severity 2 - the kast and apparaat wizards repeated step progress in too many visual forms before the user could focus on the first actual field.
2. `step-intro-stack-clutter` - severity 2 - Verificatie, Bestellijst, and Zaagplan still stack intro copy, help, and glossary UI above the primary task.
3. `paneel-desktop-drawer-overexplained` - severity 2 - the desktop panel drawer still spends too much height on explanation before the editable controls begin.
4. `verificatie-complete-primary-next-step-hidden` - severity 2 - the completion card still underplays the route to Bestellijst at a critical transition point.

### What changed

- **Step 1 forms**
  - Replaced the jargon-heavy `Mini-stepper` framing with a calmer `Huidige stap` label.
  - Removed the redundant progress bar and the extra badge rail from both the kast and apparaat wizards.
  - Kept one compact progress cue (`Stap x/y`) plus the existing help entry point.
  - Reduced the visual weight of the `hoofdmaten` / `maatvoering` helper blocks so the actual inputs begin sooner.
- **Evidence workflow**
  - Added explicit capture coverage for the apparaat wizard too, so repeated form variants are now included in the counted evidence set.
  - Generated six direct before/after comparison boards for the two wizard variants and their progress affordances.

### Screenshot coverage completed in this iteration

- **Before:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-5/before/`
- **After:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-5/after/`
- **Comparison boards:** 6 boards under `.agent/screenshots/iteration-5/after/comparisons/`

### Before vs after observations

- **Kast wizard:** the top of the dialog is materially calmer. Users now see one clear step cue and the first task faster, instead of reading the same progress three times.
- **Apparaat wizard:** same win. The form feels shorter and more direct because the opening chrome no longer competes with the actual fields.
- **Overall primary flow:** the improvement is real but local. The rest of the app still contains a few high-frequency clutter pockets, especially in shared top-of-page utility stacks and the desktop panel drawer.

### What still feels wrong after iteration 5

- Verificatie, Bestellijst, and Zaagplan still spend too much vertical space on page-intro utility UI before the main task starts.
- The desktop panel drawer still makes frequent users scroll through too much explanation before they can edit.
- The Verificatie completion card still does not make the next primary step dominant enough.

### Remaining severity 2+ issues after iteration 5

1. `step-intro-stack-clutter` - severity 2
2. `paneel-desktop-drawer-overexplained` - severity 2
3. `verificatie-complete-primary-next-step-hidden` - severity 2

### Stop gate after iteration 5

- **Not satisfied.** There are still three severity-2 issues in primary or critical flows, and the run has completed only **1 counted iteration out of the required minimum of 5** under the current rubric.
- **No regression found** in the 163 unit tests, 13 Playwright tests, fresh before/after evidence, or the wizard-specific comparison boards.

## Iteration 6 - compact support strip for steps 3-5

- Started from `5a6bb25` on `main` and captured a fresh iteration-6 before set before changing code: **102 screenshots** (`8 journey`, `27 page`, `36 section`, `31 element`).
- Reviewed fresh full-page evidence for Verificatie, Bestellijst, and Zaagplan and confirmed the same repeated pattern in both normal and dense states: title, intro paragraph, separate help button, and a full-width glossary card all appeared before the real task surface.
- Rechecked external guidance on progressive disclosure and clutter reduction from USWDS, Primer, and related UX references: optional help should stay reachable, but secondary support should sit behind lightweight controls rather than consume the same visual weight as the main task.

### Ranked issues entering iteration 6

1. `step-intro-stack-clutter` - severity 2 - Verificatie, Bestellijst, and Zaagplan still spent too much height on repeated intro, help, and glossary chrome before the main work began.
2. `paneel-desktop-drawer-overexplained` - severity 2 - the desktop paneel drawer still started too low because explanation and guided framing consumed too much of the drawer header.
3. `verificatie-complete-primary-next-step-hidden` - severity 2 - the completion state still underplayed the route to Bestellijst at an important transition moment.

### What changed

- **Shared secondary support**
  - Added a compact mode to `TerminologieUitleg` so glossary help can appear as a lightweight button and only expand inline on demand.
- **Stap 3, 4, and 5**
  - Replaced the separate right-aligned help row and the full-width glossary card with one compact support strip: `Staphulp` + `Begrippen`.
  - Shortened the top intro copy on Verificatie, Bestellijst, and Zaagplan so the screen purpose is clearer in one pass.
  - Kept both support routes available without letting them compete visually with the page’s main card or toolbar.

### Screenshot coverage completed in this iteration

- **Before:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-6/before/`
- **After:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-6/after/`
- **Comparison boards:** 6 boards under `.agent/screenshots/iteration-6/after/comparisons/`

### Before vs after observations

- **Verificatie:** strong improvement. The task list now starts noticeably higher and the page no longer spends a full extra block on secondary support before the first task card.
- **Bestellijst:** clear improvement. Review and export still feel supported, but the list reaches the main toolbar faster and the page reads as one coherent path instead of header + utility card + review card.
- **Zaagplan:** clear improvement. The page feels calmer because the optional support controls no longer create a second full-width panel above the already-large plan toolbar.
- **Dense states:** the gain holds. On dense Verificatie, Bestellijst, and Zaagplan screens, the shortened top area leaves more room for rows, lists, and plate visualizations without sacrificing orientation.

### What still feels wrong after iteration 6

- The desktop paneel-editor still makes frequent users scroll through too much explanatory framing before the editable fields begin.
- The Verificatie completion card still does not make the next primary step to Bestellijst dominant enough.

### Remaining severity 2+ issues after iteration 6

1. `paneel-desktop-drawer-overexplained` - severity 2
2. `verificatie-complete-primary-next-step-hidden` - severity 2

### Stop gate after iteration 6

- **Not satisfied.** Two severity-2 issues still remain in important flows, and the run has completed only **2 counted iterations out of the required minimum of 5** under the current rubric.
- **No regression found** in the 163 unit tests, 13 Playwright tests, refreshed dense-state screenshots, or the new comparison boards for Verificatie, Bestellijst, and Zaagplan.

## Iteration 7 - desktop paneel drawer compaction

- Started from `66205fb` on `main` and captured a fresh iteration-7 before set before changing code: **102 screenshots** (`8 journey`, `27 page`, `36 section`, `31 element`).
- The default evidence still only showed the waiting drawer state, so I also reproduced the selected desktop drawer manually and captured a targeted before screenshot at `.agent/screenshots/iteration-7/before/section/panelen-editor-drawer-selected-desktop.png`.
- Rechecked current guidance on progressive disclosure and side-panel forms: Primer and related drawer-form references all point to the same principle here - keep the next step visible, move longer explanation behind optional disclosure, and let editable controls start as high as possible.

### Ranked issues entering iteration 7

1. `paneel-desktop-drawer-overexplained` - severity 2 - the selected desktop drawer still spent too much height on a flow card, alert, help block, and separate status rows before the user could edit.
2. `verificatie-complete-primary-next-step-hidden` - severity 2 - the Verificatie completion state still underplayed the route to Bestellijst at a critical transition.

### What changed

- **Paneel drawer hierarchy**
  - Replaced the large four-step flow card, blue instructional alert, and separate wand/selectie status block with one compact `Volgende stap` focus panel.
  - Kept only three light status chips (`Wand`, `Geselecteerd`, `Opslaan`) beside the next-step copy.
  - Moved the longer tekening explanation fully behind one optional disclosure so recurring users are not forced through it on every edit.
  - Left the waiting drawer state short and explicit, so the drawer still feels clear before a kast is selected.
- **Compatibility**
  - Preserved the older helper API used by existing tests, while the UI itself now renders the lighter drawer framing.

### Screenshot coverage completed in this iteration

- **Before:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-7/before/`
- **After:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-7/after/`
- **Targeted selected-drawer evidence:** before/after screenshots under `.agent/screenshots/iteration-7/{before,after}/section/panelen-editor-drawer-selected-desktop.png`
- **Comparison boards:** 3 boards under `.agent/screenshots/iteration-7/after/comparisons/`

### Before vs after observations

- **Selected drawer:** major improvement. The first editable fields and the save CTA now start almost immediately, instead of after a four-step card, alert, help block, and separate status copy.
- **Waiting drawer:** still calm. The empty state remains clear without pretending the user has to read a mini onboarding panel first.
- **Panelen desktop page:** better overall rhythm. The drawer now behaves like a tool surface attached to the canvas, not a second explainer page competing with it.

### What still feels wrong after iteration 7

- The Verificatie completion card still does not make `Ga naar bestellijst` the dominant action inside the card itself; the main forward path still lives mostly in the lower step navigation.
- Some paneel helper copy is still slightly wordy at field level, but that now reads as severity 1 polish rather than a meaningful speed or clarity problem.

### Remaining severity 2+ issues after iteration 7

1. `verificatie-complete-primary-next-step-hidden` - severity 2

### Stop gate after iteration 7

- **Not satisfied.** One severity-2 issue still remains at a key transition point, and the run has completed only **3 counted iterations out of the required minimum of 5** under the current rubric.
- **No regression found** in the 163 unit tests, 13 Playwright tests, fresh iteration-7 before/after evidence, or the targeted selected-drawer comparison board.

## Iteration 8 - completion CTA made explicit

- Started from `9b5ff9c` on `main` and captured a fresh iteration-8 before set before changing code: **102 screenshots** (`8 journey`, `27 page`, `36 section`, `31 element`).
- Reviewed the fresh completion-page and completion-card evidence and confirmed the remaining problem: after a successful verification, the card itself still offered only `Terug`, `Deel`, and `Afdrukken`, while `Ga naar bestellijst` lived only in the lower step navigation.
- Rechecked current guidance for success and completion states from Wise Design, UX Bit, and related success-state references: the main next step should be explicit and visually dominant, while secondary actions should sit below or beside it with less emphasis.

### Ranked issues entering iteration 8

1. `verificatie-complete-primary-next-step-hidden` - severity 2 - the Verificatie completion card still underplayed the route to Bestellijst at the final decision point.

### What changed

- **Verificatie afronding**
  - Added a clear in-card primary CTA to Bestellijst so the user no longer has to rely on the lower step navigation to understand the next step.
  - Moved `Terug naar taaklijst`, `Deel link`, and `Afdrukken` into a secondary action row underneath the primary CTA.
  - Tightened the success copy so it now names the forward route first and treats share/print as extra options.
- **Regression coverage**
  - Added a Playwright test that completes verification, asserts the in-card Bestellijst CTA, and uses that route to enter Bestellijst.

### Screenshot coverage completed in this iteration

- **Before:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-8/before/`
- **After:** 102 screenshots (`8 journey`, `27 page`, `36 section`, `31 element`) under `.agent/screenshots/iteration-8/after/`
- **Comparison boards:** 2 boards under `.agent/screenshots/iteration-8/after/comparisons/`

### Before vs after observations

- **Completion card:** major improvement. The next step is now the dominant element directly below the success message, exactly where users look after checking that everything succeeded.
- **Whole completion page:** clear improvement. The bottom step navigation still exists for consistency, but it is no longer the only place that explains how to move forward.
- **Interaction pattern:** stronger and calmer. The decision point now reads as one primary route plus optional extras, instead of three similarly weighted buttons and a hidden next step elsewhere.

### What still feels wrong after iteration 8

- The generic intro line and support strip above the success card now feel slightly redundant in the afrondingsfase. They do not block the next step anymore, but they still add a little chrome above a state that is already complete.

### Remaining severity 2+ issues after iteration 8

- None.

### Stop gate after iteration 8

- **Not satisfied yet, but only because the run has completed 4 counted iterations and the brief requires at least 5.** Severity 4, 3, and 2 issues are now cleared in primary flows and dense states; only a small severity-1 polish opportunity remains.
- **No regression found** in the 163 unit tests, 14 Playwright tests, fresh iteration-8 before/after evidence, or the new completion-card comparison boards.
