# Review framework — task 482-012

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-012-exclude-timewarpnurumcp-from-the-30-package-set/
**Diff scope:** commit `502bca0a` (`build(mcp): exclude TimeWarp.Nuru.Mcp from the 3.0 package set`) against its parent. Git stat is 7 files, 149 lines (99 insertions, 50 deletions). The csproj stat is inflated by a CRLF-to-LF rewrite; `git diff -w --ignore-cr-at-eol` is +7 lines in that file. Sibling `source/*/*.csproj` files are already LF.
**Plan / brief:** Finding S-4. Do not ship `TimeWarp.Nuru.Mcp` in the 3.0 package set. Leave the source in the repo and still building. Name the exclusion in release notes or the migration guide. Do not rewrite `GetSyntax` or `GenerateHandler`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a10c14-6b0b-7f13-bedb-fcea892ce42f (2026-10-05)

## Budget (by-diff)

- Lines changed: 149
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
