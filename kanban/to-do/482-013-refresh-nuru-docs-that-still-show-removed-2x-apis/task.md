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

- [x] The D-1 file list no longer shows `CreateBuilder(args)`, `MapDefault`, `AddAutoHelp`, `TimeWarp.Nuru.Logging`, or a 1.x/2.x package pin on a current guide
- [x] Parser reference names the types that exist (`Lexer`, `Parser`, `Syntax`, `CompiledRoute`, matchers)
- [x] `error-handling.md` matches generator binding errors and handler exit-code behavior
- [x] `[NuruRoute]` XML matches NURU_A001 (one literal, or empty)
- [x] `source/timewarp-nuru-devcli/readme.md` calls `CreateBuilder()`
- [x] `documentation/posts/` is unchanged

## How to validate

Smoke:

```bash
rg -n "CreateBuilder\(args\)|MapDefault|AddAutoHelp|TimeWarp\.Nuru\.Logging|TimeWarp\.Nuru@1\.|TimeWarp\.Nuru@2\." documentation/user documentation/developer source/timewarp-nuru/attributes/nuru-route-attribute.cs source/timewarp-nuru-devcli/readme.md
rg -n "RoutePatternAst|NewRoutePatternParser" documentation/developer/reference
```

Expect: Both searches print no matches in living docs, the attribute remarks, or the DevCli readme. `documentation/posts/` may still match and is out of scope. `migrating-to-3.0.md` may name the old APIs in the removal table.

## Results

Living user and developer docs now teach the 3.0 surface. `documentation/posts/` is unchanged.

- `NuruApp.CreateBuilder(args)` and `new NuruAppBuilder()` became `NuruApp.CreateBuilder()` across the D-1 pages, the DevCli readme, and two design samples.
- `Map(pattern, delegate)` became `Map(pattern).WithHandler(...).AsCommand()/AsQuery().Done()` in the D-1 pages, `building-new-cli-apps.md`, `implementing-help.md`, `analyzer.md`, and `performance.md`. `Map<T>("pattern")` became `Map<T>()`.
- S-8: `routing.md` documents the default route as `Map("")`.
- `AddAutoHelp()` is gone. `auto-help.md`, `route-pattern-syntax.md`, `aot-compilation.md` and the glossary say help is built in and point to `ConfigureHelp(Action<HelpOptions>)`.
- S-9: both logging pages use `TimeWarp.Nuru.NuruLoggingExtensions`, which ships inside `TimeWarp.Nuru`. There is no separate package. The `debugging.md` link points at `source/timewarp-nuru/logging/nuru-logging-extensions.cs`.
- S-7 and S-10: `aot-compilation.md` and `deployment.md` use an unpinned `#:package TimeWarp.Nuru`.
- P-1: the parser references name `Lexer`, `Token`, `RouteTokenType`, the `*Syntax` nodes, `Parser`, `PatternParser`, `SemanticValidator`, `Compiler`, `CompiledRoute`, and the matchers. `OptionSyntax.LongForm`/`ShortForm` are dash-free; `OptionMatcher.MatchPattern`/`AlternateForm` carry the dashes.
- R-2: `error-handling.md` describes generator behavior.
  - Binding errors go through `app.Terminal.WriteLine` and `return 1`.
  - No match writes `Unknown command. Use --help for usage.` to stderr and returns 1.
  - Handler exceptions propagate out of `RunAsync`. Telemetry only records and rethrows them.
  - Handler return values are output. The exit code is `Environment.ExitCode`.
  - The stdout/stderr behavior is unchanged; that change is post-3.0.
- `[NuruRoute]` XML says the pattern is one literal or `""`, and points to NURU_A001 and `[NuruRouteGroup]`.
- Also removed other APIs that do not exist in `source/`:
  - `NuruAppOptions` and `CreateBuilder(args, options)`. `nuru-app-options.md` is now "Builder Configuration Options". `built-in-routes.md` explains overriding a built-in route by mapping the same pattern.
  - `RouteHelpProvider`, `EndpointCollection`, `ResolverResult`, `AddVersionRoute`, `EnableStaticCompletion`, and `EnableDynamicCompletion`.
- `dotnet build source/timewarp-nuru/timewarp-nuru.csproj`: 0 errors.

### How to validate

Smoke:

```bash
rg -n "CreateBuilder\(args|MapDefault|AddAutoHelp|TimeWarp\.Nuru\.Logging|TimeWarp\.Nuru@1\.|TimeWarp\.Nuru@2\." documentation/user documentation/developer/guides documentation/developer/reference source/timewarp-nuru/attributes/nuru-route-attribute.cs source/timewarp-nuru-devcli/readme.md
rg -n "RoutePatternAst|NewRoutePatternParser|NuruAppOptions|RouteHelpProvider" documentation/developer/reference documentation/user documentation/developer/guides
rg -n '\.Map\("[^"]*",' documentation/user documentation/developer/guides documentation/developer/reference
git diff --stat origin/master -- documentation/posts
```

Expect:

- First search: two hits only. `migrating-to-3.0.md` names `MapDefault(...)` in its removal table. `telemetry.md:39` is Aspire's `DistributedApplication.CreateBuilder(args)`, not Nuru.
- Second and third searches: no output.
- `git diff`: empty, because `documentation/posts/` is unchanged.

### Review

- Rounds: 2. Roster: general. Effort: 3 (by diff, 3496 lines).
- Round 1: 10 bugs open (M1–M7 from the reviewer, M8 and M9 raised from suggestion to bug, M10 added for `{value:float}` as NURU_P004). 0 suggestions, 0 nits.
- Round 2: those 10 bugs fixed, 0 open, 0 wontfix. No new findings.
- Disposition: clean. No exceptions and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Session

- Created: 537072 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Review oracle: grok 01a10c26-ade6-7920-b50f-35c6419409bf (2026-10-05)
- Round 1 reviewer: grok subagent 01a10c28-e156-7b71-a01b-74bed2d9d269 (2026-10-05, docs-accuracy-validator)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
