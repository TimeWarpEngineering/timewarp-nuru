# Fix MCP error-handling tool section headers after the error-handling.md rewrite

## Description

CI on master went red after PR #298 (482 fix set). Three tests in
`tests/timewarp-nuru-mcp-tests/mcp-05-error-documentation.cs` fail: `Should_get_parsing_error_scenarios`,
`Should_get_binding_error_scenarios`, `Should_get_conversion_error_scenarios`. The frozen MCP
`ErrorHandlingTool` keys its sections off literal headings in
`documentation/developer/reference/error-handling.md`, fetched from raw GitHub master. Child 482-013
renumbered and merged those headings, so the tool returns "Could not find content" (67–70 chars) and
the `> 100` length assertion fails. The child's own CI was green because the doc on master had not
changed yet.

Fix is the header map only. MCP stays frozen; conversion maps to the merged binding section.

## Checklist

- [x] `SectionHeaders` in `source/timewarp-nuru-mcp/tools/error-handling-tool.cs` match the current doc headings
- [x] `dotnet run tests/timewarp-nuru-mcp-tests/mcp-05-error-documentation.cs` passes locally (reads master doc)
- [x] PR #300 merged; PR #299 (beta.79 bump) CI green after master merged in; beta.79 released

## Notes

- Blocks 483 (beta.79 bump), whose PR #299 CI fails on these tests.
- Longer-term: the MCP tests scrape a living doc on master; any doc restructure breaks them. MCP is
  frozen and unpackaged (482-012), so consider dropping its tests from CI if this recurs.

## Session

- Created: claude 2412bd45 (2026-10-06)
