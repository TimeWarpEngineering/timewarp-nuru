# Document that analyzer bump sweeps miss devcli content and tools/dev-cli

## Description

Moves a fact out of Claude-only auto-memory into the nuru repo, where every harness reads it
(timewarp-flow task 208). When an analyzer or CodeStyle bump adds a new error, a solution-wide `dotnet format`
on `timewarp-nuru.slnx` does not cover `source/timewarp-nuru-devcli/content/**` (compiled only by the
standalone runfile tests) or `tools/dev-cli` (compiled only by `dev self-install`). In task 588 the slnx sweep
looked green and `./bin/dev test` then failed three standalone tests on devcli content.

## Requirements

- Add one or two sentences under `### Code Style` in `.agent/local/nuru-specific.md`: after a slnx-wide
  `dotnet format` sweep for a diagnostic, grep `source/timewarp-nuru-devcli/content` and `tools/dev-cli` for
  the same pattern, fix by hand, and run `dev self-install` once.
- Carry a `why:` note naming task 588 (2026-11-15).
- When this lands, delete the Claude memory copy: `analyzer-bump-sweeps-miss-runfile-sources.md` and its line
  in `MEMORY.md` under timewarp-flow master
  `claude/projects/-home-steve-repos-github-com-TimeWarpEngineering-timewarp-nuru-git/memory/` (git-ignored, not
  a commit).

## Goal (implementer restatement)

Make the "slnx-wide `dotnet format` misses runfile-only sources" fact visible to every harness by adding it to the
repo's shared agent rules instead of Claude-only memory.

Acceptance criteria:

- `.agent/local/nuru-specific.md` `### Code Style` gains one short bullet: after a slnx-wide `dotnet format` sweep for
  a diagnostic, grep `source/timewarp-nuru-devcli/content` and `tools/dev-cli` for the same pattern, fix by hand, then
  run `dev self-install` once.
- The bullet carries `why: task 588 (2026-11-15)`.
- Deleting the Claude memory copy happens after merge (git-ignored, not part of this branch).

## Checklist

- [x] Sentence added under Code Style in `.agent/local/nuru-specific.md` with `why:`
- [ ] Claude memory file and its `MEMORY.md` line deleted after merge

## Session

- Created: 2356195 (2026-11-15)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 1814d311-7e29-436a-96d1-8ccf9a344c51, max-turns 500, Done — 2026-11-17T15:31:32Z

## Results

Added one bullet under `### Code Style` in `.agent/local/nuru-specific.md`: a slnx-wide `dotnet format` sweep skips
`source/timewarp-nuru-devcli/content` and `tools/dev-cli`; after a sweep for a new diagnostic, grep both paths, fix by
hand, run `dev self-install` once. Carries `why: task 588 (2026-11-15)`. Docs-only change; no product code touched.

Follow-up (after merge, not a commit): delete `analyzer-bump-sweeps-miss-runfile-sources.md` and its `MEMORY.md` line
under timewarp-flow master `claude/projects/-home-steve-repos-github-com-TimeWarpEngineering-timewarp-nuru-git/memory/`.

### Gates

- `./bin/dev build` — passed (0 errors).
- `./bin/dev test` — passed (multi-mode and standalone phases, 0 failed).
- `ganda repo audit` — passed with 1 advisory warning, `vscode-window-icon` (`.vscode/settings.json` missing). That
  file is git-ignored (`.gitignore:404`), so there is nothing to commit for it; left as is.

### How to validate

- Smoke: `grep -n "tools/dev-cli" .agent/local/nuru-specific.md`
- Expect: one line under `### Code Style` naming `source/timewarp-nuru-devcli/content`, `tools/dev-cli`,
  `dev self-install`, and `why: task 588 (2026-11-15)`.

## Notes

Filed from timewarp-flow task 208. Keep it to the sentence; Steve 2026-11-15: "the key is clarity not more
context."
