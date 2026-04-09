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

Use the reusable capture script, which builds the empty, normal, dense, validation, error, and completion states through the UI:

```bash
node .agent/ux_capture.mjs iteration-1 before
node .agent/ux_capture.mjs iteration-1 after
```

The script clears browser storage between scenarios and writes screenshots into `.agent/screenshots/<iteration>/<phase>/`.
Mobile spot checks now include `home-resume-mobile`, `indeling-normal-mobile`, `panelen-workspace-mobile`, and `panelen-editor-mobile`.
Comparison boards are stored under `.agent/screenshots/<iteration>/after/comparisons/`.

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

## State files

- `.agent/ux_state.json` - current machine-readable run state
- `.agent/ux_backlog.json` - structured issue backlog with stable IDs
- `.agent/ux_progress.md` - human-readable running log
- `.agent/screenshots/` - before/after evidence

## Resume rule

Resume from the highest-value unresolved issue in `.agent/ux_backlog.json`, not from memory.
