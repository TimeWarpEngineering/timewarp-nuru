# Review framework — task 470-005

**Date:** 2026-09-22
**Host task:** kanban/to-do/470-005-use-getsymbolinfo-for-map-implements-addbehavior-type-args/
**Diff scope:** branch `task/470-005-use-getsymbolinfo-for-map-implements-addbehavior-t` vs `origin/master` (commits `7a013a68`, `6a4f8e4a`, `b6ea596f`). Product files: `type-syntax-resolver.cs`, `dsl-interpreter.cs` (`Map<T>`, `AddBehavior(typeof)`, `AddTypeConverter`), `implements-extractor.cs`, `service-extractor.cs` syntactic `AddSingleton` / `AddHttpClient` type arguments, `generator-44-referenced-type-arguments.cs`, CI standalone include and `CiTestExcludes`.
**Plan / brief:** Parent 470 finding M7. Type-argument extraction must try `GetSymbolInfo`, then `GetTypeInfo`, and reject `TypeKind.Error`, for `Map<T>`, `Implements<T>`, `AddBehavior(typeof)`, and service / HttpClient generic type arguments. Referenced-assembly regression when practical.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle `01a0c869-08f1-71f2-b76f-8299fdd0c2fc` (2026-09-22). Implementer `01a0c854-9d49-7e03-a6cf-e76ec33db687` (2026-09-22).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
