# UX runbook

Read these files first when resuming:
1. `.agent/ux_state.json`
2. `.agent/ux_backlog.json`
3. `.agent/ux_progress.md`
4. `.agent/ux_runbook.md`
5. `git --no-pager log --oneline -5`

## Start the app

```bash
export PATH=/root/.dotnet:$PATH
dotnet publish Keuken-inmeten/Keuken-inmeten.csproj -c Release -o dist --nologo
npm run e2e:serve
```

The published app runs on `http://127.0.0.1:4173`.

## Load realistic UX test data

Use the reusable capture script, which builds the empty, normal, dense, validation, error, completion, and responsive spot-check states through the UI:

```bash
node .agent/ux_capture.mjs iteration-1 before
node .agent/ux_capture.mjs iteration-1 after
```

The script clears browser storage between scenarios and writes screenshots into `.agent/screenshots/<iteration>/<phase>/`.
Each phase now contains explicit `journey/`, `page/`, `section/`, and `element/` subfolders plus a `manifest.json` with counts and paths.
Mobile spot checks include `home-resume-mobile`, `indeling-normal-mobile`, `panelen-workspace-mobile`, and `panelen-editor-mobile`.
Comparison boards are stored under `.agent/screenshots/<iteration>/after/comparisons/`.
The latest counted evidence set is `.agent/screenshots/iteration-7/`.
For the selected desktop paneel-drawer state, use the targeted screenshots under `.agent/screenshots/iteration-7/{before,after}/section/panelen-editor-drawer-selected-desktop.png`; the generic capture only records the waiting drawer state.

## Run validations

```bash
export PATH=/root/.dotnet:$PATH
dotnet test Code.slnx --nologo
dotnet publish Keuken-inmeten/Keuken-inmeten.csproj -c Release -o dist --nologo
npm run test:e2e
```

## Capture screenshots

Primary review viewport: desktop `1440x1200`.

Responsive spot-check viewport: mobile `390x844` (used for quick responsive validation, not the full screenshot set).

Run:

```bash
node .agent/ux_capture.mjs iteration-<n> before
node .agent/ux_capture.mjs iteration-<n> after
```

To verify the coverage floor quickly:

```bash
find .agent/screenshots/iteration-<n>/before -type f | wc -l
find .agent/screenshots/iteration-<n>/after -type f | wc -l
cat .agent/screenshots/iteration-<n>/before/manifest.json
cat .agent/screenshots/iteration-<n>/after/manifest.json
```

## State files

- `.agent/ux_state.json` - current machine-readable run state
- `.agent/ux_backlog.json` - structured issue backlog with stable IDs
- `.agent/ux_progress.md` - human-readable running log
- `.agent/screenshots/<iteration>/<phase>/manifest.json` - per-phase screenshot counts and file paths
- `.agent/screenshots/` - before/after evidence

## Resume rule

Resume from the highest-value unresolved issue in `.agent/ux_backlog.json`, not from memory.
If `.agent/ux_state.json` shows the stop gate fully satisfied, only resume for deferred severity-1 polish after new evidence.
