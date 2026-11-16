# Ship the pull request build classifier in TimeWarp.Nuru.DevCli

## Description

The fleet standard for CI on pull requests was decided in timewarp-ganda (tasks 476, 479, 565, 587):

- one workflow, `.github/workflows/workflow.yml`, whose `pull_request` trigger has **no `paths` filter**, so every
  pull request reports the `CI` check;
- the decision "does this pull request need build and test?" is made in `dev workflow` (C#), not in YAML;
- the rule is `WorkflowChangeFilter.RequiresBuild`: a pull request skips build and test only when every changed
  path is a declared non-build path.

The rule exists today as five hand copies: `source/timewarp-ganda/services/workflow-change-filter.cs`, ganda's
`tools/dev-cli/endpoints/workflow-command.cs`, the factory template
`source/timewarp-ganda/templates/dev-cli/endpoints/workflow-command.cs.txt`, and the dev CLIs of
timewarp-build-tasks and timewarp-multiavatar. Fifteen of the seventeen repos in the `architecture` reposet have no
copy at all (survey 2026-11-16, in ganda 656). A change to the allowlist has to be made by hand in each copy.

Every repo's dev CLI already references `TimeWarp.Nuru.DevCli`, which ships shared endpoints and services as content
(`content/any/services/ci-mode.cs`, `attestation-verifier.cs`, …). Put the classifier there so there is one
implementation and the existing pin-currency audit carries rule changes to every repo.

## Requirements

1. Add the classifier to `source/timewarp-nuru-devcli/content/any/services/` (next to `ci-mode.cs`). Port the rule
   from ganda's `WorkflowChangeFilter.RequiresBuild` exactly; read that file's Design region and its tests
   (`tests/timewarp-ganda-tests/workflow-change-filter.cs`, `factory-workflow-non-build-paths.cs`) as the spec:
   - non-build paths: `kanban/**`, `documentation/**`, `assets/**`, `notes/**`, `.github/**` except
     `.github/workflows/**`, a file named `.gitignore`, and any `.md` outside `source/`, `tests/`, `tools/`, `msbuild/`;
   - `source/`, `tests/`, `tools/`, `msbuild/` always build, including markdown;
   - `.editorconfig` builds (warnings are errors);
   - an empty diff does not require a build.
2. Provide the whole decision, not only the path test, so a repo's `workflow` command needs one call: read
   `pull_request.base.sha` / `head.sha` from `GITHUB_EVENT_PATH`, run `git diff --name-only base...head`, apply the
   rule, and return build / skip with the message to print. An unreadable event, a failed diff, a push, a release,
   and a local run never skip.
3. Let a repo extend the lists without forking the rule: additional build roots and additional non-build paths,
   passed by the caller or read from the repo config the package already loads (`repo-config.cs`). Decide which and
   say why. A repo must not be able to make `source/`, `tests/`, `tools/`, `msbuild/`, or `.editorconfig` non-build.
4. Keep the type and member names greppable and stable; ganda's audit (ganda 655) checks that a repo's
   `workflow` command calls it. Record the exact call a consumer writes under `## Results`.
5. Tests in this repo for every rule row above and for the never-skip cases.
6. Document it in the DevCli package docs: the standard, the one call, and that `workflow.yml` must not carry
   `paths` on `pull_request` (the `push` trigger may keep its filter).
7. Release a `TimeWarp.Nuru.DevCli` version that includes it and record the version under `## Results`; ganda
   655 and the rollout (ganda 656) pin to it.
8. Use it in this repo: timewarp-nuru's own `workflow` command has no classification today (its `pull_request`
   trigger has no `paths` filter, so every kanban-only pull request runs the full pipeline).

## Checklist

- [x] Classifier service in `timewarp-nuru-devcli` content, rule ported exactly from ganda
- [x] One-call decision (event read, diff, rule, message); never-skip cases covered
- [x] Repo extension point, with the protected roots not overridable
- [x] Tests for each rule row and never-skip case
- [x] Package docs updated
- [x] timewarp-nuru's own `workflow` command uses it
- [ ] Released; version recorded in Results (version bumped to 4.0.0-beta.15 here; `dev release` runs from master after merge)

## Goal (implementer restatement)

Ship one implementation of the "does this pull request need build and test?" rule inside the
`TimeWarp.Nuru.DevCli` source-only content, so every repo's dev CLI gets it from the package
instead of a hand copy.

Acceptance criteria:

- A service in `content/any/services/` applies ganda's `WorkflowChangeFilter.RequiresBuild` rule
  unchanged: kanban/documentation/assets/notes, `.github/**` except workflows, `.gitignore`, and
  `.md` outside the four build roots are non-build; `source/`, `tests/`, `tools/`, `msbuild/`
  always build (markdown too); `.editorconfig` builds; an empty diff does not build.
- One call decides the whole thing: read the pull request SHAs from `GITHUB_EVENT_PATH`, run
  `git diff --name-only base...head`, apply the rule, return build or skip plus the message.
  Any non-pull-request event, a local run, an unreadable event, and a failed diff build.
- A repo can add build paths and non-build paths (one place, decided and justified); no extension
  can make the four roots, `.github/workflows/**`, or `.editorconfig` non-build.
- Stable greppable names; the exact consumer call is recorded under `## Results`.
- Tests cover each rule row and each never-skip case.
- The DevCli readme documents the standard, the one call, and the no-`paths`-on-`pull_request` rule.
- A DevCli version containing it is prepared (version bump) and recorded under `## Results`.
- timewarp-nuru's own `dev workflow` uses it; the YAML path gate in `workflow.yml` goes away.

## Related tasks

- `timewarp-ganda` 655 — audit enforces the standard; merge refusal names the fix
- `timewarp-ganda` 656 — rollout to the `architecture` reposet

## Session

- Created: 31bc0748-8dda-4ee8-912e-8d546f2d6990 (2026-11-16)
- Implemented: ganda task work implement oracle, Claude Opus 5.5 (2026-11-16)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session c38d03ab-dd0c-4a05-afaf-48941e0e01f2, max-turns 500, Done — 2026-11-16T10:59:16Z
- Reviewer: Grok 4.7, general, effort 3, session 01a1e403-0252-7720-adf2-120385f3ebb0 (2026-11-16). Round 1 clean.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1e403-0252-7720-adf2-120385f3ebb0, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-16T11:11:52Z

## Results

### What shipped

- `source/timewarp-nuru-devcli/content/any/services/pull-request-build-classifier.cs`:
  `PullRequestBuildClassifier` (static) and `PullRequestBuildDecision` (record: `RequiresBuild`,
  `Message`, `ChangedPaths`). `RequiresBuild(paths)` is ganda's `WorkflowChangeFilter.RequiresBuild`
  ported unchanged; `RequiresBuild(paths, config)` adds the repo additions;
  `TryReadPullRequestShas` and `ListChangedPathsAsync` are ported from ganda too.
- `pull-request-build-config.cs`: `PullRequestBuildConfig` (`BuildPaths`, `NonBuildPaths`), read
  from `.timewarp/dev.jsonc` key `pullRequestBuild` (added to `RepoConfig` and `DevCliJsonContext`).
- Extension point decision: **repo config, not a caller argument.** The consumer call is then one
  identical line in every repo (ganda 655 greps for it), and the additions are data a person or
  the audit reads without parsing C#. `RepoConfig` is already loaded by every dev CLI. Entries
  are `path`, `dir/`, or `dir/**`; other wildcards are invalid and make the decision build.
  `buildPaths` beat every non-build list; `nonBuildPaths` are checked after the four protected
  roots, `.github/workflows/**`, and `.editorconfig`, so they cannot cover them.
- Never-skip: any event other than `pull_request` (push, release, workflow_dispatch, local run
  with no event name), unset/missing/malformed event file, event without both SHAs, failed
  `git diff`, unreadable `dev.jsonc`, invalid `pullRequestBuild` entry.
- timewarp-nuru: `dev workflow --stage classify` makes the call and writes `code=true|false` to
  `GITHUB_OUTPUT`; `--stage all` (local default) asks first and stops on skip. The `changes` job
  in `workflow.yml` now runs that command instead of the PowerShell/yq glob gate (task 537), on
  the same runner choice as the stage jobs, with `fetch-depth: 0`. Non-PR events skip the job's
  steps and the output defaults to `true`. The `pull_request` trigger still has no `paths`;
  `push.paths-ignore` stays. This repo's additions in `.timewarp/dev.jsonc`: `buildPaths`
  `readme.md`, `assets/logo.png` (packed into the nupkgs); `nonBuildPaths` `skills/**`,
  `.agent/**`, `.claude/**` (same set the old gate skipped).
- Docs: `source/timewarp-nuru-devcli/readme.md` section "Pull request build decision" (standard,
  one call, rule table, additions, split jobs) plus a 4.0.0-beta.15 migration note; changelog entry.
- Tests: `tests/timewarp-nuru-tests/devcli/pull-request-build-01-classifier.cs` (22 tests: every
  rule row, additions and protected roots, config parse, event JSON, every never-skip case, and
  end-to-end git repos for kanban-only skip, empty diff skip, source build, buildPaths build).

### The consumer call (ganda 655 checks for it)

```csharp
PullRequestBuildDecision decision = await PullRequestBuildClassifier.DecideAsync(repoRoot, ct);
Terminal.WriteLine(decision.Message);
if (!decision.RequiresBuild)
{
  return Unit.Value;
}
```

Greppable token: `PullRequestBuildClassifier.DecideAsync(`.

### Version

`TimeWarp.Nuru.DevCli` **4.0.0-beta.15** (`source/Directory.Build.props` bumped from beta.14 on
this branch). Not yet published: this walk cannot merge or release. After merge, cut it with
`dev release` from a clean synced master (tw-release); ganda 655 and 656 pin to 4.0.0-beta.15.

### How to validate

Smoke:

```bash
./bin/dev self-install   # tools/dev-cli changed
B=$(git rev-parse 5670e4c0~1); H=$(git rev-parse 5670e4c0)   # kanban-only commit
echo "{\"pull_request\":{\"base\":{\"sha\":\"$B\"},\"head\":{\"sha\":\"$H\"}}}" > /tmp/ev.json
GITHUB_EVENT_NAME=pull_request GITHUB_EVENT_PATH=/tmp/ev.json GITHUB_OUTPUT=/tmp/out ./bin/dev workflow --stage classify; cat /tmp/out
GITHUB_EVENT_NAME=push ./bin/dev workflow --stage classify
dotnet run tests/timewarp-nuru-tests/devcli/pull-request-build-01-classifier.cs
```

Expect: the kanban-only range prints `Pull request changes only declared non-build paths.
Skipping build and test. ... (1 changed paths)` and `/tmp/out` holds `code=false`; a range that
touches `.github/workflows/workflow.yml` prints `Build path changed: ... Full build.` and
`code=true`; the push run prints `Event push is not a pull request. Full build.`; the test file
reports 22 passed.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | exit 0, 0 warnings, 0 errors |
| Test | `./bin/dev test` | exit 0; multi-mode 2260 total, 2252 passed, 8 skipped, 0 failed (PullRequestBuildClassifier 22/22); standalone 0 failed |
| Audit | `ganda repo audit` | exit 0, passed with 1 advisory: `vscode-window-icon` (`.vscode/settings.json` missing) |
| Audit fix | `ganda repo audit --fix` | exit 0, "Repository passes all audit checks." (fix wrote the git-ignored `.vscode/settings.json`; nothing tracked changed) |
| Audit (rerun after fix) | `ganda repo audit` | first commit's attest refused: `runfile-executable` (new test runfile lacked the executable bit; the earlier audit ran while it was untracked). Fixed with `chmod +x` (mode 100755); rerun exit 0, "Repository passes all audit checks." |

Also: `dotnet build` in `tests/devcli-consumer-check` (the content compiled as a strict net11
consumer, warnings as errors) succeeded.

### Follow-ups

- Release 4.0.0-beta.15 after merge (`dev release`), then ganda 655/656 pin to it.
- `push.paths-ignore` in `workflow.yml` and `pullRequestBuild` in `dev.jsonc` are two lists for
  two events; the comment beside each names the other.

### Review

- Round 1, roster general, effort 3. Counts: bug 0, suggestion 0, nit 0 (all statuses 0). Disposition: **clean**. No wontfix, no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Gates were not re-run. The implementer's `## Results` record (build, test, audit) covers the diff.

## Learnings

No misread. The restatement matches the rule, the one call, the extension limits, the tests, the readme, and the in-repo workflow. Requirement 7 says release the package; the restatement says the version bump is prepared and recorded. The branch is `4.0.0-beta.15` and the release checklist item stays open, because `dev release` runs from master after merge. That did not change the classifier.

## Review

- Round 1: Done

## Notes

Requested by Steven 2026-11-16 from a timewarp-state session, after two kanban-only pull requests (#688, #689)
were refused at merge for having no CI check: "we've addressed this in ganda a dozen times… figure out a solution
not a hack". Placing the classifier in TimeWarp.Nuru.DevCli was the agent's proposal; Steven approved filing it.
