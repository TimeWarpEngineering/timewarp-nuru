# Exclude TimeWarp.Nuru.Mcp from the 3.0 package set

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding S-4 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Do not ship `TimeWarp.Nuru.Mcp` in the 3.0 package set. Do not rewrite the MCP server on this id.

`timewarp-nuru-mcp.csproj` is `PackAsTool` (`PackageId` `TimeWarp.Nuru.Mcp`). `GetSyntax("all")` asks for `MCP:endpoint-*` regions first. The embedded sample is `samples/fluent/03-syntax/fluent-syntax-examples.cs`, whose regions are `MCP:fluent-*`, so the endpoint half returns `Error: Region 'MCP:…' not found`. `GenerateHandler` emits `[NuruRoute("<full fluent pattern>", Description = ...)]` and a nested `Handler` that does not implement `ICommandHandler`. `[NuruRoute]` allows only one literal (NURU_A001).

The path-traversal fix `34ee7300` (2026-09-23) stays. Freeze of the tool remains `79706ebf` (2026-07-14) plus that fix. The security fix does not make the 3.0 answers correct.

Evidence: `source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj`, `tools/get-syntax-tool.cs`, `tools/generate-handler-tool.cs`. Parent record: `review/supporting.md` S-4.

Exclude the project from the pack that `tw-release` publishes for 3.0. Leave the source in the repo.

## Checklist

- [ ] The 3.0 pack step does not produce `TimeWarp.Nuru.Mcp.*.nupkg`
- [ ] `source/timewarp-nuru-mcp` still builds for anyone working in the repo
- [ ] Release notes or the migration guide say the MCP package is not part of 3.0
- [ ] No rewrite of `GetSyntax` or `GenerateHandler` on this id

## How to validate

Smoke:

```bash
dotnet pack source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj -c Release -o /tmp/nuru-mcp-pack-check
# after the exclude, the release pipeline's pack list must not include this project
find /tmp/nuru-mcp-pack-check -name 'TimeWarp.Nuru.Mcp*.nupkg'
```

Expect: A local pack of the project can still succeed. The 3.0 release pack output does not contain `TimeWarp.Nuru.Mcp`. The migration guide or release notes name the exclusion.

## Session

- Created: 536472 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
