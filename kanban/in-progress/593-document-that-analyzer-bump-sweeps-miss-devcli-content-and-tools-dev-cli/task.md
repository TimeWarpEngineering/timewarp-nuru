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

## Checklist

- [ ] Sentence added under Code Style in `.agent/local/nuru-specific.md` with `why:`
- [ ] Claude memory file and its `MEMORY.md` line deleted after merge

## Session

- Created: 2356195 (2026-11-15)

## Notes

Filed from timewarp-flow task 208. Keep it to the sentence; Steve 2026-11-15: "the key is clarity not more
context."
