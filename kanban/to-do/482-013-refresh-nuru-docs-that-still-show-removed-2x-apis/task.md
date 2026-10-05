# Refresh Nuru docs that still show removed 2.x APIs

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding D-1, P-1, S-7, S-8, S-9, S-10, R-2 guide, R-7 readme from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Living user and developer docs still teach APIs 3.0 removed or never shipped. `documentation/user/guides/migrating-to-3.0.md` already names the removals. The pages below should agree with it and with the source.

Removed or nonexistent surface still in those pages:

- `NuruApp.CreateBuilder(args)` — `CreateBuilder` takes no arguments
- `MapDefault()` — use `Map("")`
- `AddAutoHelp()` — removed
- `new NuruAppBuilder()` — the constructor is internal
- `Map(pattern, delegate)` — `Map` is `Map(string)` and `Map(Func<…>)`
- `using TimeWarp.Nuru.Logging` and package `TimeWarp.Nuru.Logging` — logging is `TimeWarp.Nuru.NuruLoggingExtensions` inside TimeWarp.Nuru
- `#:package TimeWarp.Nuru@1.0.0` and `@2.1.0-beta.17` on current guides

Specific guides called out in the review:

- `documentation/developer/guides/aot-compilation.md` (S-7)
- `documentation/user/features/routing.md` (S-8, `MapDefault`)
- `documentation/user/features/logging.md` and `documentation/developer/guides/logging.md` (S-9)
- `documentation/user/guides/deployment.md` (S-10, package 1.0.0)
- `documentation/developer/reference/parser-classes-syntax-vs-semantics.md` and `parsing-flow-dependency-analysis.md` (P-1: `RoutePatternAst`, `NewRoutePatternParser`, and the other names in that table are not in `source/`)
- `documentation/developer/reference/error-handling.md` (R-2: the generator writes binding errors with `Terminal.WriteLine` and does not catch handler exceptions). Describe the generator. Do not change stdout to stderr on this id (that behavior change is post-3.0).
- `source/timewarp-nuru/attributes/nuru-route-attribute.cs` remarks say a pattern may be space-separated multi-word literals. `endpoint-extractor.cs` rejects anything but one literal or `""` (NURU_A001).
- `source/timewarp-nuru-devcli/readme.md` still shows `CreateBuilder(args)`.

Also update the other living pages listed in `review/supporting.md` D-1 (`getting-started.md`, `use-cases.md`, feature and reference pages, developer guides and glossary). Do not rewrite `documentation/posts/`.

Parent records: `review/supporting.md` D-1, `review/parsing.md` P-1, `review/runtime-core.md` R-2 and R-7.

## Checklist

- [ ] The D-1 file list no longer shows `CreateBuilder(args)`, `MapDefault`, `AddAutoHelp`, `TimeWarp.Nuru.Logging`, or a 1.x/2.x package pin on a current guide
- [ ] Parser reference names the types that exist (`Lexer`, `Parser`, `Syntax`, `CompiledRoute`, matchers)
- [ ] `error-handling.md` matches generator binding errors and handler exit-code behavior
- [ ] `[NuruRoute]` XML matches NURU_A001 (one literal, or empty)
- [ ] `source/timewarp-nuru-devcli/readme.md` calls `CreateBuilder()`
- [ ] `documentation/posts/` is unchanged

## How to validate

Smoke:

```bash
rg -n "CreateBuilder\(args\)|MapDefault|AddAutoHelp|TimeWarp\.Nuru\.Logging|TimeWarp\.Nuru@1\.|TimeWarp\.Nuru@2\." documentation/user documentation/developer source/timewarp-nuru/attributes/nuru-route-attribute.cs source/timewarp-nuru-devcli/readme.md
rg -n "RoutePatternAst|NewRoutePatternParser" documentation/developer/reference
```

Expect: Both searches print no matches in living docs, the attribute remarks, or the DevCli readme. `documentation/posts/` may still match and is out of scope. `migrating-to-3.0.md` may name the old APIs in the removal table.

## Session

- Created: 537072 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
