# Review framework — task 470-006

**Date:** 2026-09-22
**Host task:** kanban/to-do/470-006-escape-service-constructor-defaults-in-source-gen-di/
**Diff scope:** branch `task/470-006-escape-service-constructor-defaults-in-source-gen` vs `origin/master` (commits `d7d30104`, `9ec8c169`). Product files: `service-extractor.cs` (`GetDefaultValueExpression`), `route-matcher-emitter.cs` (FileInfo/DirectoryInfo catches), `generator-45-constructor-default-literals.cs`, CI standalone include and `CiTestExcludes`, comment in `routing-23-uri-fileinfo-directoryinfo.cs`.
**Plan / brief:** Parent 470 findings M8 and M21. Service constructor defaults must use `SymbolDisplay.FormatLiteral` / `FormatPrimitive` and fully-qualified enum members, with a generator-hosted regression for a quote/backslash/newline string and an enum optional parameter. FileInfo/DirectoryInfo conversions must catch the documented constructor exception set or `Exception`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle `01a0c897-3b40-7ec3-8305-0294d4448f04` (2026-09-22). Implementer `01a0c887-e9be-70a2-9c90-e33d2f4642c1` (2026-09-22).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
