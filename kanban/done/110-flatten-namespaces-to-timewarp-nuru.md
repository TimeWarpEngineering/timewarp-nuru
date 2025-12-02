# Flatten Namespaces to TimeWarp.Nuru

## Status: DONE

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

### Preparation
- [x] Simplify tab-completion test namespace (commit 3e35dae)

### Implementation - Incremental Migration
- [x] Rename conflicting types (commit 6f98802)
  - `TokenType` → `RouteTokenType`
  - `LoggerMessages` → `ParsingLoggerMessages`
- [x] Flatten `TimeWarp.Nuru.Parsing` → `TimeWarp.Nuru` (commit 3ddf04d)
- [x] Flatten `TimeWarp.Nuru.Completion` → `TimeWarp.Nuru` (commit 25d8ef8)
  - `LoggerMessages` → `CompletionLoggerMessages`
- [x] Flatten `TimeWarp.Nuru.Repl` + `.Input` + `.Logging` → `TimeWarp.Nuru` (commit f6f0819)
  - `TokenType` → `ReplTokenType`
  - Removed duplicate `ReplLoggerMessages` (already in core)
- [x] Flatten `TimeWarp.Nuru.Analyzers` + `.Diagnostics` → `TimeWarp.Nuru` (commit 9e4e7bf)

### Cleanup
- [x] Remove unnecessary `using` statements that reference flattened namespaces
- [x] Update `GlobalUsings.cs` files to remove flattened namespace imports
- [x] Update remaining test namespaces to match (e.g., `TimeWarp.Nuru.Tests`)

### Final Verification
- [x] Verify build succeeds
- [x] Verify all tests pass (112/113, repl-23-key-binding-profiles.cs known failure)

### Documentation
- [ ] Update any documentation referencing the old namespaces (deferred - docs will be updated separately)

## Notes

Decision made to simplify user experience. If a package is included, users have opted in - no reason to require multiple `using` statements. This follows the pattern of libraries like Dapper, MediatR, and FluentValidation that use flat namespaces.

MCP stays separate because it's a standalone application that teaches AI how to use Nuru, not a library consumed by user code.

### Type Renames Summary
| Original | Renamed To | Reason |
|----------|------------|--------|
| `TokenType` (parsing) | `RouteTokenType` | Conflict with REPL TokenType |
| `TokenType` (repl) | `ReplTokenType` | Conflict with Parsing TokenType |
| `LoggerMessages` (parsing) | `ParsingLoggerMessages` | Conflict with Completion LoggerMessages |
| `LoggerMessages` (completion) | `CompletionLoggerMessages` | Conflict with Parsing LoggerMessages |

### Commits
1. `3e35dae` - Simplify tab-completion test namespace
2. `6f98802` - Rename TokenType to RouteTokenType and LoggerMessages to ParsingLoggerMessages
3. `3ddf04d` - Flatten TimeWarp.Nuru.Parsing namespace
4. `25d8ef8` - Flatten TimeWarp.Nuru.Completion namespace
5. `f6f0819` - Flatten TimeWarp.Nuru.Repl namespace
6. `9e4e7bf` - Flatten TimeWarp.Nuru.Analyzers namespace
