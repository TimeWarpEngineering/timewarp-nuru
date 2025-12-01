# Flatten Namespaces to TimeWarp.Nuru

## Description

Flatten all sub-namespaces into `TimeWarp.Nuru` so users only need a single `using TimeWarp.Nuru;` to access everything when the package is included.

**Keep separate:** `TimeWarp.Nuru.Mcp` (standalone AI teaching tool)

**Flatten into `TimeWarp.Nuru`:**
- `TimeWarp.Nuru.Parsing`
- `TimeWarp.Nuru.Completion`
- `TimeWarp.Nuru.Repl`
- `TimeWarp.Nuru.Repl.Input`
- `TimeWarp.Nuru.Repl.Logging`
- `TimeWarp.Nuru.Analyzers`
- `TimeWarp.Nuru.Analyzers.Diagnostics`

## Checklist

### Implementation
- [ ] Update namespace declarations in `source/timewarp-nuru-parsing/` (27 files)
- [ ] Update namespace declarations in `source/timewarp-nuru-completion/` (25 files)
- [ ] Update namespace declarations in `source/timewarp-nuru-repl/` (20 files)
- [ ] Update namespace declarations in `source/timewarp-nuru-analyzers/` (4 files)
- [ ] Update namespace declarations in `source/timewarp-nuru-core/` (verify all use `TimeWarp.Nuru`)
- [ ] Remove unnecessary `using` statements that reference flattened namespaces
- [ ] Update `GlobalUsings.cs` files to remove flattened namespace imports
- [ ] Update test namespaces to match (e.g., `TimeWarp.Nuru.Tests`)
- [ ] Verify build succeeds
- [ ] Verify all tests pass

### Documentation
- [ ] Update any documentation referencing the old namespaces

## Notes

Decision made to simplify user experience. If a package is included, users have opted in - no reason to require multiple `using` statements. This follows the pattern of libraries like Dapper, MediatR, and FluentValidation that use flat namespaces.

MCP stays separate because it's a standalone application that teaches AI how to use Nuru, not a library consumed by user code.
