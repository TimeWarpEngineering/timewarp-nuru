# Root --help hides the default route's parameters and options

## Description

A single-command tool (`[NuruRoute("")]` with `[Parameter]`/`[Option]`) shows only an empty-name row under
`Commands:` in root `--help`. There is no way to see its options from `--help`.

Expected: root `--help` for an app whose default route has parameters or options lists them.

## Goal (implementer restatement)

Root `--help` has to show the parameters and options of a default route, the route with no command
literal (`[NuruRoute("")]` or an empty pattern). That route has no `<command> --help` page, because
per-route help only runs when the route has a literal prefix, so root `--help` is the only page that
can show them.

Acceptance criteria:

- A single-command app whose default route has parameters or options shows those parameters and options
  on root `--help`, with their descriptions, and does not leave them behind a blank command name.
- When the app also has other commands, root `--help` still lists those commands and also lists the
  default route's parameters and options.
- A default route that has only a description, with no parameters, options, or examples, stays a row
  under `Commands:`. That row is how its description is shown.
- Named commands keep their parameters on `<command> --help`. Root `--help` does not repeat them.
- Tests cover each accepted case. The migration guide and the migrate skill stop saying that root
  `--help` hides the default route's parameters and options.

## Checklist

- [x] Decide per item: fix it, or keep the behavior and say why in Results
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear`, then the changed test files green (full CI suite runs on the PR)

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- 2026-11-19 implementer (grok, ganda task work): root help lists the default route's parameters, options, and examples.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f377-ba63-7e51-926e-0243f2c72b0a, max-turns 500, Done — 2026-11-19T11:25:51Z
- Review oracle: claude (claude-opus-5-5), ganda task work review node, effort 2, roster general, turn cap 120 — 2026-11-19, disposition accepted-exceptions
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 397ac3f3-6efc-4e20-9e5c-f5feb83021af, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-19T11:27:42Z

## Review

- Round 1: Done

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.

## Results

Fixed. A root default route (no group prefix and no command literal) has no `<command> --help` page, because per-route help only runs when the route has a literal prefix. `HelpEmitter` now prints that route on the root page: usage pattern, description, parameters, options, and examples. The blank command row is omitted for that route. Built-in options stay in the same options table, after the route's options. When the app also has other commands, those stay under `Commands:` and the usage block keeps `app [command] [options]`.

Kept: a default route with only a description (no parameters, options, or examples) stays a blank name under `Commands:`. That row is how its description is shown. A named command's parameters and options stay on `<command> --help` and are not repeated on the root page.

`documentation/user/guides/migrating-from-system-commandline.md` item 1 and `skills/tw-nuru-migrate-system-commandline/SKILL.md` (step 9 and the behavior list) describe the new behavior. `documentation/user/features/auto-help.md` notes that a default route's details are on root `--help`.

`ganda repo audit --fix` wrote the gitignored local `.vscode/settings.json` window title so `vscode-window-icon` passes. Nothing from that fix is committed.

### Gates

- `./bin/dev build`: pass (0 warnings)
- `ganda runfile cache --clear`: done before the test runs
- `dotnet run --file tests/timewarp-nuru-tests/help/help-11-root-default-route-parameters.cs`: 6/6 pass (rerun after removing the RCS1163 suppression: 6/6 pass)
- Extra regression runs on root and per-route help: help-01 11/11, help-01b 8/8, help-02-default-route-help 6/6, help-02-table-formatting 10/10, help-03 4/4, help-04 6/6, help-05 1/1, help-06 7/7, help-07 3/3, help-08 8/8, help-09 4/4
- `dotnet run --file tests/timewarp-nuru-tests/help/help-10-configure-help-filtering.cs`: does not compile standalone (pre-existing RCS1021 on the `ConfigureHelp` lambda; the file is unchanged from master)
- `ganda repo audit`: pass (45 passed, 0 failed, 1 skipped) after `--fix` for the local window title

### Review

- Rounds: 1. Effort 2, roster: general.
- Final counts: bug 0, suggestion 0, nit 2 (0 open, 0 fixed, 2 wontfix).
- Disposition: **accepted-exceptions**. M1 (option/parameter/example formatting duplicated from
  `route-help-emitter.cs`) and M2 (several promoted default routes share one Parameters/Options table)
  are wontfix nits; rationale in `review/disposition.md`.
- No mismatch between the goal restatement and the task.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

### How to validate

Smoke:

```bash
dotnet run --file tests/timewarp-nuru-tests/help/help-11-root-default-route-parameters.cs
```

Expect: 6 passed. A single `[NuruRoute("")]` command's root `--help` contains `Parameters:`, `--format, -f <format>`, and `Examples:`, and does not contain `Commands:`. An app that also has `status` lists `status` under `Commands:` and still lists the default route's parameters. A description-only `Map("")` stays under `Commands:`. A named `run {path} --loud` route does not put `Parameters:` or `--loud` on root `--help`.
