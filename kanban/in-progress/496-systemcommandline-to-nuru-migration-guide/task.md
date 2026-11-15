# System.CommandLine to Nuru migration guide

## Description

Write a user guide for porting a CLI from System.CommandLine to TimeWarp.Nuru, centered on the three things
that push System.CommandLine users to look elsewhere: Native AOT, trimming, and the 2.0 API churn
(beta -> 2.0 renames and removed APIs). Requested by Steven on 2026-10-12 after the first Nuru competitor
adoption scan (timewarp-intel PR #4).

## Goal (implementer restatement)

Give a System.CommandLine user (2.0 GA or still on a 2.0 beta) one page that explains how to move a CLI to
Nuru, plus an agent skill that turns the page into a step-by-step recipe. Both must describe what Nuru 4.0
betas actually do today (probe the package, not the docs), including the places where Nuru behaves
differently from System.CommandLine.

Acceptance:

1. `documentation/user/guides/migrating-from-system-commandline.md` exists, is linked from
   `documentation/user/guides/overview.md`, and cross-links `migrating-to-3.0.md` (task 490 is done, so the
   flat `endpoints` `--capabilities` shape is the one to describe).
2. It covers: a concept map (RootCommand, Command, Option<T>, Argument<T>, SetAction, beta SetHandler) to
   routes, options, and handlers; AOT and trimming (what System.CommandLine apps need vs Nuru's generated
   path) with a `PublishAot` publish that has zero warnings; a beta-era API churn table (beta4 SetHandler,
   InvocationContext, CommandLineBuilder, and friends) mapped to Nuru; global-tool packaging; `--`
   passthrough; free-form trailing args; verbosity and configuration binding.
3. `skills/tw-nuru-migrate-system-commandline/SKILL.md` exists, uses the `skills/tw-nuru/SKILL.md`
   frontmatter shape (`name`, `description`), holds the porting recipe, mapping table and pitfalls, and
   links to the docs page for the prose instead of copying it.
4. An agent ports SlnUp, litmus, dotnet-run-script, dotnet-httpie and grate privately (scratch clones, no
   outreach) by following the skill only. Every step it had to invent is folded back into the skill or the
   page.
5. At least one port publishes with `PublishAot` and zero trim/AOT warnings.
6. Product defects found along the way are filed as follow-up tasks, not fixed here (this is a docs task).

## Relationship to task 490

Task 490 fixes `documentation/user/guides/migrating-to-3.0.md`, which is for **Nuru 2.x -> 3.0** users. This
task is a separate guide for **System.CommandLine -> Nuru** users. Overlap: both should describe the same 3.0
surface (e.g. `--capabilities` flat `endpoints` output). Cross-link the two guides, and land or rebase on 490
first so this guide does not copy the stale hierarchical `--capabilities` description.

## Deliverables

1. **Human docs page** (single source of truth): `documentation/user/guides/migrating-from-system-commandline.md`.
2. **Migration skill** at `skills/tw-nuru-migrate-system-commandline/SKILL.md`, next to the repo's existing
   `skills/tw-nuru` and `skills/tw-cli-search`, so our agents and outside agents can be pointed at it. It holds the
   step-by-step porting recipe, the API mapping table and pitfalls (`--` passthrough, free-form trailing args,
   verbosity/config binding, global-tool packaging, AOT/trim warnings), and links to the docs page for the
   full explanation instead of duplicating it. Keep the two in sync: the docs page owns the prose, the skill
   owns the agent recipe.

## Requirements

- Docs page linked from the guides overview; skill follows the format of `skills/tw-nuru/SKILL.md` (frontmatter name/description) and links to the docs page.
- Concept map: RootCommand/Command/Option<T>/Argument<T>/SetAction (and the old SetHandler) -> Nuru route patterns, options, handlers.
- AOT and trimming: what System.CommandLine needs (reflection, trim warnings) vs. Nuru's source-generated path; show a `PublishAot` publish with zero warnings.
- 2.0 API churn: a table of beta-era APIs (beta4 SetHandler, InvocationContext, CommandLineBuilder, etc.) and their Nuru equivalent, so users still on betas can skip straight to Nuru.
- Cover global-tool packaging, `--` passthrough, free-form trailing args, verbosity/config binding.
- Cross-link `migrating-to-3.0.md` (task 490).

## Validation

An agent ports these repos in order **by following the skill only**, as **private test runs, no outreach**
(no PRs, issues or comments on them). Any step the agent has to invent becomes a fix in the skill and/or docs page.

1. https://github.com/craigktreasure/SlnUp - baseline single command plus global-tool packaging.
2. https://github.com/ebrahim-s-ebrahim/litmus - option and argument mapping, JSON output.
3. https://github.com/xt0rted/dotnet-run-script - passthrough and `--` handling.
4. https://github.com/WeihanLi/dotnet-httpie - free-form `key=value` and `header:value` tail (hardest parsing case).
5. https://github.com/grate-devs/grate - mid-size, many options, verbosity and config binding.

## Checklist

- [x] Docs page written and linked from guides overview
- [x] Skill written in `skills/`, linking to the docs page
- [x] Cross-linked with migrating-to-3.0 (490)
- [x] Ports 1-5 done privately by an agent following the skill; invented steps folded back into skill/docs
- [x] AOT publish of at least one port with zero trim/AOT warnings
- [ ] PR merged

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6
- Round 1 fix pass: M1–M6 fixed (docs, skill, glossary; product gaps filed as task 591 items 11–13)
- Round 2: Done
## Notes

- System.CommandLine was the biggest group in the scan (49 of 205 repos). AOT pain shows up across every library.
- kendryte/nncase and QutEcoacoustics/emu are still on System.CommandLine 2.0 betas.
- Validation targets should be small and active: 10-999 stars, push in the last 90 days.
- Excluded as poor fits: GordonBeeming/copilot_here, Nimblesite/DataProvider.
- Source data: timewarp-intel PR #4 (merged 2026-10-12).

## Session

- Created: grok-bot (2026-10-12)
- 2026-11-15 implementer-claude (ganda task work, headless): merged origin/master (task 497 land). Probed
  TimeWarp.Nuru 4.0.0-beta.14 from nuget.org in scratch apps under `/tmp/nuru496/probe*` before writing,
  because several documented behaviors did not hold (see Results). Wrote the docs page and skill, then ran
  five port agents (sonnet subagents, one per target repo, scratch clones under `/tmp/nuru496/ports/`,
  local branches only, nothing pushed, no outreach). Each followed only the skill and reported its
  invented steps; every one was either folded into the page and skill or filed as a product defect.
  Filed task 591 for the product defects.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-sonnet-5-5), session b9116d6a-fc74-4920-bb1a-05e41098b727, max-turns 500, Done — 2026-11-15T03:44:39Z
- 2026-11-15 review oracle (general, effort 3, print mode, turn cap 200): round 1 under `review/round-1/`. Open findings M1–M6. Disposition not written. The launch prompt did not supply a vendor session id.
- 2026-11-15 implement-fix-2 (implementer-claude, headless): fixed review round 1 M1–M6 in the guide, the
  skill, and `glossary.md`; appended the unread `Order`/`IsRepeated` and endpoint `FileInfo` binding gaps to
  task 591 (items 11–13). Verified each finding against `endpoint-extractor.properties.cs` first.
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200), artifacts general.md, merged.md — 2026-11-15T03:55:29Z
- 2026-11-15 review-2 oracle (general, effort 2, print mode, turn cap 120): round 2 under `review/round-2/`. Re-verified M1–M6 fixed on `95dc80b8`. No new findings. Disposition clean.
- Review-2 oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts review/round-2/general.md, review/round-2/merged.md, review/disposition.md — 2026-11-15

## Results

### What changed

- `documentation/user/guides/migrating-from-system-commandline.md` (new): before-you-start (net11.0, packages),
  concept map, worked example, arguments/options (required options, option syntax, an argv-normalizer for
  `=` forms and legacy aliases), subcommands and global options, results/exit codes/exceptions, free-form
  trailing args, `--` passthrough (with a split-at-`--` workaround), configuration (`IConfiguration`,
  binder generator, existing service containers), runtime verbosity filter, AOT and trimming with a
  zero-warning publish, .NET tool packaging, tests in a separate project (CS0436 / NoHandlerException fix),
  the beta4 -> 2.0 -> Nuru churn table, behavior differences, checklist.
- `skills/tw-nuru-migrate-system-commandline/SKILL.md` (new): Step 0-9 recipe, mapping table, rules and
  pitfalls, report format; links to the page for prose.
- `documentation/user/guides/overview.md`: guide entry. `documentation/user/guides/migrating-to-3.0.md`:
  reverse link. `readme.md`: pointer to the skill next to the `tw-nuru` skill.

### Port validation (private, no outreach)

| # | Repo | Build | Tests | AOT publish | Notes |
|---|------|-------|-------|-------------|-------|
| 1 | craigktreasure/SlnUp | 0 warnings (strict analyzers on) | 49 + 71 + 13 pass | **0 IL warnings**, 5.2 MB | `.sln` output byte-identical to the original; tool packs and installs |
| 2 | ebrahim-s-ebrahim/litmus | 0 warnings | 259 pass | **0 IL warnings** after replacing an anonymous-type `JsonSerializer` call with a `JsonSerializerContext` (8 before) | JSON output keys/order unchanged; tool packs and installs |
| 3 | xt0rted/dotnet-run-script | 0 warnings (Roslynator on) | 121 pass, 1 skipped (Windows) | IL warnings only from the repo's own `JsonSerializer` calls | passthrough identical to the original using the split-at-`--` workaround |
| 4 | WeihanLi/dotnet-httpie | 0 warnings | 169 unit + 19 integration pass | same 6 IL warnings as the original (Roslyn scripting), none from Nuru | 8 of 10 request comparisons identical; diffs are `--raw=x` (defect 1) and `--foo` now erroring |
| 5 | grate-devs/grate | 0 warnings (CLI + providers) | 539 of 546 pass (5 fail: help/stderr differences) | 17 warnings, none from Nuru (Dapper, Oracle, SqlClient, grate's own formatter) | SQLite smoke and `-v Debug` output identical to the original |

Invented steps the agents reported, now in the page and skill: `using TimeWarp.Terminal` / DI usings,
internal endpoints, `global.json` and NBGV history, `GeneratePackageOnBuild` vs AOT, `--source` vs
`--add-source`, test-project CS0436 and `NoHandlerException` (build the app in the CLI assembly,
`PrivateAssets="analyzers;build"`, not `all`), process-wide `Environment.ExitCode` in tests, compiled
`ConfigureServices` (static holders for substitutes), literal-only `WithDescription` and builder chains,
shared options via `[NuruRouteGroup("")]` + `[GroupOption]` (a plain base `[Option]` is silently ignored),
multi-alias options, `-h` reserved, enum limits (hyphens, duplicate simple names), `Type` property name,
optional-argument defaults and optional-before-required positionals, `bool` flags that default to `true`,
explicit `--flag false`, several values per token, help table truncation, stdout parse errors, Ctrl+C,
retargeting libraries under `TreatWarningsAsErrors`, `-f net11.0` with plural `TargetFrameworks`.

### Product defects found (filed as task 591, not fixed here)

1. `--name=value` / `--name:value` (and `/x:y`) are swallowed as configuration overrides, also after `--`.
2. `IOptions<T>` in an endpoint `Handler` constructor is not registered; the repo's own
   `samples/endpoints/07-configuration/advanced` sample fails with it.
3. Root `--help` hides the default route's parameters and options.
4. Extra positionals ignored on routes with a parameter; `-o` with no value binds the default.
5. Ctrl+C does not cancel the handler token in a CLI run.
6. A `bool` option initializer (`= true`) is ignored; `--flag false` feeds `false` to a positional.
7. `--env A B` with `IsRepeated` leaks `B` into positionals.
8. A property named `Type`, or two same-named enum types, break the generated code.
9. Some parse errors print to stdout.
10. Unhandled handler exceptions abort the process (exit 134).
11. `[Parameter(Order = n)]` is not read and `NURU_A002` is never reported (review M1).
12. `[Option(IsRepeated = true)]` is not read; the property type decides (review M4).
13. Endpoint `FileInfo`/`DirectoryInfo`/`IPAddress` properties do not bind (review M5).

### How to validate

Smoke:

```bash
# The page and skill exist and cross-link
ls documentation/user/guides/migrating-from-system-commandline.md skills/tw-nuru-migrate-system-commandline/SKILL.md
grep -n migrating-from-system-commandline documentation/user/guides/overview.md documentation/user/guides/migrating-to-3.0.md readme.md skills/tw-nuru-migrate-system-commandline/SKILL.md
# Reproduce the zero-warning AOT claim with a fresh consumer
mkdir -p /tmp/v496 && cd /tmp/v496 && git init -q && dotnet new console -n app -f net11.0 >/dev/null && cd app
dotnet add package TimeWarp.Nuru --version 4.0.0-beta.14 >/dev/null
# replace Program.cs with the Nuru code from "A worked example" (handler body: print the bound values), then:
dotnet publish -c Release -r linux-x64 -p:PublishAot=true 2>&1 | grep -c 'warning IL'
# Reproduce the headline behavior difference
./bin/Release/net11.0/linux-x64/publish/app --output=x.txt   # Output stays null, exit 0
```

```bash
# Review round 1 fixes: no Order/IsRepeated in the recipe, normalizer stops at --, Step 4 uses Unit.ValueTask
grep -n "Order = \|IsRepeated = true" documentation/user/guides/migrating-from-system-commandline.md skills/tw-nuru-migrate-system-commandline/SKILL.md
grep -n 'arg == "--"' documentation/user/guides/migrating-from-system-commandline.md
grep -n "ExitCode = 2; return Unit.ValueTask" skills/tw-nuru-migrate-system-commandline/SKILL.md
```

Expect: both files exist; four files link the page; the publish prints `0` IL warnings; `--output=x.txt` is
not bound (exit 0), matching "Behavior differences to check" item 1. The review-fix greps print no `Order = ` /
`IsRepeated = true` lines, one `arg == "--"` line, and one `Unit.ValueTask` exit-code line.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Passed, 0 warnings, 0 errors |
| Test | `./bin/dev test` | Passed (exit 0; standalone phase 6/6; CI suite green) |
| Audit | `ganda repo audit` | First run failed: `kanban-landed` (task 591's new kitchen not yet on origin-home) and warning `vscode-window-icon` (`.vscode/settings.json` missing). Fixed by `ganda kanban publish 591` and `ganda repo audit --fix` (creates the git-ignored `.vscode/settings.json`); rerun: "Repository passes all audit checks." |

Review round 1 fix pass (2026-11-15, markdown only: guide, skill, `glossary.md`, task 591, review ledger):

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Passed, 0 errors ("Build completed successfully!") |
| Test | `./bin/dev test` | Passed ("Tests completed successfully!"; standalone 6/6, 0 failed) |
| Audit | `ganda repo audit` | Passed: "Repository passes all audit checks." |

The original build and test ran after the first draft of the page and skill. Markdown edits between that
draft and those gates were not a second full run. The review-round-1 fix pass recorded its own gates in
the table above.

### Review

Round 1 (general, effort 3) raised M1–M6. The fix pass fixed all six. Round 2 (general, effort 2) re-verified
them on `95dc80b8` and raised nothing new. Disposition: **clean** (0 open, no wontfix). The goal restatement
matches the original task; no misread.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

- M1: `Order` / `NURU_A002` removed from the recipe (concept map, samples, checklist, skill); position is
  property declaration order.
- M2: skill Step 4 uses `return Unit.ValueTask;` for the synchronous template.
- M3: the `=`/alias normalizer sample stops at `--` and copies the tail unchanged.
- M4: repeat comes from the array/collection type; `IsRepeated` documented as unread.
- M5: endpoint-property type set spelled out (`Version` binds; `FileInfo`/`DirectoryInfo`/`IPAddress` do
  not); `glossary.md` `built-in type` notes the narrower endpoint subset.
- M6: stray `**` removed in behavior difference 9; enum sentence split out.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
`review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.
Confirming ledger: `review/round-2/merged.md`. Disposition: `review/disposition.md`.
