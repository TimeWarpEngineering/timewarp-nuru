# Decide whether WithNuruTools on an ASP.NET builder chain is supported, then fix the generator or the doc

## Description

A `WithNuruTools` call on an ASP.NET builder chain fails the build with `NURU_S999 Unrecognized DSL method: AddMcpServer`, even in a real `Microsoft.NET.Sdk.Web` project referencing TimeWarp.Nuru.McpHost and ModelContextProtocol.AspNetCore. Found by the task 523 consumer check (TimeWarp.Nuru 4.0.0-beta.5 from nuget.org, clean cache; evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

The user doc `documentation/user/.../mcp-hosting.md` (~L50) shows `builder.Services.AddMcpServer()...WithNuruTools(nuruApp)`. The source is the source of truth: `WithNuruTools` is a public extension in `source/timewarp-nuru-mcphost/nuru-mcp-server-builder-extensions.cs` (~line 26). Whether that call is meant to be usable on a non-Nuru builder chain must be decided from that file's intent (it has no Purpose/Design region, see the Purpose/Design task). The doc failing is a docs defect unless the public API says the pattern should work, and the public signature (an extension on the MCP server builder) suggests it should.

Note: the `timewarp-nuru-mcp` server is frozen; `timewarp-nuru-mcphost` (McpHost) is a different package and is not.

## Goal (implementer restatement)

`WithNuruTools` is an extension on `IMcpServerBuilder`, and its own XML `<example>` shows the
ASP.NET chain `builder.Services.AddMcpServer().WithHttpTransport().WithNuruTools(nuruApp)`. So the
pattern is supported, and the build failure is a generator defect, not a doc defect.

Acceptance:

- A method that builds a Nuru app and also calls `WebApplication.CreateBuilder(args)` and
  `builder.Services.AddMcpServer()...WithNuruTools(nuruApp)` compiles with no NURU_S999.
- The generator treats a `CreateBuilder` call as the Nuru DSL entry only when it binds to
  `TimeWarp.Nuru.NuruApp.CreateBuilder`; other `CreateBuilder` calls (ASP.NET, Host, user types)
  are ignored, and their `Build()` is not taken for a Nuru app.
- A regression test runs the doc's chain shape with a non-Nuru `CreateBuilder` receiver and the
  real `AddMcpServer()` + `WithNuruTools`, in the same method as a Nuru app.
- Touched files carry `#region Purpose` / `#region Design`.
- Gates green: `./bin/dev build`, `./bin/dev test`, `ganda repo audit`.

## Checklist

- [x] Read `nuru-mcp-server-builder-extensions.cs` and the DSL interpreter to establish intent; record in Notes (ask the human if unclear)
- [x] If supported: fix the generator so it does not interpret non-Nuru builder chains (`AddMcpServer`, `builder.Services...`) as the Nuru DSL, then add a regression test using a chain with a non-Nuru receiver
- [x] If not supported: n/a, the pattern is supported (doc left as is)
- [x] Add `#region Purpose` / `#region Design` to the touched files
- [x] Verify with the `tw-nuru-verify` probe in a real Web SDK project (installed package, clean cache)
- [x] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green (run through `./bin/dev test`)

## Review

- Round 1: Done

## Notes

- Use `.Map<T>()` in tests, not `DiscoverEndpoints()`.
- New analyzer diagnostics go in `AnalyzerReleases.Unshipped.md`, never Shipped.
- Intent: supported. `WithNuruTools` extends `IMcpServerBuilder`, and its XML `<example>` is the
  ASP.NET chain `builder.Services.AddMcpServer().WithHttpTransport().WithNuruTools(...)`, the same
  chain `mcp-hosting.md` shows. The doc is correct; the generator was wrong.
- Root cause: `InterpretBuildCall` interprets the whole method body around a Nuru `Build()`.
  `DispatchMethodCall` matched `CreateBuilder` by name only, so `WebApplication.CreateBuilder(args)`
  became an `IrAppBuilder`; `builder.Services` evaluated to that builder, and `AddMcpServer()` on it
  hit `HandleNonDslMethod` and threw NURU_S999. The ASP.NET `builder.Build()` was also recorded as a
  built Nuru app. `Host.CreateApplicationBuilder` was never affected (different name).
- Fix: `"CreateBuilder" when IsNuruAppCreateBuilder(invocation)` in
  `dsl-interpreter.expressions.cs`. It binds the call and requires `TimeWarp.Nuru.NuruApp`; an
  unbound call falls back to the `CreateBuilderLocator` syntax check (`NuruApp.CreateBuilder`).
  No new diagnostic.

## Results

Generator fix: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.expressions.cs`
(semantic `CreateBuilder` gate, Design region added). Regression test:
`tests/timewarp-nuru-mcphost-tests/mcphost-07-host-builder-chain.cs` (stand-in `WebApplication`
with a static `CreateBuilder`, real `AddMcpServer()` + `WithNuruTools`, same method as the Nuru
app). Without the fix the file fails to compile with
`NURU_S999: Unrecognized DSL method: AddMcpServer` (checked by reverting the fix); with it, 2/2 pass.
`mcp-hosting.md` is unchanged because its snippet is the supported usage.

Probe (`tw-nuru-verify` style): fresh `Microsoft.NET.Sdk.Web` project in `/tmp`, empty
`NUGET_PACKAGES`, `TimeWarp.Nuru.McpHost` 4.0.0-beta.13 packed by `./bin/dev build` (source-mapped
to `artifacts/packages`), `ModelContextProtocol.AspNetCore` 2.4.0, `Program.cs` = the doc's L50
snippet. `dotnet build`: Build succeeded, 0 errors. `dotnet run -- --check`: `probe-ok`.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Pass: Build completed successfully, 0 warnings, 0 errors |
| Test | `ganda runfile cache --clear` then `./bin/dev test` | Pass: exit 0; CI multi-mode 2197 total, 2191 passed, 6 skipped, 0 failed; standalone 0 failed |
| Audit | `ganda repo audit` | Pass: "Repository passes all audit checks." (first run passed with 1 advisory, `vscode-window-icon`: `.vscode/settings.json` missing; `ganda repo audit --fix` wrote the git-ignored local file, rerun clean) |

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-07-host-builder-chain.cs
```

Expect: both `McpHostHostBuilderChain` tests pass (tools `hbc-ping` and `hbc-greet` registered;
the Nuru app beside the host builder still dispatches). Reverting the one-line `"CreateBuilder"
when IsNuruAppCreateBuilder(...)` guard makes the file fail to build with
`NURU_S999: Unrecognized DSL method: AddMcpServer`.

### Review

Round 1, effort 2, roster general. Counts: bug 0, suggestion 0, nit 0; open 0. Disposition: clean.
No wontfix and no escalation. The implementer restatement matches the task. Paths:
`review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
`review/disposition.md`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-13T22:36:41Z
- Review oracle: reviewed by reviewer-grok (grok, model Grok 4.7), session not reported, max-turns uncapped (budget ladder 120), effort 2, roster general, round 1, disposition clean — 2026-11-14
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-13T22:43:46Z
