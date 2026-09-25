# Review framework — task 443-002

**Date:** 2026-09-25
**Host task:** kanban/to-do/443-002-delete-nuru-local-mediator-types-and-test/
**Diff scope:** branch `task/443-002-delete-nuru-local-mediator-types-and-test` vs `origin/feature/443-mediator`
(commits 20c368fd, b76cdf49, e110d9d3; 227 files). Focus: `source/timewarp-nuru-analyzers/**`,
`source/timewarp-nuru/**`, `Directory.Packages.props`, `source/Directory.Build.props`, new/changed tests
(`capabilities-07-mediator-message-kind.cs`, `generator-46-generated-mediator.cs`). Sample/doc/tool
migrations spot-checked.
**Plan / brief:** delete Nuru-local `IMessage`/`IQuery`/`ICommand`/`IIdempotent*`/handlers/`Unit`; use
`TimeWarp.Mediator` 14.0.0-beta.3 contracts; explicit Query > IdempotentCommand > Command classification;
`ISender`/`IPublisher` resolve under static and Microsoft DI; breaking-change changelog + version bump.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task-work review oracle (cursor implementer profile)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
