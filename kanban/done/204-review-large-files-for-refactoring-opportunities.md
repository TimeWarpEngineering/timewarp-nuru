# Review large files for refactoring opportunities

## Description

Files over 500 lines create cognitive load for both humans and AI agents. Large files waste tokens in agentic contexts and make code harder to understand and maintain. This task tracks files that should be reviewed for potential refactoring into smaller, more focused units.

## Checklist

### Source Files (Priority - Production Code)

- [x] `nuru-delegate-command-generator.cs` (1250 lines, 812 code) - Largest source file → Task 211
- [x] `nuru-attributed-route-generator.cs` (854 lines, 644 code) → Task 212
- [x] `repl-console-reader.selection.cs` (807 lines, 589 code) → Task 210
- [x] `endpoint-resolver.cs` (798 lines, 601 code) - Already well-refactored, no action needed
- [x] `nuru-core-app.cs` (709 lines, 433 code) → Task 214
- [x] `help-provider.cs` (643 lines, 440 code) → Task 215
- [x] `parser.cs` (628 lines, 485 code) → Task 216
- [x] `key-binding-builder.cs` (584 lines, 209 code, 325 comments) → Task 209
- [x] `nuru-invoker-generator.cs` (540 lines, 380 code) → Task 213
- [x] `nuru-app-builder-extensions.cs` (509 lines, 315 code) → Task 217

### Sample Files

- [x] `pipeline-middleware.cs` (696 lines, 427 code) - Consider splitting into multiple samples → Task 218

### Test Files (Lower Priority)

- [x] All test files tracked in separate task → Task 219

## Notes

### Excluded from Review

Generated files were excluded from this list as they are auto-generated:
- `GeneratedDelegateCommands.g.cs` (2916 lines)
- `Mediator.g.cs` (multiple instances, 1263-2023 lines)
- `ConsoleApp.g.cs` (1050 lines)
- `GeneratedRouteInvokers.g.cs` (873 lines)

### Refactoring Strategies

1. **Partial classes** - Split by responsibility (already used in some files like `repl-console-reader.selection.cs`)
2. **Extract helper classes** - Move utility methods to separate files
3. **Extract interfaces** - Create smaller focused interfaces
4. **Compose over inherit** - Break monolithic classes into composed smaller classes
5. **Region-based splitting** - If regions are used, each region could become its own file

### Metrics Explanation

- **Total**: Total line count including blank lines
- **Code**: Lines with actual code (excluding comments and blanks)
- **Comments**: Lines that are comments (///, //, /*, *)
- **Blank**: Empty lines

### Target Guidelines

- Aim for files under 300-400 lines for optimal readability
- Test files can be slightly larger due to test data
- Consider cognitive complexity, not just line count

## Results

### Analysis Completed

Comprehensive analysis of all large files was performed, identifying:
- Existing partial class patterns in the codebase (`endpoint-resolver.cs`, `ReplConsoleReader`)
- Established naming conventions (`{class-name}.{feature}.cs`)
- Documentation conventions (XML `<remarks>` listing all partials)

### Tasks Created

| Task | File | Lines | Refactoring |
|------|------|-------|-------------|
| 209 | `key-binding-builder.cs` | 585 | Split 3 classes, reduce duplication |
| 210 | `repl-console-reader.selection.cs` | 807 | Extract clipboard logic |
| 211 | `nuru-delegate-command-generator.cs` | 1250 | Split into 5-6 partials |
| 212 | `nuru-attributed-route-generator.cs` | 854 | Split into 4 partials |
| 213 | `nuru-invoker-generator.cs` | 541 | Split into 3 partials |
| 214 | `nuru-core-app.cs` | 709 | Split into 4 partials |
| 215 | `help-provider.cs` | 643 | Split into 4 partials |
| 216 | `parser.cs` | 628 | Split into 4 partials |
| 217 | `nuru-app-builder-extensions.cs` | 509 | Split into 3 partials + utility |
| 218 | `pipeline-middleware.cs` | 696 | Split sample into multiple files |
| 219 | Test files | Various | Separate review task |

### No Action Required

- `endpoint-resolver.cs` - Already well-organized with 4 partial files following best practices. Serves as the reference pattern for other refactorings.
