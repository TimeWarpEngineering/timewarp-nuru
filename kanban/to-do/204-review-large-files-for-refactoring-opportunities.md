# Review large files for refactoring opportunities

## Description

Files over 500 lines create cognitive load for both humans and AI agents. Large files waste tokens in agentic contexts and make code harder to understand and maintain. This task tracks files that should be reviewed for potential refactoring into smaller, more focused units.

## Checklist

### Source Files (Priority - Production Code)

- [ ] `nuru-delegate-command-generator.cs` (1250 lines, 812 code) - Largest source file
- [ ] `nuru-attributed-route-generator.cs` (854 lines, 644 code)
- [ ] `repl-console-reader.selection.cs` (807 lines, 589 code)
- [ ] `endpoint-resolver.cs` (798 lines, 601 code)
- [ ] `nuru-core-app.cs` (709 lines, 433 code)
- [ ] `help-provider.cs` (643 lines, 440 code)
- [ ] `parser.cs` (628 lines, 485 code)
- [ ] `key-binding-builder.cs` (584 lines, 209 code, 325 comments)
- [ ] `nuru-invoker-generator.cs` (540 lines, 380 code)
- [ ] `nuru-app-builder-extensions.cs` (509 lines, 315 code)

### Sample Files

- [ ] `pipeline-middleware.cs` (696 lines, 427 code) - Consider splitting into multiple samples

### Test Files (Lower Priority)

- [ ] `attributed-route-generator-02-source.cs` (692 lines, 506 code)
- [ ] `routing-05-option-matching.cs` (689 lines, 439 code)
- [ ] `engine-03-candidate-generator.cs` (673 lines, 463 code)
- [ ] `engine-01-input-tokenizer.cs` (672 lines, 467 code)
- [ ] `delegate-command-generator-01-basic.cs` (624 lines, 450 code)
- [ ] `program.cs` (testapp-mediator) (595 lines, 483 code)
- [ ] `repl-31-multiline-buffer.cs` (572 lines, 312 code)
- [ ] `repl-18-psreadline-keybindings.cs` (558 lines, 332 code)
- [ ] `repl-23-key-binding-profiles.cs` (530 lines, 358 code)
- [ ] `repl-28-text-selection.cs` (526 lines, 349 code)
- [ ] `attributed-route-generator-03-matching.cs` (524 lines, 353 code)
- [ ] `repl-33-yank-arguments.cs` (521 lines, 354 code)
- [ ] `lexer-15-advanced-features.cs` (515 lines, 376 code)
- [ ] `repl-29-word-operations.cs` (506 lines, 349 code)

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
