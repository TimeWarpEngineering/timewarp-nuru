# Decide whether WithNuruTools on an ASP.NET builder chain is supported, then fix the generator or the doc

## Description

A `WithNuruTools` call on an ASP.NET builder chain fails the build with `NURU_S999 Unrecognized DSL method: AddMcpServer`, even in a real `Microsoft.NET.Sdk.Web` project referencing TimeWarp.Nuru.McpHost and ModelContextProtocol.AspNetCore. Found by the task 523 consumer check (TimeWarp.Nuru 4.0.0-beta.5 from nuget.org, clean cache; evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

The user doc `documentation/user/.../mcp-hosting.md` (~L50) shows `builder.Services.AddMcpServer()...WithNuruTools(nuruApp)`. The source is the source of truth: `WithNuruTools` is a public extension in `source/timewarp-nuru-mcphost/nuru-mcp-server-builder-extensions.cs` (~line 26). Whether that call is meant to be usable on a non-Nuru builder chain must be decided from that file's intent (it has no Purpose/Design region, see the Purpose/Design task). The doc failing is a docs defect unless the public API says the pattern should work, and the public signature (an extension on the MCP server builder) suggests it should.

Note: the `timewarp-nuru-mcp` server is frozen; `timewarp-nuru-mcphost` (McpHost) is a different package and is not.

## Checklist

- [ ] Read `nuru-mcp-server-builder-extensions.cs` and the DSL interpreter to establish intent; record in Notes (ask the human if unclear)
- [ ] If supported: fix the generator so it does not interpret non-Nuru builder chains (`AddMcpServer`, `builder.Services...`) as the Nuru DSL, then add a regression test using a chain with a non-Nuru receiver
- [ ] If not supported: fix `mcp-hosting.md` to show the supported usage, and make the API shape or a diagnostic say so
- [ ] Add `#region Purpose` / `#region Design` to the touched files
- [ ] Verify with the `tw-nuru-verify` probe in a real Web SDK project (installed package, clean cache)
- [ ] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green

## Notes

- Use `.Map<T>()` in tests, not `DiscoverEndpoints()`.
- New analyzer diagnostics go in `AnalyzerReleases.Unshipped.md`, never Shipped.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
