# Fix MCP Examples Manifest Drift And Endpoint Syntax Regions

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (product bugs discovered by
re-enabling the MCP tests in CI — task 454-001).

## Description

Two user-facing MCP server content bugs:

1. **examples.json manifest drift**: 28 of 58 entries in `samples/examples.json` point at
   paths that no longer exist — the entire endpoint-samples section was renumbered/renamed
   (e.g. manifest says `samples/endpoints/10-logging/endpoint-logging-serilog.cs`, tree has
   `samples/endpoints/09-logging/`; manifest `09-repl` vs tree `10-repl`; `11-discovery` vs
   `13-discovery`; `12-completion` vs `11-completion`; `13-runtime-di` vs `12-runtime-di`;
   many endpoint file names changed too). `GetExampleTool.GetExampleAsync` fetches from
   GitHub master using these paths → 404 "Error fetching example" for every affected ID.
   Run `python3 -c "import json,os; [print(e['id'],e['path']) for e in
   json.load(open('samples/examples.json'))['examples'] if not os.path.exists(e['path'])]"
   for the current list.

2. **Endpoint syntax regions unresolvable**: `GetSyntaxTool`
   (`source/timewarp-nuru-mcp/tools/get-syntax-tool.cs`) treats Endpoint DSL as the
   default/priority-1 path, mapping features to `MCP:endpoint-*` regions — but the only
   embedded resource is the FLUENT syntax file
   (`timewarp-nuru-mcp.csproj:41` embeds `samples/fluent/03-syntax/fluent-syntax-examples.cs`,
   which contains only `#region MCP:fluent-*`). No file anywhere defines
   `#region MCP:endpoint-*`, so `GetSyntax("literals")`, `GetPatternExamples("basic")`,
   etc. ALWAYS return "Region 'MCP:endpoint-...' not found" for the recommended DSL.

## Requirements

- Regenerate/correct `samples/examples.json` to match the real samples tree (consider a
  script or CI check that validates manifest paths exist, so it can't drift again).
- Create the endpoint syntax examples file with `#region MCP:endpoint-{literals,parameters,
  types,optional,catchall,options,complex,descriptions}` regions (or decide fluent-only and
  rework the tool), embed it in timewarp-nuru-mcp.csproj, and make GetSyntaxTool resolve it.
- Re-include `tests/timewarp-nuru-mcp-tests/mcp-02-syntax-documentation.cs` in
  `tests/ci-tests/Directory.Build.props` (currently excluded with a pointer to this task)
  and update its assertions to the final region content.
- Revisit the two mcp-01 tests repointed away from dead manifest entries
  (`Should_retrieve_hello_world_endpoint_example`, `Should_retrieve_testing_output_example`)
  — restore serilog/calculator-endpoint coverage once the manifest is fixed.
- Note: `GetExampleTool` fetches from GitHub MASTER at runtime, so CI only fully greens
  after the manifest fix reaches master. Consider making the tool/tests read local repo
  content to remove the remote coupling (relates to review finding M24's file, same service
  family).

## Checklist

- [ ] examples.json paths corrected + drift guard
- [ ] Endpoint syntax regions authored + embedded + tool resolves them
- [ ] mcp-02 re-included in CI and green
- [ ] mcp-01 serilog/calculator-endpoint coverage restored
- [ ] Decide on remote-master vs local-content coupling
